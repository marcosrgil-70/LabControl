using LabControl.Infra;

namespace LabControl.ViewModels;

/// <summary>Filtros da consulta de resultados (T26 › Consulta).</summary>
public sealed class ResultadoFiltroVM
{
    public const int TamanhoPagina = 25;
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>Parte do nº da proposta.</summary>
    public string? Proposta { get; set; }
    /// <summary>Parte do nº da amostra.</summary>
    public string? Numero { get; set; }
    /// <summary>Data de entrega a partir de (padrão: hoje − DIAS_PESQUISA_PADRAO).</summary>
    public DateOnly? APartirDe { get; set; }
    public bool SoUltima { get; set; } = true;
    public int Pagina { get; set; } = 1;
    /// <summary>Aba aberta: consulta ou fila de análises.</summary>
    public bool Fila { get; set; }
}

public sealed record ResultadoListaItemVM(int Id, string NrAmostra, int Revisao, bool UltimaRevisao, DateOnly DtEntrega, string NrProposta,
    string Produto, string TipoAmostra, string TipoAnalise, string Cliente, int Parametros);

public sealed record TestePendenteVM(int IdAmostra, string NrAmostra, string Parametro, DateOnly? DtPrevista, int? DiasParaPrazo, string Cliente, string Produto);

public sealed class ResultadoListaVM
{
    public required ResultadoFiltroVM Filtro { get; init; }
    public required IReadOnlyList<ResultadoListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, ResultadoFiltroVM.TamanhoPagina);
    /// <summary>Fila de análises (testes sem resultado), por data prevista.</summary>
    public required IReadOnlyList<TestePendenteVM> Fila { get; init; }
    public int TotalFila { get; init; }
}

/// <summary>
/// Resultado em edição (T26): cabeçalho só leitura (amostra) e um cartão por teste da amostra. <see cref="Id"/> = 0 enquanto
/// a amostra ainda não tem resultado (o cabeçalho nasce na 1ª gravação).
/// </summary>
public sealed class ResultadoVM
{
    public int Id { get; set; }
    public int IdAmostra { get; set; }
    public List<ResultadoLinhaVM> Linhas { get; set; } = [];

    // só leitura (vindos do banco)
    public string? NrAmostra { get; set; }
    public string? Produto { get; set; }
    public string? TipoAmostra { get; set; }
    public string? TipoAnalise { get; set; }
    public string? Cliente { get; set; }
    public DateOnly DtEntrega { get; set; }
    public int Revisao { get; set; }
    public string? MotivoRevisao { get; set; }
    public bool UltimaRevisao { get; set; } = true;
    public bool TemBoletim { get; set; }
    public int? IdResultadoAnterior { get; set; }
    /// <summary>Motivo de só consulta (revisão antiga, boletim emitido); null = pode alterar.</summary>
    public string? Bloqueio { get; set; }
    public ResultadoOpcoesVM Opcoes { get; set; } = new();
}

/// <summary>Um teste da amostra: com resultado (<see cref="IdParam"/> &gt; 0), lançado agora (<see cref="Lancar"/>) ou pendente.</summary>
public sealed class ResultadoLinhaVM
{
    public int IdTeste { get; set; }
    public int IdParam { get; set; }
    public bool Lancar { get; set; }
    public string? Parametro { get; set; }
    public string? Metodo { get; set; }
    public bool Acreditado { get; set; }
    public bool NoBoletim { get; set; }

    public int? IdAnalista { get; set; }
    public DateOnly? DtInicial { get; set; }
    public DateOnly? DtResultado { get; set; }
    public string? QtAmostra { get; set; }
    public int? IdUnidadeAmostra { get; set; }

    public bool Numerico { get; set; } = true;
    public string? Simbolo { get; set; }
    public string? Valor { get; set; }
    public short? Expoente { get; set; }
    public int? IdUnidade { get; set; }
    public int? IdTipoResultado { get; set; }

    public bool EspecNumerica { get; set; } = true;
    public string? EspecSimbolo { get; set; }
    public string? EspecValor { get; set; }
    public short? EspecExpoente { get; set; }
    public int? EspecIdUnidade { get; set; }
    public int? EspecIdTipoResultado { get; set; }

    /// <summary>Conforme? Sim / Não / não informado.</summary>
    public bool? Conforme { get; set; }

    // prévia relida do banco
    public string? ResultadoFormatado { get; set; }
    public string? EspecFormatada { get; set; }
}

public sealed class ResultadoOpcoesVM
{
    public IReadOnlyList<OpcaoVM> Analistas { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Unidades { get; set; } = [];
    public IReadOnlyList<OpcaoVM> TiposResultado { get; set; } = [];
    /// <summary>Data do servidor (limite da data do resultado).</summary>
    public DateOnly Hoje { get; set; }
    /// <summary>Profissional do usuário, se executa análise (analista padrão do "Lançar todos").</summary>
    public int? AnalistaPadrao { get; set; }
    public static readonly IReadOnlyList<string> Simbolos = ["<", ">", "≤", "≥", "~"];
}
