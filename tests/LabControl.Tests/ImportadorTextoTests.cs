using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LabControl.Infra;

namespace LabControl.Tests;

/// <summary>6.1 — "Importar arquivo" do contrato da proposta: texto, HTML (limpo) e Word (.docx).</summary>
public class ImportadorTextoTests
{
    private static Resultado<TextoImportado> Importar(string nome, string conteudo) =>
        ImportadorTexto.Importar(nome, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(conteudo)));

    [Fact]
    public void Texto_vira_paragrafos_e_html_perde_o_que_e_perigoso()
    {
        Assert.Equal("<p>Cláusula 1<br>continua</p><p>Cláusula 2 &lt;a&gt;</p>", Importar("c.txt", "Cláusula 1\r\ncontinua\r\n\r\nCláusula 2 <a>").Valor.Html);
        Assert.Equal("<p>Olá <strong>mundo</strong></p>",
            Importar("c.html", "<html><body><p style='x' onclick='y'>Olá <strong>mundo</strong></p><script>alert(1)</script></body></html>").Valor.Html);
        Assert.Contains("Formato não aceito", Importar("c.pdf", "x").Mensagem);
        Assert.Contains("Word", ImportadorTexto.Importar("c.docx", new MemoryStream([1, 2, 3])).Mensagem);
    }

    [Fact]
    public void Word_traz_paragrafos_negrito_italico_listas_e_avisa_a_tabela()
    {
        using var arquivo = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(arquivo, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var lista = new ParagraphProperties(new NumberingProperties(new NumberingLevelReference { Val = 0 }, new NumberingId { Val = 1 }));
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("Contrato de ") { Space = SpaceProcessingModeValues.Preserve }),
                    new Run(new RunProperties(new Bold()), new Text("serviços")),
                    new Run(new RunProperties(new Italic()), new Text(" analíticos") { Space = SpaceProcessingModeValues.Preserve })),
                new Paragraph((ParagraphProperties)lista.CloneNode(true), new Run(new Text("prazo"))),
                new Paragraph((ParagraphProperties)lista.CloneNode(true), new Run(new Text("preço"))),
                new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text("célula")))))),
                new Paragraph(new Run(new Text("Fim")))));
        }
        arquivo.Position = 0;
        var r = ImportadorTexto.Importar("contrato.docx", arquivo);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal("<p>Contrato de <strong>serviços</strong><em> analíticos</em></p><ul><li>prazo</li><li>preço</li></ul><p>Fim</p>", r.Valor.Html);
        Assert.Contains("1 tabela(s)", r.Valor.Aviso);
    }
}
