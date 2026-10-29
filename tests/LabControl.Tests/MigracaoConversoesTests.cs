using LabControl.Models;
using LabControl.Services.Migracao;

namespace LabControl.Tests;

/// <summary>Conversões dos valores do legado usadas pela migração (3.5).</summary>
public class MigracaoConversoesTests
{
    [Theory]
    [InlineData("65280", "#00FF00")]      // clLime (BGR: verde no byte do meio)
    [InlineData("255", "#FF0000")]        // clRed (R = byte baixo)
    [InlineData("16711680", "#0000FF")]   // clBlue
    [InlineData("12632256", "#C0C0C0")]   // clSilver
    [InlineData("-16777214", null)]       // cor de sistema
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Cor_tcolor_para_hex(string? tcolor, string? esperado) => Assert.Equal(esperado, Conversoes.Cor(tcolor));

    [Fact]
    public void Estado_civil_sexo_flag_e_documento()
    {
        Assert.Equal([EstadoCivil.Solteiro, EstadoCivil.Casado, EstadoCivil.Divorciado, EstadoCivil.Separado, EstadoCivil.Viuvo],
            Enumerable.Range(0, 5).Select(i => Conversoes.EstadoCivil(i)!.Value));
        Assert.Null(Conversoes.EstadoCivil(7));
        Assert.Equal((Sexo?)Sexo.Feminino, Conversoes.Sexo(" f "));
        Assert.True(Conversoes.Flag("S"));
        Assert.False(Conversoes.Flag(null));
        Assert.Equal("529.982.247-25", Conversoes.Documento("52998224725"));
        Assert.Equal("11.222.333/0001-81", Conversoes.Documento("11.222.333/0001-81"));
        Assert.Null(Conversoes.Documento("  "));
    }

    [Fact]
    public void Nome_de_cidade_comparavel_e_email()
    {
        Assert.Equal(Conversoes.NomeComparavel("São João d'Aliança"), Conversoes.NomeComparavel("SAO JOAO D ALIANCA"));
        Assert.Equal(Conversoes.NomeComparavel("Santa Bárbara D'Oeste"), Conversoes.NomeComparavel("SANTA BARBARA D  OESTE"));
        Assert.Equal("contato@empax.com.br", Conversoes.Email("Contato@Empax.com.br, www.empax.com.br"));
        Assert.Equal("a@b.com", Conversoes.Email("<a@b.com>;"));
        Assert.Null(Conversoes.Email("sem arroba"));
    }

    // RTF do TRichEdit do legado (amostras reais de LAB_PROPOSTAS_CONTRATOS/_OBSERVACAO e LAB_HIST_AMOSTRAS_OBSERVACAO)
    private const string Cabecalho = @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fnil\fcharset0 Calibri;}}" + "\r\n" + @"\viewkind4\uc1\pard\lang1046\f0\fs18 ";

    [Fact]
    public void Rtf_do_legado_para_html()
    {
        Assert.Equal("<p>Conforme contrato 1007/2017.</p>", Conversoes.RtfParaHtml(Cabecalho + "Conforme contrato 1007/2017.\\par\r\n}"));
        Assert.Null(Conversoes.RtfParaHtml(Cabecalho + "\\tab\\par\r\n}"));   // só tab e parágrafo vazio
        Assert.Null(Conversoes.RtfParaHtml(Cabecalho + "\\par\r\n}"));
        Assert.Equal("<p>An\u00e1lise &amp; <strong>pre\u00e7o</strong></p><p><br></p><p>fim</p>",
            Conversoes.RtfParaHtml(Cabecalho + @"An\'e1lise & \b pre\'e7o\b0\par\par fim\par" + "\r\n}"));
        Assert.Equal("<p>\u201ccita\u00e7\u00e3o\u201d \u20ac</p>", Conversoes.RtfParaHtml(Cabecalho + @"\'93cita\u231?\'e3o\'94 \'80\par}"));
        Assert.Equal("<p>linha 1</p><p>linha 2</p>", Conversoes.RtfParaHtml("linha 1\r\nlinha 2"));
        Assert.Null(Conversoes.RtfParaHtml("  "));
        Assert.Throws<FormatException>(() => Conversoes.RtfParaHtml(@"{\rtf1 texto \"));
    }

    [Theory]
    [InlineData(15, 3, 2017, 15, 3, 2017)]
    [InlineData(0, 3, 2017, null, 3, 2017)]
    [InlineData(0, 0, 2017, null, null, 2017)]
    [InlineData(10, 0, 2017, null, null, 2017)]      // dia sem mês
    [InlineData(10, 5, 0, null, null, null)]         // dia e mês sem ano
    [InlineData(31, 4, 2017, null, 4, 2017)]         // 31/04
    [InlineData(29, 2, 2016, 29, 2, 2016)]
    [InlineData(5, 13, 2017, null, null, 2017)]      // mês 13
    public void Data_parcial_do_legado_no_formato_da_trigger(int? dia, int? mes, int? ano, int? d, int? m, int? a) =>
        Assert.Equal((d, m, a), Conversoes.DataParcial(dia, mes, ano));

    [Theory]
    [InlineData("Boletim", "BOLETIM")]
    [InlineData("Relatorio", "RELATORIO")]
    [InlineData("RELATÓRIO", "RELATORIO")]
    [InlineData(null, "BOLETIM")]
    public void Tipo_de_documento_do_legado(string? legado, string esperado) =>
        Assert.Equal(esperado, LabControl.Services.Migracao.Etapas.M12ProdutosAnalisesProposta.TipoDocumento(legado));

    // pendência 36: os casos reais do legado (2017) e os que não podem ser corrigidos
    [Theory]
    [InlineData("0171-10-04", "2017-10-09", "2017-10-04", "2017-10-09", true)]
    [InlineData("0201-10-02", "2017-10-06", "2017-10-02", "2017-10-06", true)]
    [InlineData("1971-10-09", "2017-10-11", "2017-10-09", "2017-10-11", true)]
    [InlineData("2017-10-03", "2107-10-05", "2017-10-03", "2017-10-05", true)]
    [InlineData("0171-12-30", "2018-01-02", "2017-12-30", "2018-01-02", true)]    // virada do ano
    [InlineData("2017-10-09", "2017-10-11", "2017-10-09", "2017-10-11", false)]   // datas boas
    [InlineData("1971-03-09", "2017-10-11", "1971-03-09", "2017-10-11", false)]   // ano trocado daria 7 meses: fica
    [InlineData("0171-10-04", "0201-10-09", "0171-10-04", "0201-10-09", false)]   // as duas impossíveis: fica
    public void Ano_digitado_errado_no_resultado(string inicio, string resultado, string inicioOk, string resultadoOk, bool corrigiu) =>
        Assert.Equal((DateOnly.Parse(inicioOk), DateOnly.Parse(resultadoOk), corrigiu),
            Conversoes.AnoDigitadoErrado(DateOnly.Parse(inicio), DateOnly.Parse(resultado), new DateOnly(2026, 10, 29)));
}
