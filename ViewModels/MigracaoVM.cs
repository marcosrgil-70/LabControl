using System.ComponentModel.DataAnnotations;
using LabControl.Services.Migracao;

namespace LabControl.ViewModels;

/// <summary>Formulário da T07: origem Firebird, destino vazio e a senha provisória única dos usuários migrados.</summary>
public sealed class MigracaoFormVM
{
    [Required(ErrorMessage = "Informe o caminho do banco do legado."), Display(Name = "Banco do legado (.FDB)")]
    public string CaminhoFirebird { get; set; } = "";

    [Required(ErrorMessage = "Informe o usuário do Firebird."), Display(Name = "Usuário do Firebird")]
    public string UsuarioFirebird { get; set; } = "SYSDBA";

    [Display(Name = "Senha do Firebird")]
    public string? SenhaFirebird { get; set; }

    [Required(ErrorMessage = "Informe o banco destino."), Display(Name = "Banco destino (vazio)")]
    public string BancoDestino { get; set; } = "labcontrol_migracao";

    [Required(ErrorMessage = "Informe a senha provisória dos usuários migrados."), Display(Name = "Senha provisória dos usuários")]
    public string? SenhaProvisoria { get; set; }

    [Compare(nameof(SenhaProvisoria), ErrorMessage = "A confirmação não confere com a senha provisória."), Display(Name = "Confirme a senha")]
    public string? ConfirmarSenha { get; set; }
}

public sealed class MigracaoVM
{
    public required MigracaoFormVM Form { get; init; }
    /// <summary>Última execução (em andamento ou terminada) desde que o app subiu.</summary>
    public RelatorioMigracao? Relatorio { get; init; }
}
