using LabControl.Infra;

namespace LabControl.ViewModels;

/// <summary>Filtros da consulta de boletins (T27 › Consulta).</summary>
public sealed class BoletimFiltroVM
{
    public const int TamanhoPagina = 25;
    /// <summary>Nome do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>Parte do nº da amostra ou do código do boletim.</summary>
    public string? Numero { get; set; }
    /// <summary>Data do boletim a partir de (padrão: hoje − DIAS_PESQUISA_PADRAO).</summary>
    public DateOnly? APartirDe { get; set; }
    /// <summary>null = todos; true = só emitidos; false = só não emitidos.</summary>
    public bool? Emitido { get; set; }
    public bool SoUltima { get; set; } = true;
    public int Pagina { get; set; } = 1;
}

public sealed record BoletimListaItemVM(int Id, string NrAmostra, string CodBoletim, int Revisao, bool UltimaRevisao, DateOnly DtBoletim,
    DateTime DhGeracao, DateTime? DhEmissao, string Produto, string Cliente, string? Status);

public sealed class BoletimListaVM
{
    public required BoletimFiltroVM Filtro { get; init; }
    public required IReadOnlyList<BoletimListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, BoletimFiltroVM.TamanhoPagina);
}

/// <summary>
/// Boletim em edição (T27). <see cref="Id"/> = 0 na inclusão: o cabeçalho e os parâmetros marcados nascem juntos na
/// 1ª gravação; depois a seleção, a ordem e a exclusão de parâmetros são ações próprias.
/// </summary>
public sealed class BoletimVM
{
    public int Id { get; set; }
    public int IdAmostra { get; set; }

    // editáveis
    public DateOnly? DtBoletim { get; set; }
    public int? IdStatus { get; set; }
    public int? IdResponsavel1 { get; set; }
    public int? IdResponsavel2 { get; set; }
    public string? Observacao { get; set; }
    public string? MetodologiaAcreditada { get; set; }
    public string? MetodologiaNaoAcreditada { get; set; }
    public string? Legenda { get; set; }
    /// <summary>Parâmetros do resultado marcados na inclusão (IDs de LAB_RESULTADOS_PARAM).</summary>
    public List<int> Selecionados { get; set; } = [];

    // só leitura (vindos do banco)
    public string? NrAmostra { get; set; }
    public string? Produto { get; set; }
    public string? Lote { get; set; }
    public string? Cliente { get; set; }
    public string? CodBoletim { get; set; }
    public int Revisao { get; set; }
    public string? MotivoRevisao { get; set; }
    public bool UltimaRevisao { get; set; } = true;
    public int IdResultado { get; set; }
    public int RevisaoResultado { get; set; }
    public DateTime? DhEmissao { get; set; }
    public string? Emissor { get; set; }
    public bool TemArquivo { get; set; }
    /// <summary>Motivo de só consulta (emitido, revisão antiga); null = pode alterar.</summary>
    public string? Bloqueio { get; set; }
    public bool ExigeResponsavel2 { get; set; }
    public IReadOnlyList<ResultadoDisponivelVM> Disponiveis { get; set; } = [];
    public IReadOnlyList<ParametroBoletimVM> Parametros { get; set; } = [];
    public BoletimOpcoesVM Opcoes { get; set; } = new();
}

/// <summary>Parâmetro do resultado vinculado que ainda não está no boletim (grid da esquerda).</summary>
public sealed record ResultadoDisponivelVM(int IdParam, string Parametro, bool Acreditado, DateOnly DtResultado, string? Resultado, string? Descritivo,
    string? Unidade, string? QtAmostra);

/// <summary>Parâmetro do boletim (grid da direita), com o texto congelado.</summary>
public sealed record ParametroBoletimVM(int Id, int Ordem, string Parametro, string? Resultado, string? Descritivo, string? Unidade,
    string? Especificacao, bool Acreditado);

public sealed class BoletimOpcoesVM
{
    public IReadOnlyList<OpcaoVM> Responsaveis { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Status { get; set; } = [];
}
