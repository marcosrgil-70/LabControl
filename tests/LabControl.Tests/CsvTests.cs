using System.Text;
using LabControl.Infra;

namespace LabControl.Tests;

/// <summary>CSV para o Excel (8.5): texto que viraria fórmula sai neutralizado; números com sinal continuam números.</summary>
public class CsvTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")", "\"'=HYPERLINK(\"\"http://x\"\")\"")]
    [InlineData("@SOMA(A1)", "\"'@SOMA(A1)\"")]
    [InlineData("+cmd", "\"'+cmd\"")]
    [InlineData("-10,00", "\"-10,00\"")]
    [InlineData("1.234,56", "\"1.234,56\"")]
    [InlineData("CLIENTE TESTE", "\"CLIENTE TESTE\"")]
    [InlineData(null, "")]
    public void Celula_neutraliza_formula(string? valor, string esperado) => Assert.Equal(esperado, Csv.Celula(valor));

    [Fact]
    public void Bytes_com_bom_e_ponto_e_virgula() =>
        Assert.Equal("﻿\"a\";;\"b\"\r\n", Encoding.UTF8.GetString(new Csv().Linha("a", null, "b").Bytes()));
}
