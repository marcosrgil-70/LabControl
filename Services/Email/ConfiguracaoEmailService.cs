using System.Net.Mail;
using System.Security.Cryptography;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Email;

/// <summary>
/// T05 › aba E-mail (8.3, pedido do usuário em 29/10/2026): servidor de saída e modelos do e-mail do boletim, por empresa,
/// em <c>EMPRESAS_PARAMETROS</c> (auditada pelo SaveChanges; a senha aparece como ***). A senha é cifrada com o
/// ASP.NET Data Protection e nunca volta para a tela: em branco mantém a gravada.
/// </summary>
public sealed class ConfiguracaoEmailService(LabDbContext db, IDataProtectionProvider protecao, IEnvioEmail envio, ILogger<ConfiguracaoEmailService> log)
{
    public const string AssuntoPadrao = "Boletim de análise {boletim} — {empresa}";
    public const string TextoPadrao = "Prezado(a),\n\nSegue em anexo o boletim de análise {boletim} da amostra {amostra}.\n\nAtenciosamente,\n{empresa}";
    public const string NaoConfigurado = "O envio de e-mail não está configurado nesta empresa: preencha Parâmetros da Empresa › E-mail.";
    public const int MaxDestinatarios = 10;

    private IDataProtector Protetor => protecao.CreateProtector("LabControl.EmpresasParametros.SmtpSenha.v1");

    public async Task<EmailEmpresaVM?> CarregarAsync()
    {
        var p = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        if (p is null) return null;
        return new EmailEmpresaVM
        {
            Servidor = p.SmtpServidor, Porta = p.SmtpPorta, Seguranca = p.SmtpSeguranca, Usuario = p.SmtpUsuario,
            Remetente = p.EmailRemetente, RemetenteNome = p.EmailRemetenteNome, CopiaOculta = p.EmailCopiaOculta,
            Assunto = p.EmailAssuntoBoletim ?? AssuntoPadrao, Texto = p.EmailTextoBoletim ?? TextoPadrao,
            TemSenha = p.SmtpSenha is not null, Configurado = p.SmtpServidor is not null,
        };
    }

    /// <summary>Servidor vazio desliga o envio (os outros campos ficam guardados).</summary>
    public async Task<Resultado> GravarAsync(EmailEmpresaVM vm)
    {
        var p = await db.EmpresasParametros.SingleOrDefaultAsync();
        if (p is null) return Resultado.Erro("Parâmetros da empresa não encontrados.");
        var servidor = Textos.Limpo(vm.Servidor)?.ToLowerInvariant();
        var usuario = Textos.Limpo(vm.Usuario);
        var remetente = Textos.Limpo(vm.Remetente)?.ToLowerInvariant();
        var copiaOculta = Textos.Limpo(vm.CopiaOculta)?.ToLowerInvariant();
        var assunto = Textos.Limpo(vm.Assunto);
        var texto = Textos.Limpo(vm.Texto);
        var temSenha = !string.IsNullOrEmpty(vm.NovaSenha) || (p.SmtpSenha is not null && !vm.ApagarSenha);

        if (servidor is not null)
        {
            if (servidor.Length > 150 || servidor.Any(char.IsWhiteSpace)) return Resultado.Erro("Servidor inválido (ex.: smtp.gmail.com).", nameof(vm.Servidor));
            if (vm.Porta is < 1 or > 65535) return Resultado.Erro("Informe uma porta de 1 a 65535 (normalmente 587 ou 465).", nameof(vm.Porta));
            if (remetente is null) return Resultado.Erro("Informe o e-mail do remetente.", nameof(vm.Remetente));
            if (usuario is not null && !temSenha) return Resultado.Erro("Informe a senha do usuário do e-mail.", nameof(vm.NovaSenha));
        }
        if (!Enum.IsDefined(vm.Seguranca)) return Resultado.Erro("Escolha a segurança da conexão.", nameof(vm.Seguranca));
        if (usuario?.Length > 150) return Resultado.Erro("Usuário: até 150 caracteres.", nameof(vm.Usuario));
        if (remetente is not null && !EmailValido(remetente, 150)) return Resultado.Erro("E-mail do remetente inválido.", nameof(vm.Remetente));
        if (Textos.Limpo(vm.RemetenteNome)?.Length > 100) return Resultado.Erro("Nome do remetente: até 100 caracteres.", nameof(vm.RemetenteNome));
        if (copiaOculta is not null && !EmailValido(copiaOculta, 150)) return Resultado.Erro("E-mail da cópia oculta inválido.", nameof(vm.CopiaOculta));
        if (assunto?.Length > 200) return Resultado.Erro("Assunto: até 200 caracteres.", nameof(vm.Assunto));
        if (vm.NovaSenha?.Length > 200) return Resultado.Erro("Senha: até 200 caracteres.", nameof(vm.NovaSenha));

        (p.SmtpServidor, p.SmtpPorta, p.SmtpSeguranca, p.SmtpUsuario) = (servidor, vm.Porta, vm.Seguranca, usuario);
        if (!string.IsNullOrEmpty(vm.NovaSenha)) p.SmtpSenha = Protetor.Protect(vm.NovaSenha);
        else if (vm.ApagarSenha) p.SmtpSenha = null;
        (p.EmailRemetente, p.EmailRemetenteNome, p.EmailCopiaOculta) = (remetente, Textos.Limpo(vm.RemetenteNome), copiaOculta);
        // o modelo igual ao padrão não é gravado: se o padrão mudar numa versão nova, a empresa acompanha
        p.EmailAssuntoBoletim = assunto == AssuntoPadrao ? null : assunto;
        p.EmailTextoBoletim = texto is null || Normalizar(texto) == Normalizar(TextoPadrao) ? null : texto;
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        return gravou.Sucesso ? Resultado.Ok() : gravou.Falha;
    }

    /// <summary>Configuração gravada pronta para enviar (senha decifrada), ou a mensagem do que falta.</summary>
    public async Task<Resultado<ConfiguracaoEmail>> ConfiguracaoAsync()
    {
        var p = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        if (p?.SmtpServidor is null || p.EmailRemetente is null) return Resultado.Erro(NaoConfigurado);
        string? senha = null;
        if (p.SmtpSenha is not null)
            try { senha = Protetor.Unprotect(p.SmtpSenha); }
            catch (CryptographicException ex)
            {
                // as chaves do Data Protection do servidor mudaram (outra máquina, pasta de chaves apagada)
                log.LogError(ex, "Senha do e-mail da empresa ilegível");
                return Resultado.Erro("A senha do e-mail gravada não pôde ser lida neste servidor: informe-a de novo em Parâmetros da Empresa › E-mail.");
            }
        return Resultado.Ok(new ConfiguracaoEmail(
            new ServidorEmail(p.SmtpServidor, p.SmtpPorta, p.SmtpSeguranca, p.SmtpUsuario, senha),
            p.EmailRemetente, p.EmailRemetenteNome, p.EmailCopiaOculta,
            p.EmailAssuntoBoletim ?? AssuntoPadrao, p.EmailTextoBoletim ?? TextoPadrao));
    }

    /// <summary>"Enviar e-mail de teste" com a configuração gravada (sem anexo).</summary>
    public async Task<Resultado> TestarAsync(string? destino)
    {
        var para = Textos.Limpo(destino)?.ToLowerInvariant();
        if (para is null || !EmailValido(para, 150)) return Resultado.Erro("Informe um e-mail válido para o teste.");
        var cfg = await ConfiguracaoAsync();
        if (!cfg.Sucesso) return cfg.Falha;
        var c = cfg.Valor;
        return await EnviarAsync(c.Servidor, new MensagemEmail(c.Remetente, c.RemetenteNome, [para], [], [],
            "Teste de envio do LabControl", "Este é um e-mail de teste do LabControl: o envio de boletins por e-mail está funcionando.", []));
    }

    /// <summary>Envia e transforma falha de conexão, autenticação ou recusa do servidor em mensagem para a tela.</summary>
    public async Task<Resultado> EnviarAsync(ServidorEmail servidor, MensagemEmail mensagem)
    {
        try
        {
            await envio.EnviarAsync(servidor, mensagem);
            return Resultado.Ok();
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or OperationCanceledException))
        {
            log.LogWarning(ex, "Falha ao enviar e-mail por {Servidor}:{Porta}", servidor.Servidor, servidor.Porta);
            return Resultado.Erro($"Não foi possível enviar o e-mail ({servidor.Servidor}:{servidor.Porta}): {ex.Message}");
        }
    }

    /// <summary>Lista separada por <c>;</c>, <c>,</c> ou espaço; null = algum endereço inválido.</summary>
    public static List<string>? Enderecos(string? texto)
    {
        var lista = (texto ?? "").Split([';', ',', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.ToLowerInvariant()).Distinct().ToList();
        return lista.All(e => EmailValido(e, 150)) ? lista : null;
    }

    public static bool EmailValido(string email, int max) =>
        email.Length <= max && MailAddress.TryCreate(email, out var a) && a.Address == email && a.Host.Contains('.');

    private static string Normalizar(string s) => s.Replace("\r\n", "\n").Trim();
}

public sealed record ConfiguracaoEmail(ServidorEmail Servidor, string Remetente, string? RemetenteNome, string? CopiaOculta,
    string ModeloAssunto, string ModeloTexto);
