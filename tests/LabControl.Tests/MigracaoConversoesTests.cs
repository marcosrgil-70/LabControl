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
}
