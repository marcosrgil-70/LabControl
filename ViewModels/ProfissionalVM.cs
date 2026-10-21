using LabControl.Infra;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LabControl.ViewModels;

public sealed class ProfissionalFiltroVM
{
    public const int TamanhoPagina = 20;
    public string? Texto { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record ProfissionalListaItemVM(int Id, string NomeCompleto, string? Cargo, string? Registro, bool AssinaBoletim,
    bool ExecutaAnalise, bool TemAssinatura, string? Login, bool Inativo);

public sealed class ProfissionalListaVM
{
    public required ProfissionalFiltroVM Filtro { get; init; }
    public required IReadOnlyList<ProfissionalListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int InativosOcultos { get; init; }
    public int Paginas => Paginacao.Paginas(Total, ProfissionalFiltroVM.TamanhoPagina);
}

/// <summary>Formulário da T13 (ENTIDADES categoria F + ENTIDADES_FUNCIONARIOS + vínculo com o usuário).</summary>
public sealed class ProfissionalVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Informe o nome."), StringLength(150), Display(Name = "Nome")] public string Nome { get; set; } = "";
    [StringLength(150), Display(Name = "Sobrenome")] public string? Sobrenome { get; set; }
    [StringLength(14), Display(Name = "CPF")] public string? Cpf { get; set; }
    [StringLength(20), Display(Name = "RG")] public string? Rg { get; set; }

    [Display(Name = "Cargo")] public int? IdCargosFuncionarios { get; set; }
    [Display(Name = "Tipo de registro")] public int? IdTiposRegProfissional { get; set; }
    [StringLength(25), Display(Name = "Nº do registro")] public string? NrRegistroProfissional { get; set; }
    [Display(Name = "UF do registro")] public string? UfRegistro { get; set; }
    [Display(Name = "Assina boletim (responsável)")] public bool AssinaBoletim { get; set; }
    [Display(Name = "Executa análise (analista)")] public bool ExecutaAnalise { get; set; } = true;

    [Display(Name = "Assinatura (PNG com fundo transparente, até 300 KB)")] public IFormFile? NovaAssinatura { get; set; }
    [Display(Name = "Remover a assinatura")] public bool RemoverAssinatura { get; set; }

    [Display(Name = "Usuário do sistema")] public int? IdUsuario { get; set; }
    [Display(Name = "Inativo")] public bool Inativo { get; set; }

    // exibição
    public bool TemAssinatura { get; set; }
    public IReadOnlyList<OpcaoVM> Cargos { get; set; } = [];
    public IReadOnlyList<OpcaoVM> TiposRegistro { get; set; } = [];
    public IReadOnlyList<string> Ufs { get; set; } = [];
    /// <summary>Usuários com acesso à empresa e sem profissional (ou o já vinculado a este).</summary>
    public IReadOnlyList<OpcaoVM> Usuarios { get; set; } = [];
}
