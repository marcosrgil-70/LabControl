using LabControl.Infra;

namespace LabControl.Tests;

/// <summary>Número digitado nas telas (valor do parâmetro, qtde. por embalagem): "1.000" é milhar, não 1 (code-review 3.6).</summary>
public class TextosTests
{
    [Theory]
    [InlineData("1.000", 1000)]
    [InlineData("12.500", 12500)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("12.5", 12.5)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("0,0001", 0.0001)]
    public void Decimal_le_milhar_e_decimal(string texto, double esperado)
    {
        Assert.True(Textos.Decimal(texto, out var v));
        Assert.Equal((decimal)esperado, v);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("1.000.5")]
    public void Decimal_recusa_texto_invalido(string texto) => Assert.False(Textos.Decimal(texto, out _));
}
