using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models.Laboratorio;

[Table("AMOSTRAS_TIPO")]
public class AmostraTipo
{
    [Key] [Column("ID_AMOSTRAS_TIPO")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
}

[Table("AMOSTRAS_STATUS")]
public class AmostraStatus
{
    [Key] [Column("ID_AMOSTRAS_STATUS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("COR")] [StringLength(20)] public string? Cor { get; set; }
}

[Table("ANALISES_TIPO")]
public class AnaliseTipo
{
    [Key] [Column("ID_ANALISES_TIPO")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
}

[Table("ANALISES_STATUS")]
public class AnaliseStatus
{
    [Key] [Column("ID_ANALISES_STATUS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("COR")] [StringLength(20)] public string? Cor { get; set; }
}

[Table("ANALISES_METODOS")]
public class AnaliseMetodo
{
    [Key] [Column("ID_ANALISES_METODOS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(150)] public string Descricao { get; set; } = string.Empty;
}

[Table("BOLETINS_STATUS")]
public class BoletimStatus
{
    [Key] [Column("ID_BOLETINS_STATUS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("COR")] [StringLength(20)] public string? Cor { get; set; }
}

[Table("IDIOMAS")]
public class Idioma
{
    [Key] [Column("ID_IDIOMAS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(50)] public string Descricao { get; set; } = string.Empty;
}

[Table("PRAZOS")]
public class Prazo
{
    [Key] [Column("ID_PRAZOS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("QTDE")] public int QtdeDias { get; set; }
}

[Table("MOEDAS")]
public class Moeda
{
    [Key] [Column("ID_MOEDAS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(50)] public string Descricao { get; set; } = string.Empty;
    [Column("SIGLA")] [StringLength(5)] public string Sigla { get; set; } = string.Empty;
}

[Table("UNIDADES")]
public class Unidade
{
    [Key] [Column("ID_UNIDADES")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(50)] public string Descricao { get; set; } = string.Empty;
    [Column("SIGLA")] [StringLength(10)] public string Sigla { get; set; } = string.Empty;
}

[Table("EMBALAGENS_TIPOS")]
public class EmbalagemTipo
{
    [Key] [Column("ID_EMBALAGENS_TIPOS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
}

[Table("LAB_PROPOSTAS_STATUS")]
public class PropostaStatus
{
    [Key] [Column("ID_LAB_PROPOSTAS_STATUS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("COR")] [StringLength(20)] public string? Cor { get; set; }
}

[Table("LAB_TIPOS_RESULTADOS")]
public class TipoResultado
{
    [Key] [Column("ID_LAB_TIPOS_RESULTADOS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
}

[Table("CONDICOES_PAGTOS")]
public class CondicaoPagamento
{
    [Key] [Column("ID_CONDICOES_PAGTOS")] public int Id { get; set; }
    [Column("CODIGO")] [StringLength(10)] public string Codigo { get; set; } = string.Empty;
    [Column("DESCRICAO")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
}

[Table("PRODUTOS")]
public class Produto
{
    [Key] [Column("ID_PRODUTOS")] public int Id { get; set; }
    [Column("CODIGO")] [StringLength(20)] public string Codigo { get; set; } = string.Empty;
    [Column("DESCRICAO")] [StringLength(150)] public string Descricao { get; set; } = string.Empty;
    [Column("ID_UNIDADES")] public int? IdUnidade { get; set; }
    [Column("ID_EMBALAGENS_TIPOS")] public int? IdEmbalagemTipo { get; set; }
    [Column("QTDE_EMBALAGEM")] public decimal? QtdeEmbalagem { get; set; }

    [ForeignKey(nameof(IdUnidade))]
    public Unidade? Unidade { get; set; }

    [ForeignKey(nameof(IdEmbalagemTipo))]
    public EmbalagemTipo? EmbalagemTipo { get; set; }
}

[Table("LAB_PARAMETROS_ANALISES")]
public class ParametroAnalise
{
    [Key] [Column("ID_LAB_PARAMETROS_ANALISES")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(150)] public string Descricao { get; set; } = string.Empty;
    [Column("ID_ANALISES_TIPO")] public int? IdAnaliseTipo { get; set; }
    [Column("VR_UNITARIO", TypeName = "decimal(15,4)")] public decimal VrUnitario { get; set; }

    [ForeignKey(nameof(IdAnaliseTipo))]
    public AnaliseTipo? AnaliseTipo { get; set; }
}
