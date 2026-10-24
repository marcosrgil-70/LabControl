namespace LabControl.ViewModels;

/// <summary>Filtros da T25: cliente ou nº da amostra, entrega a partir de (padrão hoje − 7), só última revisão.</summary>
public sealed class EtiquetaFiltroVM
{
    public string? Cliente { get; set; }
    public string? Numero { get; set; }
    public DateOnly? APartirDe { get; set; }
    public bool SoUltima { get; set; } = true;
    /// <summary>Amostra que veio do recebimento (T22): já marcada, com a quantidade de embalagens.</summary>
    public int? Amostra { get; set; }
}

/// <summary>Linha do grid: dados que vão na etiqueta e a quantidade sugerida (= embalagens entregues).</summary>
public sealed record EtiquetaLinhaVM(int Id, string NrAmostra, string Cliente, string Produto, DateOnly DtEntrega, string? NrPedido,
    string? Fabricacao, string? Validade, int Quantidade);

public sealed class EtiquetaPaginaVM
{
    public required EtiquetaFiltroVM Filtro { get; init; }
    public required IReadOnlyList<EtiquetaLinhaVM> Itens { get; init; }
    /// <summary>Impressoras instaladas no servidor (a última usada fica marcada pelo navegador).</summary>
    public required IReadOnlyList<string> Impressoras { get; init; }
}

/// <summary>Pedido de impressão: impressora escolhida, sigilo do produto e as amostras marcadas com a quantidade.</summary>
public sealed class EtiquetaImprimirVM
{
    public string? Impressora { get; set; }
    /// <summary>"Não imprimir o nome do produto" (sigilo, legado).</summary>
    public bool SemProduto { get; set; }
    public List<EtiquetaItemVM> Itens { get; set; } = [];
}

public sealed class EtiquetaItemVM
{
    public int Id { get; set; }
    public bool Marcada { get; set; }
    public int Quantidade { get; set; }
}

/// <summary>O que vai impresso numa etiqueta (T25, P19).</summary>
public sealed record EtiquetaDados(string NrAmostra, string? Produto, string? NrPedido, DateOnly DtEntrega, string? Fabricacao, string? Validade);
