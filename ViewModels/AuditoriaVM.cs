namespace LabControl.ViewModels;

/// <summary>Filtros da T06 (8.4). Sem data: os últimos 7 dias.</summary>
public sealed class AuditoriaFiltroVM
{
    public const int TamanhoPagina = 50;

    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    /// <summary>Login (parte).</summary>
    public string? Usuario { get; set; }
    /// <summary>Chave de <c>AuditoriaService.Assuntos</c>.</summary>
    public string? Assunto { get; set; }
    public string? Acao { get; set; }
    /// <summary>ID do registro ou texto dos dados (nº do documento, login…).</summary>
    public string? Registro { get; set; }
    /// <summary>Só ADMIN_GLOBAL: eventos de todas as empresas e logins sem empresa.</summary>
    public bool TodasEmpresas { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record AuditoriaItemVM(long Id, DateTime Quando, string? Usuario, string Acao, string Assunto, string Tabela, string IdRegistro,
    string? Link, string? Ip, string? Resumo, string? Empresa);

public sealed class AuditoriaListaVM
{
    public required AuditoriaFiltroVM Filtro { get; init; }
    public required IReadOnlyList<AuditoriaItemVM> Itens { get; init; }
    public int Total { get; init; }
    public bool AdminGlobal { get; init; }
    public int Paginas => Infra.Paginacao.Paginas(Total, AuditoriaFiltroVM.TamanhoPagina);
}

/// <summary>Uma coluna do "Ver diferença": antes × depois, marcada quando mudou.</summary>
public sealed record AuditoriaCampoVM(string Campo, string? Antes, string? Depois, bool Mudou);

public sealed class AuditoriaDetalheVM
{
    public required AuditoriaItemVM Evento { get; init; }
    public required IReadOnlyList<AuditoriaCampoVM> Campos { get; init; }
}

/// <summary>Aba "Histórico da amostra": o log das triggers (<c>LAB_HIST_AMOSTRAS_LOG</c>) de cada revisão da amostra física.</summary>
public sealed class HistoricoAmostraVM
{
    public string? Numero { get; init; }
    public string? Mensagem { get; init; }
    public IReadOnlyList<(string NrAmostra, IReadOnlyList<AmostraLogVM> Linhas)> Revisoes { get; init; } = [];
}
