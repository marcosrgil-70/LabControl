using LabControl.Data;
using LabControl.Infra;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Email;

/// <summary>
/// Envio do boletim emitido por e-mail (T27, P17 passo 8, tarefa 8.3): para o contato do resultado da proposta (pode
/// trocar ou acrescentar), com o PDF gravado na emissão conferido pelo SHA-256 (migrado: a 2ª via). Cada envio vira um
/// evento N em AUDITORIA (destinatários, assunto, anexo), que a tela lista como histórico.
/// </summary>
public sealed class EnvioBoletimService(LabDbContext db, BoletimService boletins, ConfiguracaoEmailService config, IAuditoria auditoria)
{
    private const string Tabela = "LAB_BOLETIM_ANALISES";

    public async Task<Resultado<EnvioBoletimVM>> PrepararAsync(int id)
    {
        var dados = await DadosAsync(id);
        if (!dados.Sucesso) return dados.Falha;
        var d = dados.Valor;
        var cfg = await config.ConfiguracaoAsync();
        if (!cfg.Sucesso) return cfg.Falha;
        var c = cfg.Valor;
        var vm = new EnvioBoletimVM
        {
            IdBoletim = id, Para = d.EmailResultado, Assunto = Preencher(c.ModeloAssunto, d), Texto = Preencher(c.ModeloTexto, d),
        };
        await ExibicaoAsync(vm, d, c);
        return Resultado.Ok(vm);
    }

    /// <summary>Recusa: a tela volta com o que foi digitado (dados de exibição recarregados).</summary>
    public async Task<EnvioBoletimVM?> RecarregarAsync(EnvioBoletimVM vm)
    {
        var dados = await DadosAsync(vm.IdBoletim);
        if (!dados.Sucesso) return null;
        var cfg = await config.ConfiguracaoAsync();
        await ExibicaoAsync(vm, dados.Valor, cfg.Sucesso ? cfg.Valor : null);
        return vm;
    }

    public async Task<Resultado> EnviarAsync(EnvioBoletimVM vm)
    {
        var dados = await DadosAsync(vm.IdBoletim);
        if (!dados.Sucesso) return dados.Falha;
        var para = ConfiguracaoEmailService.Enderecos(vm.Para);
        if (para is null) return Resultado.Erro("Há um endereço inválido em \"Para\" (separe vários com ponto e vírgula).", nameof(vm.Para));
        if (para.Count == 0) return Resultado.Erro("Informe ao menos um destinatário.", nameof(vm.Para));
        var copia = ConfiguracaoEmailService.Enderecos(vm.Copia);
        if (copia is null) return Resultado.Erro("Há um endereço inválido em \"Cópia\".", nameof(vm.Copia));
        if (para.Count + copia.Count > ConfiguracaoEmailService.MaxDestinatarios)
            return Resultado.Erro($"No máximo {ConfiguracaoEmailService.MaxDestinatarios} destinatários por envio.", nameof(vm.Para));
        var assunto = Textos.Limpo(vm.Assunto);
        if (assunto is null) return Resultado.Erro("Informe o assunto.", nameof(vm.Assunto));
        if (assunto.Length > 200) return Resultado.Erro("Assunto: até 200 caracteres.", nameof(vm.Assunto));
        var texto = Textos.Limpo(vm.Texto);
        if (texto is null) return Resultado.Erro("Escreva a mensagem.", nameof(vm.Texto));

        var cfg = await config.ConfiguracaoAsync();
        if (!cfg.Sucesso) return cfg.Falha;
        var c = cfg.Valor;
        // o mesmo PDF da reimpressão: arquivo da emissão conferido pelo hash (alerta de integridade não envia)
        var pdf = await boletins.ImprimirAsync(vm.IdBoletim);
        if (!pdf.Sucesso) return pdf.Falha;
        var oculta = c.CopiaOculta is { } co && !para.Contains(co) && !copia.Contains(co) ? new List<string> { co } : [];

        var enviado = await config.EnviarAsync(c.Servidor, new MensagemEmail(c.Remetente, c.RemetenteNome, para, copia, oculta, assunto, texto,
            [new AnexoEmail(pdf.Valor.Nome, pdf.Valor.Conteudo)]));
        if (!enviado.Sucesso) return enviado;
        await auditoria.RegistrarAsync(AcoesAuditoria.EnvioEmail, Tabela, vm.IdBoletim.ToString(), depois: new
        {
            PARA = string.Join("; ", para), COPIA = copia.Count == 0 ? null : string.Join("; ", copia),
            COPIA_OCULTA = oculta.FirstOrDefault(), ASSUNTO = assunto, ANEXO = pdf.Valor.Nome, SERVIDOR = c.Servidor.Servidor,
        });
        return Resultado.Ok();
    }

    // ------------------------------------------------------------------ apoio

    private sealed record DadosBoletim(string CodBoletim, int Revisao, string NrAmostra, string Cliente, string? EmailResultado, string Empresa, bool TemArquivo);

    private async Task<Resultado<DadosBoletim>> DadosAsync(int id)
    {
        var b = await db.LabBoletimAnalises.AsNoTracking().Where(x => x.IdLabBoletimAnalises == id)
            .Select(x => new
            {
                x.CodBoletim, x.Revisao, x.DhEmissao, x.ArquivoPdf, x.LabHistAmostra.NrAmostra, Cliente = x.LabHistAmostra.Entidade.Nome,
                EmailResultado = x.LabHistAmostra.LabProposta.EntContatoResultado.Email, Empresa = x.Empresa.RazaoSocial,
            })
            .SingleOrDefaultAsync();
        if (b is null) return Resultado.Erro(BoletimService.MensagemNaoEncontrado);
        if (b.DhEmissao is null) return Resultado.Erro("Só o boletim emitido pode ser enviado por e-mail.");
        return Resultado.Ok(new DadosBoletim(b.CodBoletim, b.Revisao, b.NrAmostra, b.Cliente, b.EmailResultado, b.Empresa, b.ArquivoPdf is not null));
    }

    private async Task ExibicaoAsync(EnvioBoletimVM vm, DadosBoletim d, ConfiguracaoEmail? c)
    {
        (vm.CodBoletim, vm.Revisao, vm.NrAmostra, vm.Cliente) = (d.CodBoletim, d.Revisao, d.NrAmostra, d.Cliente);
        (vm.Remetente, vm.CopiaOculta) = (c?.Remetente, c?.CopiaOculta);
        vm.Anexo = $"{d.CodBoletim.Replace('/', '-')} R{d.Revisao:00}.pdf" + (d.TemArquivo ? "" : " (2ª via — boletim migrado)");
        var id = vm.IdBoletim.ToString();
        vm.Envios = (await (from a in db.Auditoria.AsNoTracking()
                            where a.Tabela == Tabela && a.IdRegistro == id && a.Acao == AcoesAuditoria.EnvioEmail
                            join u in db.Usuarios.AsNoTracking() on a.IdUsuarios equals u.IdUsuarios into us
                            from u in us.DefaultIfEmpty()
                            orderby a.DtEvento descending
                            select new { a.DtEvento, Login = u == null ? null : u.Login, a.DadosDepois })
                .Take(20).ToListAsync())
            .Select(a => new EnvioRealizadoVM(a.DtEvento, a.Login, Destinatarios(a.DadosDepois)))
            .ToList();
    }

    private static string? Destinatarios(string? json)
    {
        if (json is null) return null;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var r = doc.RootElement;
        string? Campo(string nome) => r.TryGetProperty(nome, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
        return Campo("COPIA") is { } cc ? $"{Campo("PARA")}; cópia: {cc}" : Campo("PARA");
    }

    private static string Preencher(string modelo, DadosBoletim d) => modelo
        .Replace("{boletim}", $"{d.CodBoletim} R{d.Revisao:00}").Replace("{amostra}", d.NrAmostra)
        .Replace("{cliente}", d.Cliente).Replace("{empresa}", d.Empresa);
}
