using LabControl.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LabControl.Infra;

/// <summary>Servidor de saída da empresa ativa (T05 › E-mail), com a senha já decifrada.</summary>
public sealed record ServidorEmail(string Servidor, int Porta, SegurancaSmtp Seguranca, string? Usuario, string? Senha);

public sealed record AnexoEmail(string Nome, byte[] Conteudo, string TipoMime = "application/pdf");

public sealed record MensagemEmail(string RemetenteEmail, string? RemetenteNome, IReadOnlyList<string> Para, IReadOnlyList<string> Copia,
    IReadOnlyList<string> CopiaOculta, string Assunto, string Texto, IReadOnlyList<AnexoEmail> Anexos);

/// <summary>
/// Envio de e-mail (8.3). A implementação real usa SMTP (MailKit); os testes registram uma falsa que guarda as mensagens,
/// como a <see cref="IImpressoraRaw"/> das etiquetas. Falha de conexão, autenticação ou recusa do servidor vira exceção.
/// </summary>
public interface IEnvioEmail
{
    Task EnviarAsync(ServidorEmail servidor, MensagemEmail mensagem, CancellationToken ct = default);
}

public sealed class EnvioEmailSmtp : IEnvioEmail
{
    public async Task EnviarAsync(ServidorEmail servidor, MensagemEmail m, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(m.RemetenteNome ?? "", m.RemetenteEmail));
        foreach (var e in m.Para) msg.To.Add(MailboxAddress.Parse(e));
        foreach (var e in m.Copia) msg.Cc.Add(MailboxAddress.Parse(e));
        foreach (var e in m.CopiaOculta) msg.Bcc.Add(MailboxAddress.Parse(e));
        msg.Subject = m.Assunto;
        var corpo = new BodyBuilder { TextBody = m.Texto };
        foreach (var a in m.Anexos) corpo.Attachments.Add(a.Nome, a.Conteudo, ContentType.Parse(a.TipoMime));
        msg.Body = corpo.ToMessageBody();

        using var smtp = new SmtpClient { Timeout = 30_000 };
        var opcao = servidor.Seguranca switch
        {
            SegurancaSmtp.Ssl => SecureSocketOptions.SslOnConnect,
            SegurancaSmtp.StartTls => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None,
        };
        await smtp.ConnectAsync(servidor.Servidor, servidor.Porta, opcao, ct);
        if (!string.IsNullOrEmpty(servidor.Usuario)) await smtp.AuthenticateAsync(servidor.Usuario, servidor.Senha ?? "", ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
