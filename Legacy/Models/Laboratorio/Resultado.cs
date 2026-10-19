using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models.Laboratorio;

[Table("LAB_RESULTADOS_ANALISES")]
public class ResultadoAnalise
{
    [Key]
    [Column("ID_LAB_RESULTADOS_ANALISES")]
    public int Id { get; set; }

    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int IdHistAmostra { get; set; }

    [Column("ID_ENTIDADES_FUNC_DIG")]
    public int? IdFuncionarioDigitador { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [Column("REVISAO")]
    public int Revisao { get; set; } = 0;

    [Column("DT_RESULTADO")]
    public DateTime? DtResultado { get; set; }

    [ForeignKey(nameof(IdHistAmostra))]
    public HistAmostra HistAmostra { get; set; } = null!;

    public ICollection<ResultadoParam> Parametros { get; set; } = [];
}

[Table("LAB_RESULTADOS_PARAM")]
public class ResultadoParam
{
    [Key]
    [Column("ID_LAB_RESULTADOS_PARAM")]
    public int Id { get; set; }

    [Column("ID_LAB_RESULTADOS_ANALISES")]
    public int IdResultadoAnalise { get; set; }

    [Column("ID_LAB_HIST_AMOSTRAS_TESTES")]
    public int? IdHistAmostraTeste { get; set; }

    [Column("DT_RESULTADO")]
    public DateTime? DtResultado { get; set; }

    [Column("TIPO_RESULTADO")]
    [StringLength(1)]
    public string? TipoResultado { get; set; }

    [Column("ID_UNIDADES")]
    public int? IdUnidade { get; set; }

    [Column("VR_SATISFEITO")]
    public bool? VrSatisfeito { get; set; }

    [Column("ID_ENTIDADES_FUNC_ANALISE")]
    public int? IdFuncionarioAnalise { get; set; }

    [Column("QT_AMOSTRA")]
    public decimal? QtAmostra { get; set; }

    [Column("VR_RESULTADO")]
    [StringLength(100)]
    public string? VrResultado { get; set; }

    [Column("VR_RESULTADO_ESPECIF")]
    [StringLength(100)]
    public string? VrResultadoEspecif { get; set; }

    [Column("SIMBOLO_GRANDEZA")]
    [StringLength(20)]
    public string? SimboloGrandeza { get; set; }

    [ForeignKey(nameof(IdResultadoAnalise))]
    public ResultadoAnalise ResultadoAnalise { get; set; } = null!;

    [ForeignKey(nameof(IdHistAmostraTeste))]
    public HistAmostraTeste? HistAmostraTeste { get; set; }

    [ForeignKey(nameof(IdUnidade))]
    public Unidade? Unidade { get; set; }
}
