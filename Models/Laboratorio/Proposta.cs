using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabControl.Models.Entidades;

namespace LabControl.Models.Laboratorio;

[Table("LAB_PROPOSTAS")]
public class Proposta
{
    [Key]
    [Column("ID_LAB_PROPOSTAS")]
    public int Id { get; set; }

    [Column("ID_ENTIDADES")]
    public int IdEntidade { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [Column("COD_PROPOSTA")]
    public int CodProposta { get; set; }

    [Column("ANO_PROPOSTA")]
    public int AnoProposta { get; set; }

    [Column("REV_PROPOSTA")]
    public int RevProposta { get; set; } = 0;

    [Column("DT_SOLICITACAO")]
    public DateTime DtSolicitacao { get; set; } = DateTime.Now;

    [Column("DT_VALIDADE")]
    public DateTime? DtValidade { get; set; }

    [Column("ID_LAB_PROPOSTAS_STATUS")]
    public int? IdStatus { get; set; }

    [Column("VR_TOTAL_PROPOSTA")]
    public decimal VrTotal { get; set; }

    [Column("PORC_DESCONTO")]
    public decimal? PorcDesconto { get; set; }

    [Column("VR_DESCONTO")]
    public decimal? VrDesconto { get; set; }

    [Column("ID_ENTIDADES_FUNC")]
    public int? IdFuncionario { get; set; }

    [Column("ID_CONDICOES_PAGTOS")]
    public int? IdCondicaoPagamento { get; set; }

    [Column("DT_AUTORIZACAO")]
    public DateTime? DtAutorizacao { get; set; }

    [Column("ID_MOEDAS")]
    public int? IdMoeda { get; set; }

    [NotMapped]
    public string CodigoFormatado =>
        $"{CodProposta:D3}/{AnoProposta}-R{RevProposta}";

    [ForeignKey(nameof(IdEntidade))]
    public Entidade Entidade { get; set; } = null!;

    [ForeignKey(nameof(IdStatus))]
    public PropostaStatus? Status { get; set; }

    [ForeignKey(nameof(IdCondicaoPagamento))]
    public CondicaoPagamento? CondicaoPagamento { get; set; }

    [ForeignKey(nameof(IdMoeda))]
    public Moeda? Moeda { get; set; }

    public ICollection<PropostaAnalise> Analises { get; set; } = [];
}

[Table("LAB_PROPOSTAS_ANALISES")]
public class PropostaAnalise
{
    [Key]
    [Column("ID_LAB_PROPOSTAS_ANALISES")]
    public int Id { get; set; }

    [Column("ID_LAB_PROPOSTAS")]
    public int IdProposta { get; set; }

    [Column("ID_PRODUTOS")]
    public int? IdProduto { get; set; }

    [Column("ID_ANALISES_METODOS")]
    public int? IdAnaliseMetodo { get; set; }

    [Column("ID_ANALISES_TIPO")]
    public int? IdAnaliseTipo { get; set; }

    [Column("ID_LAB_PARAMETROS_ANALISES")]
    public int? IdParametroAnalise { get; set; }

    [Column("ID_IDIOMAS")]
    public int? IdIdioma { get; set; }

    [Column("QTDE_AMOSTRAS")]
    public int QtdeAmostras { get; set; } = 1;

    [Column("VR_UNITARIO")]
    public decimal VrUnitario { get; set; }

    [Column("VR_DESCONTO")]
    public decimal VrDesconto { get; set; }

    [Column("VR_TOTAL")]
    public decimal VrTotal { get; set; }

    [Column("ID_PRAZOS")]
    public int? IdPrazo { get; set; }

    [ForeignKey(nameof(IdProposta))]
    public Proposta Proposta { get; set; } = null!;

    [ForeignKey(nameof(IdProduto))]
    public Produto? Produto { get; set; }

    [ForeignKey(nameof(IdAnaliseMetodo))]
    public AnaliseMetodo? AnaliseMetodo { get; set; }

    [ForeignKey(nameof(IdAnaliseTipo))]
    public AnaliseTipo? AnaliseTipo { get; set; }

    [ForeignKey(nameof(IdParametroAnalise))]
    public ParametroAnalise? ParametroAnalise { get; set; }

    [ForeignKey(nameof(IdIdioma))]
    public Idioma? Idioma { get; set; }

    [ForeignKey(nameof(IdPrazo))]
    public Prazo? Prazo { get; set; }
}
