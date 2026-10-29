using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.Services.Email;
using LabControl.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests;

/// <summary>Envio de e-mail dos testes: guarda as mensagens (ou falha como um servidor fora do ar).</summary>
public sealed class EmailDoTeste : IEnvioEmail
{
    public List<(ServidorEmail Servidor, MensagemEmail Mensagem)> Enviados { get; } = [];
    public string? Falha { get; set; }

    public Task EnviarAsync(ServidorEmail servidor, MensagemEmail mensagem, CancellationToken ct = default)
    {
        if (Falha is not null) throw new System.Net.Sockets.SocketException(10061);
        lock (Enviados) Enviados.Add((servidor, mensagem));
        return Task.CompletedTask;
    }
}

/// <summary>Aceite da 8.3: configuração do e-mail na T05 (senha cifrada, fora da auditoria) e envio do boletim emitido (T27).</summary>
public class EmailTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "lc-boletins-email-" + Guid.NewGuid().ToString("N")[..8]);
    private readonly IDataProtectionProvider _protecao = new EphemeralDataProtectionProvider();
    private readonly EmailDoTeste _email = new();

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true);
        GC.SuppressFinalize(this);
    }

    private ConfiguracaoEmailService Config(Cenario c, IDataProtectionProvider? protecao = null) =>
        new(c.Db, protecao ?? _protecao, _email, NullLogger<ConfiguracaoEmailService>.Instance);

    private BoletimService Boletins(Cenario c) =>
        new(c.Db, c.Ativa, c.Procedures, new AmostraService(c.Db, c.Ativa, c.Procedures), new ArmazemBoletins(_pasta), NullLogger<BoletimService>.Instance);

    private EnvioBoletimService Envio(Cenario c) => new(c.Db, Boletins(c), Config(c), new ServicoAuditoria(c.Db, c.Ativa));

    private static EmailEmpresaVM Valida() => new()
    {
        Servidor = "SMTP.Laboratorio.com.br", Porta = 587, Seguranca = SegurancaSmtp.StartTls, Usuario = "laudos@laboratorio.com.br",
        NovaSenha = "segredo-123", Remetente = "Laudos@Laboratorio.com.br", RemetenteNome = "Laboratório Teste",
        CopiaOculta = "arquivo@laboratorio.com.br", Assunto = ConfiguracaoEmailService.AssuntoPadrao, Texto = ConfiguracaoEmailService.TextoPadrao,
    };

    /// <summary>Boletim emitido de verdade (PDF na pasta temporária), como na tela.</summary>
    private async Task<int> BoletimEmitidoAsync(Cenario c)
    {
        await c.Db.EntidadesFuncionarios.Where(f => f.IdEntidades == c.Ativa.ProfissionalId).ExecuteUpdateAsync(s => s.SetProperty(f => f.AssinaBoletim, true));
        var (amo, _, num, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        var s = Boletins(c);
        var vm = (await s.NovoAsync(amo.IdLabHistAmostras))!;
        (vm.Selecionados, vm.IdResponsavel1) = ([num.IdLabResultadosParam], c.Ativa.ProfissionalId);
        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal("Só o boletim emitido pode ser enviado por e-mail.", (await Envio(c).PrepararAsync(r.Valor)).Mensagem);
        var emitir = await s.EmitirAsync(r.Valor);
        Assert.True(emitir.Sucesso, emitir.Mensagem);
        return r.Valor;
    }

    [Fact]
    public async Task Configuracao_valida_cifra_a_senha_e_a_auditoria_nao_mostra()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Config(c);
        Assert.Equal(ConfiguracaoEmailService.NaoConfigurado, (await s.ConfiguracaoAsync()).Mensagem);
        var vazio = (await s.CarregarAsync())!;
        Assert.Equal((false, false, 587, ConfiguracaoEmailService.AssuntoPadrao), (vazio.Configurado, vazio.TemSenha, vazio.Porta, vazio.Assunto));

        var semRemetente = Valida();
        semRemetente.Remetente = null;
        Assert.Equal(("Informe o e-mail do remetente.", "Remetente"), ((await s.GravarAsync(semRemetente)).Mensagem, (await s.GravarAsync(semRemetente)).Campo));
        var semSenha = Valida();
        semSenha.NovaSenha = null;
        Assert.Equal("Informe a senha do usuário do e-mail.", (await s.GravarAsync(semSenha)).Mensagem);
        var porta = Valida();
        porta.Porta = 0;
        Assert.Equal("Porta", (await s.GravarAsync(porta)).Campo);
        var remetente = Valida();
        remetente.Remetente = "laudos@";
        Assert.Equal("E-mail do remetente inválido.", (await s.GravarAsync(remetente)).Mensagem);

        Assert.True((await s.GravarAsync(Valida())).Sucesso);
        var p = await c.Db.EmpresasParametros.AsNoTracking().SingleAsync();
        Assert.Equal(("smtp.laboratorio.com.br", "laudos@laboratorio.com.br"), (p.SmtpServidor, p.EmailRemetente));
        Assert.DoesNotContain("segredo-123", p.SmtpSenha);
        Assert.Null(p.EmailAssuntoBoletim);   // igual ao padrão: não grava
        var tela = (await s.CarregarAsync())!;
        Assert.Equal((true, true, null), (tela.Configurado, tela.TemSenha, tela.NovaSenha));
        Assert.Equal("segredo-123", (await s.ConfiguracaoAsync()).Valor.Servidor.Senha);

        // senha em branco mantém a gravada; "apagar" tira (e o usuário passa a exigir uma nova)
        tela.NovaSenha = null;
        Assert.True((await s.GravarAsync(tela)).Sucesso);
        Assert.Equal("segredo-123", (await s.ConfiguracaoAsync()).Valor.Servidor.Senha);
        var auditoria = await c.Db.Auditoria.AsNoTracking().Where(a => a.Tabela == "EMPRESAS_PARAMETROS").Select(a => a.DadosDepois).ToListAsync();
        Assert.Contains(auditoria, d => d!.Contains("\"SMTP_SENHA\": \"***\""));
        Assert.DoesNotContain(auditoria, d => d!.Contains("segredo"));
        tela.ApagarSenha = true;
        Assert.Equal("Informe a senha do usuário do e-mail.", (await s.GravarAsync(tela)).Mensagem);

        // chaves do servidor trocadas: pede a senha de novo em vez de dar erro
        Assert.StartsWith("A senha do e-mail gravada não pôde ser lida", (await Config(c, new EphemeralDataProtectionProvider()).ConfiguracaoAsync()).Mensagem);

        // e-mail de teste com a configuração gravada; servidor fora do ar vira mensagem
        Assert.Equal("Informe um e-mail válido para o teste.", (await s.TestarAsync("x")).Mensagem);
        Assert.True((await s.TestarAsync("Eu@Teste.com")).Sucesso);
        var (servidor, msg) = Assert.Single(_email.Enviados);
        Assert.Equal(("smtp.laboratorio.com.br", 587, SegurancaSmtp.StartTls, "segredo-123"), (servidor.Servidor, servidor.Porta, servidor.Seguranca, servidor.Senha));
        Assert.Equal(["eu@teste.com"], msg.Para);
        Assert.Equal(("laudos@laboratorio.com.br", "Laboratório Teste"), (msg.RemetenteEmail, msg.RemetenteNome));
        _email.Falha = "fora";
        Assert.StartsWith("Não foi possível enviar o e-mail (smtp.laboratorio.com.br:587):", (await s.TestarAsync("eu@teste.com")).Mensagem);
    }

    [Fact]
    public async Task Boletim_emitido_vai_com_o_pdf_gravado_e_o_envio_fica_no_historico()
    {
        await using var c = await Cenario.CriarAsync();
        var id = await BoletimEmitidoAsync(c);
        var envio = Envio(c);
        Assert.Equal(ConfiguracaoEmailService.NaoConfigurado, (await envio.PrepararAsync(id)).Mensagem);
        Assert.True((await Config(c).GravarAsync(Valida())).Sucesso);

        var vm = (await envio.PrepararAsync(id)).Valor;
        var b = await c.Db.LabBoletimAnalises.AsNoTracking().SingleAsync(x => x.IdLabBoletimAnalises == id);
        Assert.Equal("cliente@teste.com", vm.Para);   // contato do resultado da proposta
        Assert.Equal($"Boletim de análise {b.CodBoletim} R00 — {c.Empresa.RazaoSocial}", vm.Assunto);
        Assert.Contains($"da amostra {b.CodBoletim}", vm.Texto);
        Assert.Empty(vm.Envios);

        vm.Para = "cliente@teste.com; errado@";
        Assert.Equal(("Há um endereço inválido em \"Para\" (separe vários com ponto e vírgula).", "Para"), ((await envio.EnviarAsync(vm)).Mensagem, (await envio.EnviarAsync(vm)).Campo));
        vm.Para = "";
        Assert.Equal("Informe ao menos um destinatário.", (await envio.EnviarAsync(vm)).Mensagem);
        _email.Falha = "fora";
        vm.Para = "cliente@teste.com";
        Assert.StartsWith("Não foi possível enviar o e-mail", (await envio.EnviarAsync(vm)).Mensagem);
        _email.Falha = null;

        vm.Copia = "Gerente@Cliente.com";
        var r = await envio.EnviarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        var (_, msg) = Assert.Single(_email.Enviados);
        Assert.Equal(["cliente@teste.com"], msg.Para);
        Assert.Equal(["gerente@cliente.com"], msg.Copia);
        Assert.Equal(["arquivo@laboratorio.com.br"], msg.CopiaOculta);
        var anexo = Assert.Single(msg.Anexos);
        Assert.Equal(b.HashPdf, ArmazemBoletins.Hash(anexo.Conteudo));   // o PDF da emissão, não um gerado de novo
        Assert.Equal("application/pdf", anexo.TipoMime);

        // auditoria N (só o envio que deu certo) e histórico na tela
        var eventos = await c.Db.Auditoria.AsNoTracking().Where(a => a.Tabela == "LAB_BOLETIM_ANALISES" && a.IdRegistro == id.ToString() && a.Acao == AcoesAuditoria.EnvioEmail).ToListAsync();
        Assert.Contains("\"ANEXO\"", Assert.Single(eventos).DadosDepois);
        var historico = Assert.Single((await envio.PrepararAsync(id)).Valor.Envios);
        Assert.Equal("cliente@teste.com; cópia: gerente@cliente.com", historico.Para);
    }
}
