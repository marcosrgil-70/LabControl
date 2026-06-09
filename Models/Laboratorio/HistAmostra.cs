using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabControl.Models.Entidades;

namespace LabControl.Models.Laboratorio;

[Table("LAB_HIST_AMOSTRAS")]
public class HistAmostra
{
    [Key]
    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int Id { get; set; }

    [Column("ID_AMOSTRAS_TIPO")]
    public int IdAmostraTipo { get; set; }

    [Column("COD_AMOSTRA")]
    public int CodAmostra { get; set; }

    [Column("ID_ANALISES_TIPO")]
    public int? IdAnaliseTipo { get; set; }

    [Column("ANO_AMOSTRA")]
    public int AnoAmostra { get; set; }

    [Column("ID_ENTIDADES")]
    public int? IdEntidade { get; set; }

    [Column("NOME_CONTATO")]
    [StringLength(100)]
    public string? NomeContato { get; set; }

    [Column("ID_LAB_PROPOSTAS")]
    public int? IdProposta { get; set; }

    [Column("DT_ENTREGA")]
    public DateTime? DtEntrega { get; set; }

    [Column("HR_ENTREGA")]
    [StringLength(5)]
    public string? HrEntrega { get; set; }

    [Column("LOCAL_RECEBIMENTO")]
    [StringLength(100)]
    public string? LocalRecebimento { get; set; }

    [Column("ID_EMBALAGENS_TIPOS")]
    public int? IdEmbalagemTipo { get; set; }

    [Column("QTDE_EMBALAGENS_ENTREGUE")]
    public int? QtdeEmbalagensEntregue { get; set; }

    [Column("ID_PRODUTOS")]
    public int? IdProduto { get; set; }

    [Column("NR_LOTE")]
    [StringLength(50)]
    public string? NrLote { get; set; }

    [Column("DT_FABRICACAO")]
    public DateTime? DtFabricacao { get; set; }

    [Column("DT_VALIDADE")]
    public DateTime? DtValidade { get; set; }

    [Column("NOTA_ROTULO")]
    [StringLength(100)]
    public string? NotaRotulo { get; set; }

    [Column("ESPECIE_AMOSTRA")]
    [StringLength(100)]
    public string? EspecieAmostra { get; set; }

    [Column("ASPECTO_AMOSTRA")]
    [StringLength(100)]
    public string? AspectoAmostra { get; set; }

    [Column("COR")]
    [StringLength(50)]
    public string? Cor { get; set; }

    [Column("OUTRAS_CARACTERISTICAS")]
    [StringLength(200)]
    public string? OutrasCaracteristicas { get; set; }

    [Column("QTDE_AMOSTRA_VERIFICACAO")]
    public decimal? QtdeAmostraVerificacao { get; set; }

    [Column("TEMPERATURA_VERIFICACAO")]
    public decimal? TemperaturaVerificacao { get; set; }

    [Column("ACOMPANHA_FICHA_TECNICA")]
    public bool AcompanhaFichaTecnica { get; set; } = false;

    [Column("ACOMPANHA_PADRAO_ANALITICO")]
    public bool AcompanhaPadraoAnalitico { get; set; } = false;

    [Column("ACOMPANHA_CA_CLIENTE")]
    public bool AcompanhaCaCliente { get; set; } = false;

    [Column("ENVIAR_OUTRO_LABORATORIO")]
    public bool EnviarOutroLaboratorio { get; set; } = false;

    [Column("QTDE_ENVIO_OUTRO_LABORATORIO")]
    public int? QtdeEnvioOutroLaboratorio { get; set; }

    [Column("ID_AMOSTRAS_STATUS")]
    public int? IdAmostraStatus { get; set; }

    [Column("ID_ENTIDADES_FUNC_DIG")]
    public int? IdFuncionarioDigitador { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [NotMapped]
    public string CodigoFormatado =>
        $"{IdAmostraTipo}{CodAmostra:D3}{IdAnaliseTipo}{AnoAmostra}";

    // Navegação — ForeignKey aponta para a propriedade escalar correspondente
    [ForeignKey(nameof(IdAmostraTipo))]
    public AmostraTipo AmostraTipo { get; set; } = null!;

    [ForeignKey(nameof(IdAnaliseTipo))]
    public AnaliseTipo? AnaliseTipo { get; set; }

    [ForeignKey(nameof(IdEntidade))]
    public Entidade? Entidade { get; set; }

    [ForeignKey(nameof(IdProposta))]
    public Proposta? Proposta { get; set; }

    [ForeignKey(nameof(IdProduto))]
    public Produto? Produto { get; set; }

    [ForeignKey(nameof(IdEmbalagemTipo))]
    public EmbalagemTipo? EmbalagemTipo { get; set; }

    [ForeignKey(nameof(IdAmostraStatus))]
    public AmostraStatus? AmostraStatus { get; set; }

    public ICollection<HistAmostraTeste> Testes { get; set; } = [];
    public ICollection<MovAmostra> Movimentacoes { get; set; } = [];
    public HistAmostraSaldo? Saldo { get; set; }
    public LocalAmostra? LocalizacaoAtual { get; set; }
}

[Table("LAB_HIST_AMOSTRAS_TESTES")]
public class HistAmostraTeste
{
    [Key]
    [Column("ID_LAB_HIST_AMOSTRAS_TESTES")]
    public int Id { get; set; }

    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int IdHistAmostra { get; set; }

    [Column("ID_ANALISES_TIPO")]
    public int? IdAnaliseTipo { get; set; }

    [Column("ID_ANALISES_METODOS")]
    public int? IdAnaliseMetodo { get; set; }

    [Column("ID_IDIOMAS")]
    public int? IdIdioma { get; set; }

    [Column("ID_PRAZOS")]
    public int? IdPrazo { get; set; }

    [Column("ID_ENTIDADES")]
    public int? IdEntidade { get; set; }

    [Column("ID_LAB_PARAMETROS_ANALISES")]
    public int? IdParametroAnalise { get; set; }

    [ForeignKey(nameof(IdHistAmostra))]
    public HistAmostra HistAmostra { get; set; } = null!;

    [ForeignKey(nameof(IdAnaliseTipo))]
    public AnaliseTipo? AnaliseTipo { get; set; }

    [ForeignKey(nameof(IdAnaliseMetodo))]
    public AnaliseMetodo? AnaliseMetodo { get; set; }

    [ForeignKey(nameof(IdIdioma))]
    public Idioma? Idioma { get; set; }

    [ForeignKey(nameof(IdPrazo))]
    public Prazo? Prazo { get; set; }

    [ForeignKey(nameof(IdParametroAnalise))]
    public ParametroAnalise? ParametroAnalise { get; set; }
}

[Table("LAB_HIST_AMOSTRAS_SALDO")]
public class HistAmostraSaldo
{
    [Key]
    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int IdHistAmostra { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [Column("SALDO_ATUAL")]
    public decimal SaldoAtual { get; set; }

    [Column("DATA_ATUALIZACAO")]
    public DateTime DataAtualizacao { get; set; }

    public HistAmostra HistAmostra { get; set; } = null!;
}

[Table("LAB_MOV_AMOSTRAS")]
public class MovAmostra
{
    [Key]
    [Column("ID_LAB_MOV_AMOSTRAS")]
    public int Id { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int IdHistAmostra { get; set; }

    [Column("ID_ENTIDADES_FUNC")]
    public int? IdFuncionario { get; set; }

    [Column("DATA_MOV")]
    public DateTime DataMov { get; set; } = DateTime.Now;

    [Column("QTDE")]
    public decimal Qtde { get; set; }

    [Column("E_S")]
    [StringLength(1)]
    public string EntradaSaida { get; set; } = "E";

    [Column("JUSTIFICATIVA")]
    [StringLength(200)]
    public string? Justificativa { get; set; }

    [Column("AMOSTRA_COMPLEMENTAR")]
    public bool AmostraComplementar { get; set; } = false;

    [ForeignKey(nameof(IdHistAmostra))]
    public HistAmostra HistAmostra { get; set; } = null!;
}

[Table("LAB_LOCAL_AMOSTRAS")]
public class LocalAmostra
{
    [Key]
    [Column("ID_LAB_LOCAL_AMOSTRAS")]
    public int Id { get; set; }

    [Column("ID_LAB_HIST_AMOSTRAS")]
    public int IdHistAmostra { get; set; }

    [Column("ID_EMPRESAS")]
    public int IdEmpresa { get; set; }

    [Column("LOCAL")]
    [StringLength(100)]
    public string? Local { get; set; }

    [Column("ID_FUNCIONARIO_DESCARTE")]
    public int? IdFuncionarioDescarte { get; set; }

    [Column("DT_HR_DESCARTE")]
    public DateTime? DtHrDescarte { get; set; }

    [ForeignKey(nameof(IdHistAmostra))]
    public HistAmostra HistAmostra { get; set; } = null!;
}
