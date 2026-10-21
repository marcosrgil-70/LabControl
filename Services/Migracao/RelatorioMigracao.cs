namespace LabControl.Services.Migracao;

public enum SituacaoEtapa { Pendente, Executando, Ok, Erro }

public sealed record EstadoEtapa(string Codigo, string Nome)
{
    public SituacaoEtapa Situacao { get; set; } = SituacaoEtapa.Pendente;
    public int Gravados { get; set; }
    public TimeSpan Tempo { get; set; }
    public string? Erro { get; set; }
}

/// <summary>Esperado (lido no Firebird) × encontrado (no destino) — verde quando iguais.</summary>
public sealed record Conferencia(string Etapa, string Item, string Esperado, string Encontrado)
{
    public bool Ok => Esperado == Encontrado;
}

public sealed record Anomalia(string Etapa, string Texto);

/// <summary>
/// Andamento e resultado de uma execução (T07): etapas, log, anomalias e conferências. Lido pela tela enquanto a
/// migração roda em segundo plano — todo acesso passa pelo lock e a tela recebe cópias (<see cref="Copia"/>).
/// </summary>
public sealed class RelatorioMigracao
{
    private readonly Lock _lock = new();
    private readonly List<string> _log = [];
    private readonly List<Anomalia> _anomalias = [];
    private readonly List<Conferencia> _conferencias = [];

    public RelatorioMigracao(string destino, IEnumerable<EstadoEtapa> etapas) => (Destino, Etapas) = (destino, etapas.ToList());

    public string Destino { get; }
    public IReadOnlyList<EstadoEtapa> Etapas { get; }
    public DateTime Inicio { get; } = DateTime.Now;
    public DateTime? Fim { get; private set; }
    public bool Terminou => Fim is not null;
    public bool Sucesso => Terminou && Etapas.All(e => e.Situacao == SituacaoEtapa.Ok);

    public void Log(string texto) { lock (_lock) _log.Add($"{DateTime.Now:HH:mm:ss} {texto}"); }
    public void Anomalia(string etapa, string texto) { lock (_lock) _anomalias.Add(new(etapa, texto)); }
    public void Conferir(string etapa, string item, object esperado, object encontrado)
    {
        lock (_lock) _conferencias.Add(new(etapa, item, Convert.ToString(esperado)!, Convert.ToString(encontrado)!));
    }

    public void Concluir() { lock (_lock) Fim = DateTime.Now; }

    public (IReadOnlyList<string> Log, IReadOnlyList<Anomalia> Anomalias, IReadOnlyList<Conferencia> Conferencias) Copia()
    {
        lock (_lock) return (_log.ToList(), _anomalias.ToList(), _conferencias.ToList());
    }
}
