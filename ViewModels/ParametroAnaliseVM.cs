using System.ComponentModel.DataAnnotations;
using LabControl.Infra;

namespace LabControl.ViewModels;

/// <summary>Filtro da consulta de parâmetros de análise (T15).</summary>
public sealed class ParametroAnaliseFiltroVM
{
    public const int TamanhoPagina = 20;
    /// <summary>Parte da descrição ou da descrição reduzida.</summary>
    public string? Texto { get; set; }
    public int? IdAnalisesTipo { get; set; }
    /// <summary>"sim", "nao" ou vazio (todos).</summary>
    public string? Acreditado { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record ParametroAnaliseListaItemVM(int Id, string Descricao, string DescReduzida, string Tipo, string? Metodo,
    string VrUnitario, bool Acreditado, bool Inativo);

public sealed class ParametroAnaliseListaVM
{
    public required ParametroAnaliseFiltroVM Filtro { get; init; }
    public required IReadOnlyList<ParametroAnaliseListaItemVM> Itens { get; init; }
    public IReadOnlyList<OpcaoVM> Tipos { get; init; } = [];
    public int Total { get; init; }
    /// <summary>Inativos que a pesquisa acharia com "mostrar inativos" (aviso na lista).</summary>
    public int InativosOcultos { get; init; }
    public int Paginas => Paginacao.Paginas(Total, ParametroAnaliseFiltroVM.TamanhoPagina);
}

/// <summary>Parâmetro analisado (ex.: "CONTAGEM TOTAL DE BACTÉRIAS"): proposta, testes, resultados e boletim.</summary>
public sealed class ParametroAnaliseVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o parâmetro."), StringLength(150), Display(Name = "Parâmetro")]
    public string Descricao { get; set; } = "";

    [Required(ErrorMessage = "Informe a descrição reduzida (é o texto impresso no boletim)."), StringLength(150), Display(Name = "Descrição reduzida")]
    public string DescReduzida { get; set; } = "";

    [Required(ErrorMessage = "Escolha o tipo de análise."), Display(Name = "Tipo de análise")]
    public int? IdAnalisesTipo { get; set; }

    [Display(Name = "Método padrão")]
    public int? IdAnalisesMetodos { get; set; }

    [StringLength(150), Display(Name = "Descrição do método")]
    public string? DescrMetodo { get; set; }

    /// <summary>Texto em reais ("1.234,56" ou "1234.56").</summary>
    [Display(Name = "Valor unitário (R$)")]
    public string? VrUnitario { get; set; }

    [Display(Name = "Acreditado (INMETRO)")]
    public bool Auditado { get; set; }

    [Display(Name = "Inativo")]
    public bool Inativo { get; set; }

    // combos (preenchidos pelo serviço): ativos + o já escolhido
    public IReadOnlyList<OpcaoVM> Tipos { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Metodos { get; set; } = [];
}
