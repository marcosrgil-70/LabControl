using System.ComponentModel.DataAnnotations;
using LabControl.Services.Entidades;

namespace LabControl.ViewModels;

/// <summary>Abas Endereços / Telefones / E-mails de uma entidade (empresa usuária, cliente/fornecedor, profissional).</summary>
public sealed class ContatosVM
{
    public required CadastroDono Dono { get; init; }
    public required int IdEntidades { get; init; }
    /// <summary>Base das rotas (ex.: <c>/Clientes/12/Contatos</c>, <c>/Empresa/Contatos</c>).</summary>
    public required string UrlBase { get; init; }
    /// <summary>Usuário pode incluir/alterar/excluir contatos (permissão de ALTERAR do cadastro).</summary>
    public bool PodeAlterar { get; init; }

    public IReadOnlyList<EnderecoLinhaVM> Enderecos { get; init; } = [];
    public IReadOnlyList<FoneLinhaVM> Fones { get; init; } = [];
    public IReadOnlyList<EmailLinhaVM> Emails { get; init; } = [];

    // combos (ativos + os já usados)
    public IReadOnlyList<OpcaoVM> TiposEndereco { get; init; } = [];
    public IReadOnlyList<OpcaoVM> TiposLogradouro { get; init; } = [];
    public IReadOnlyList<OpcaoVM> TiposFone { get; init; } = [];
    public IReadOnlyList<OpcaoVM> TiposEmail { get; init; } = [];
    public IReadOnlyList<OpcaoVM> Estados { get; init; } = [];
}

public sealed record EnderecoLinhaVM(int Id, int IdTipo, string Tipo, int? IdTipoLogradouro, string Logradouro, string? Numero,
    string? Complemento, string? Bairro, string? Cep, int IdEstado, int IdCidade, string Cidade, string Uf, bool Inativo, string Linha1, string Linha2);

public sealed record FoneLinhaVM(int Id, int IdTipo, string Tipo, string Ddd, string Fone, string? Contato, string? Departamento, bool Principal, bool Inativo);

public sealed record EmailLinhaVM(int Id, int? IdTipo, string? Tipo, string Email, string? NomeContato, string? Departamento,
    bool Principal, bool EnviarNfe, bool EnviarLaudo, bool Inativo);

/// <summary>Formulário de endereço (modal). Id = 0 inclui.</summary>
public sealed class EnderecoVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Informe o tipo de endereço."), Display(Name = "Tipo de endereço")] public int? IdEnderecosTipos { get; set; }
    [Display(Name = "CEP")] public string? Cep { get; set; }
    [Display(Name = "Tipo de logradouro")] public int? IdTiposLogradouro { get; set; }
    [Required(ErrorMessage = "Informe o logradouro."), StringLength(150), Display(Name = "Logradouro")] public string? Logradouro { get; set; }
    [StringLength(20), Display(Name = "Número")] public string? Numero { get; set; }
    [StringLength(50), Display(Name = "Complemento")] public string? Complemento { get; set; }
    [StringLength(80), Display(Name = "Bairro")] public string? Bairro { get; set; }
    [Display(Name = "UF")] public int? IdEstado { get; set; }
    [Required(ErrorMessage = "Informe a cidade."), Display(Name = "Cidade")] public int? IdCidades { get; set; }
    [Display(Name = "Inativo")] public bool Inativo { get; set; }
}

public sealed class FoneVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Informe o tipo de telefone."), Display(Name = "Tipo")] public int? IdFonesTipos { get; set; }
    [Required(ErrorMessage = "Informe o DDD."), Display(Name = "DDD")] public string? Ddd { get; set; }
    [Required(ErrorMessage = "Informe o telefone."), Display(Name = "Telefone")] public string? Fone { get; set; }
    [StringLength(100), Display(Name = "Contato")] public string? Contato { get; set; }
    [StringLength(100), Display(Name = "Departamento")] public string? Departamento { get; set; }
    [Display(Name = "Principal")] public bool Principal { get; set; }
    [Display(Name = "Inativo")] public bool Inativo { get; set; }
}

public sealed class EmailVM
{
    public int Id { get; set; }
    [Display(Name = "Tipo")] public int? IdEmailsTipos { get; set; }
    [Required(ErrorMessage = "Informe o e-mail."), Display(Name = "E-mail")] public string? Email { get; set; }
    [StringLength(100), Display(Name = "Nome do contato")] public string? NomeContato { get; set; }
    [StringLength(100), Display(Name = "Departamento")] public string? Departamento { get; set; }
    [Display(Name = "Principal")] public bool Principal { get; set; }
    [Display(Name = "Enviar NF-e/NFS-e")] public bool EnviarNfe { get; set; }
    [Display(Name = "Enviar laudo")] public bool EnviarLaudo { get; set; }
    [Display(Name = "Inativo")] public bool Inativo { get; set; }
}
