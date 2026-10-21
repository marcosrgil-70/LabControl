using System.ComponentModel.DataAnnotations;
using LabControl.Infra;

namespace LabControl.ViewModels;

/// <summary>Filtro da consulta de produtos (T14).</summary>
public sealed class ProdutoFiltroVM
{
    public const int TamanhoPagina = 20;
    /// <summary>Parte do código ou da descrição.</summary>
    public string? Texto { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record ProdutoListaItemVM(int Id, string Codigo, string Descricao, string Unidade, string? Embalagem, string? QtdeEmbalagem, bool Inativo);

public sealed class ProdutoListaVM
{
    public required ProdutoFiltroVM Filtro { get; init; }
    public required IReadOnlyList<ProdutoListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    /// <summary>Inativos que a pesquisa acharia com "mostrar inativos" (aviso na lista).</summary>
    public int InativosOcultos { get; init; }
    public int Paginas => Paginacao.Paginas(Total, ProdutoFiltroVM.TamanhoPagina);
}

/// <summary>Produto = nome da amostra do cliente (ex.: "QUEIJO MINAS").</summary>
public sealed class ProdutoVM
{
    public int Id { get; set; }

    /// <summary>Vazio na inclusão → o banco gera (6 dígitos, sequência PRODUTO da empresa).</summary>
    [StringLength(10), Display(Name = "Código")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "Informe a descrição."), StringLength(150), Display(Name = "Descrição")]
    public string Descricao { get; set; } = "";

    [Required(ErrorMessage = "Escolha a unidade de estoque."), Display(Name = "Unidade de estoque")]
    public int? IdUnidades { get; set; }

    [Display(Name = "Embalagem")]
    public int? IdEmbalagensTipos { get; set; }

    /// <summary>Texto, para aceitar "2,5" (vírgula) ou "2.5".</summary>
    [Display(Name = "Qtde. por embalagem")]
    public string? QtdeEmbalagem { get; set; }

    [Display(Name = "Inativo")]
    public bool Inativo { get; set; }

    // combos (preenchidos pelo serviço): ativos + o já escolhido
    public IReadOnlyList<OpcaoVM> Unidades { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Embalagens { get; set; } = [];
}

/// <summary>Cadastro rápido "+" do produto (para a proposta, 4.2): só o essencial.</summary>
public sealed class ProdutoRapidoVM
{
    public string? Descricao { get; set; }
    public int? IdUnidades { get; set; }
}
