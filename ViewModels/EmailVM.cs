using System.ComponentModel.DataAnnotations;
using LabControl.Models;

namespace LabControl.ViewModels;

/// <summary>Aba "E-mail" da T05 (8.3): servidor de saída e modelos do e-mail do boletim, por empresa.</summary>
public sealed class EmailEmpresaVM
{
    [Display(Name = "Servidor SMTP")] public string? Servidor { get; set; }
    [Display(Name = "Porta")] public int Porta { get; set; } = 587;
    [Display(Name = "Segurança")] public SegurancaSmtp Seguranca { get; set; } = SegurancaSmtp.StartTls;
    [Display(Name = "Usuário")] public string? Usuario { get; set; }
    /// <summary>Só vem preenchida quando o usuário digita uma senha nova (a gravada nunca volta para a tela).</summary>
    [Display(Name = "Senha"), DataType(DataType.Password)] public string? NovaSenha { get; set; }
    [Display(Name = "Apagar a senha gravada")] public bool ApagarSenha { get; set; }
    [Display(Name = "E-mail do remetente")] public string? Remetente { get; set; }
    [Display(Name = "Nome do remetente")] public string? RemetenteNome { get; set; }
    [Display(Name = "Cópia oculta de todo envio")] public string? CopiaOculta { get; set; }
    [Display(Name = "Assunto do e-mail do boletim")] public string? Assunto { get; set; }
    [Display(Name = "Texto do e-mail do boletim")] public string? Texto { get; set; }

    // exibição
    public bool TemSenha { get; set; }
    public bool Configurado { get; set; }
}

/// <summary>Envio de um boletim emitido por e-mail (T27, 8.3).</summary>
public sealed class EnvioBoletimVM
{
    public int IdBoletim { get; set; }
    [Display(Name = "Para")] public string? Para { get; set; }
    [Display(Name = "Cópia")] public string? Copia { get; set; }
    [Display(Name = "Assunto")] public string? Assunto { get; set; }
    [Display(Name = "Mensagem")] public string? Texto { get; set; }

    // exibição
    public string CodBoletim { get; set; } = "";
    public int Revisao { get; set; }
    public string NrAmostra { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string? CopiaOculta { get; set; }
    public string? Remetente { get; set; }
    public string Anexo { get; set; } = "";
    public IReadOnlyList<EnvioRealizadoVM> Envios { get; set; } = [];
}

public sealed record EnvioRealizadoVM(DateTime Quando, string? Usuario, string? Para);
