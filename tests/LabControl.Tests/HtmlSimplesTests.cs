using LabControl.Infra;

namespace LabControl.Tests;

/// <summary>Texto rico das telas (observação, itens, contrato): só formatação simples, sem atributo nem tag aberta.</summary>
public class HtmlSimplesTests
{
    [Theory]
    [InlineData("<p>Amostra <strong>lacrada</strong></p>", "<p>Amostra <strong>lacrada</strong></p>")]
    [InlineData("<P class=\"x\" onclick=\"y()\">a<BR/>b</P>", "<p>a<br>b</p>")]
    [InlineData("<img src=x onerror=alert(1)>texto", "texto")]
    [InlineData("<script>alert(1)</script>ok", "ok")]
    [InlineData("<!-- comentário -->ok", "ok")]
    [InlineData("temperatura > 5 e < 10", "temperatura &gt; 5 e &lt; 10")]
    // security-review 9.3: tag sem o ">" final seria fechada pelo navegador com a marcação seguinte
    [InlineData("ok<img src=x onerror=alert(1)", "ok&lt;img src=x onerror=alert(1)")]
    [InlineData("<scr<b>ipt>alert(1)</script>", "ipt&gt;alert(1)")]
    [InlineData("<!--<img src=x onerror=alert(1)>", "&lt;!--")]
    [InlineData("<p>&nbsp;</p><br>", null)]
    [InlineData("   ", null)]
    public void Limpar(string html, string? esperado) => Assert.Equal(esperado, HtmlSimples.Limpar(html));
}
