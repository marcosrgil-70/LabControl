using LabControl.Models.Laboratorio;

namespace LabControl.Models;

// ─── Amostras por Período ─────────────────────────────────────────────────

public class FiltroAmostrasVM
{
    public DateTime? DtInicio { get; set; }
    public DateTime? DtFim { get; set; }
    public int? IdStatus { get; set; }
}

public class RelAmostraItem
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string TipoAmostra { get; set; } = string.Empty;
    public string? TipoAnalise { get; set; }
    public string? Produto { get; set; }
    public string? NrLote { get; set; }
    public DateTime? DtEntrega { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CorStatus { get; set; }
    public decimal? Saldo { get; set; }
}

public class RelAmostrasVM
{
    public FiltroAmostrasVM Filtro { get; set; } = new();
    public List<AmostraStatus> StatusOpcoes { get; set; } = [];
    public List<RelAmostraItem>? Resultado { get; set; }
}

// ─── Propostas por Período ────────────────────────────────────────────────

public class FiltroPropostasVM
{
    public DateTime? DtInicio { get; set; }
    public DateTime? DtFim { get; set; }
    public int? IdStatus { get; set; }
}

public class RelPropostaItem
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime DtSolicitacao { get; set; }
    public DateTime? DtValidade { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CorStatus { get; set; }
    public decimal VrTotal { get; set; }
    public string? Moeda { get; set; }
    public int QtdItens { get; set; }
}

public class RelPropostasVM
{
    public FiltroPropostasVM Filtro { get; set; } = new();
    public List<PropostaStatus> StatusOpcoes { get; set; } = [];
    public List<RelPropostaItem>? Resultado { get; set; }
}
