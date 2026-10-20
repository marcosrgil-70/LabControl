using LabControl.Services.Auxiliares;

namespace LabControl.ViewModels;

/// <summary>Filtro da lista de um auxiliar (busca, inativos, página).</summary>
public sealed class AuxiliarFiltroVM
{
    public const int TamanhoPagina = 20;
    public string? Busca { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;
}

/// <param name="Valores">Um por coluna da lista, já formatado (lógico "Sim"/"Não", opção pelo nome, cor "#RRGGBB").</param>
public sealed record AuxiliarLinhaVM(int Id, bool Ativo, IReadOnlyList<string?> Valores);

/// <summary>Campo do formulário já resolvido (rótulo, tipo, tamanho e obrigatoriedade vindos do modelo).</summary>
public sealed record CampoFormVM(string Nome, string Rotulo, TipoCampo Tipo, int? Tamanho, bool Obrigatorio,
    string? Ajuda, int? Minimo, int? Maximo, bool Maiusculas, IReadOnlyList<OpcaoVM> Opcoes, bool Bloqueado);

/// <summary>Tela de um auxiliar: lista + (quando houver erro ao gravar) o formulário reaberto no modal.</summary>
public sealed class AuxiliarListaVM
{
    public required AuxiliarDefinicao Definicao { get; init; }
    public required AuxiliarFiltroVM Filtro { get; init; }
    public required IReadOnlyList<CampoAuxiliar> Colunas { get; init; }
    public required IReadOnlyList<AuxiliarLinhaVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Math.Max(1, (Total + AuxiliarFiltroVM.TamanhoPagina - 1) / AuxiliarFiltroVM.TamanhoPagina);
    public AuxiliarFormVM? Form { get; set; }
    /// <summary>Registro que não pôde ser excluído por estar em uso: a tela oferece "Inativar".</summary>
    public int? OferecerInativar { get; set; }
}

/// <summary>Formulário genérico: valores por nome de propriedade (<c>Valores[Descricao]</c>).</summary>
public sealed class AuxiliarFormVM
{
    public int Id { get; set; }
    public bool Ativo { get; set; } = true;
    public Dictionary<string, string?> Valores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // filtro da lista, para voltar ao mesmo ponto depois de gravar
    public string? Busca { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;

    // exibição (preenchidos pelo serviço)
    public string Tabela { get; set; } = "";
    public string Singular { get; set; } = "";
    public IReadOnlyList<CampoFormVM> Campos { get; set; } = [];
    public Dictionary<string, string> Erros { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Erro { get; set; }
}
