using LabControl.Infra;
namespace LabControl.ViewModels;

/// <summary>Filtro das listas de países, estados e cidades (T11-01…03).</summary>
public sealed class GeoFiltroVM
{
    public const int TamanhoPagina = 50;
    public string? Busca { get; set; }
    /// <summary>Estados: país; cidades: país do combo de UF.</summary>
    public int? IdPais { get; set; }
    /// <summary>Cidades: UF.</summary>
    public int? IdEstado { get; set; }
    public int Pagina { get; set; } = 1;
}

/// <summary>Formulário único de país/estado/cidade (cada tela usa os campos que tem).</summary>
public sealed class GeoFormVM
{
    public int Id { get; set; }
    /// <summary>Estado: país; cidade: estado.</summary>
    public int? IdPai { get; set; }
    public string? Nome { get; set; }
    public string? Sigla { get; set; }
    /// <summary>País: código BACEN; estado/cidade: código IBGE.</summary>
    public string? Codigo { get; set; }

    // filtro da lista, para voltar ao mesmo ponto depois de gravar
    public string? Busca { get; set; }
    public int? IdPais { get; set; }
    public int? IdEstado { get; set; }
    public int Pagina { get; set; } = 1;

    public Dictionary<string, string> Erros { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Erro { get; set; }
}

/// <param name="Pai">Estado: nome do país; cidade: sigla da UF.</param>
/// <param name="Filhos">País: nº de estados; estado: nº de cidades; cidade: nº de endereços que a usam.</param>
public sealed record GeoLinhaVM(int Id, int? IdPai, string? Pai, string Nome, string? Sigla, string? Codigo, int Filhos);

/// <summary>Uma das telas de geografia: lista + (gravação com erro) o formulário reaberto no modal.</summary>
public sealed class GeoListaVM
{
    /// <summary>Paises, Estados ou Cidades (nome da action).</summary>
    public required string Tela { get; init; }
    public required GeoFiltroVM Filtro { get; init; }
    public required IReadOnlyList<GeoLinhaVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, GeoFiltroVM.TamanhoPagina);
    /// <summary>Combos: países (estados e filtro de cidades) e estados do país (cidades).</summary>
    public IReadOnlyList<OpcaoVM> Paises { get; init; } = [];
    public IReadOnlyList<OpcaoVM> Estados { get; init; } = [];
    public GeoFormVM? Form { get; set; }
    public ImportacaoIbgeVM? Importacao { get; set; }
}

/// <summary>Resultado da importação do CSV de municípios do IBGE.</summary>
public sealed record ImportacaoIbgeVM(int Lidas, int Incluidas, int Atualizadas, int Iguais, IReadOnlyList<string> Erros)
{
    public const int MaximoErros = 20;
}
