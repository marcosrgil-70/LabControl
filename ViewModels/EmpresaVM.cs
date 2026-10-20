using System.ComponentModel.DataAnnotations;
using LabControl.Services;
using Microsoft.AspNetCore.Http;

namespace LabControl.ViewModels;

public sealed record EmpresaListaItemVM(int Id, string Codigo, string Nome, string? Cnpj, string? Cidade, bool Ativa, int Usuarios);

public sealed record OpcaoVM(int Id, string Nome);

/// <summary>Formulário da T03 (dados da empresa; na inclusão, também a cópia de cadastros).</summary>
public sealed class EmpresaVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o código."), StringLength(3, MinimumLength = 1, ErrorMessage = "Código com até 3 caracteres."),
     RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "O código tem só letras e números."), Display(Name = "Código")]
    public string CodEmpresa { get; set; } = "";

    [Required(ErrorMessage = "Informe a razão social."), StringLength(150), Display(Name = "Razão social")]
    public string RazaoSocial { get; set; } = "";

    [StringLength(150), Display(Name = "Nome fantasia")] public string? NomeFantasia { get; set; }
    [StringLength(18), Display(Name = "CNPJ")] public string? Cnpj { get; set; }
    [StringLength(20), Display(Name = "Inscrição estadual")] public string? InscEstadual { get; set; }
    [StringLength(20), Display(Name = "Inscrição municipal")] public string? InscMunicipal { get; set; }

    [Url(ErrorMessage = "Site inválido (use http:// ou https://)."), StringLength(150), Display(Name = "Site")] public string? Site { get; set; }

    [Display(Name = "Cabeçalho do laudo")] public string? CabecalhoLaudo { get; set; }
    [Display(Name = "Rodapé do laudo")] public string? RodapeLaudo { get; set; }
    [StringLength(50), Display(Name = "Nº de acreditação")] public string? NrAcreditacao { get; set; }
    [Display(Name = "Ativa")] public bool Ativa { get; set; } = true;

    [Display(Name = "Logo (PNG ou JPG, até 1 MB)")] public IFormFile? NovoLogo { get; set; }
    [Display(Name = "Remover o logo")] public bool RemoverLogo { get; set; }

    // inclusão (ADMIN_GLOBAL)
    [Display(Name = "Copiar cadastros de")] public int? CopiarDe { get; set; }
    public List<string> Grupos { get; set; } = [];

    // exibição
    public bool TemLogo { get; set; }
    public bool AdminGlobal { get; set; }
    /// <summary>Entidade da empresa (endereços, telefones e e-mails — componente de contatos).</summary>
    public int? IdEntidades { get; set; }
    public IReadOnlyList<OpcaoVM> EmpresasOrigem { get; set; } = [];
    public IReadOnlyList<GrupoCopia> GruposDisponiveis { get; set; } = [];
}
