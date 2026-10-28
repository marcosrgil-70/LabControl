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

    /// <summary>6.3 — cláusulas numeradas viram &lt;ol&gt; (com subnível) e o texto de controles de conteúdo não se perde.</summary>
    [Fact]
    public void Word_lista_numerada_vira_ol_e_controle_de_conteudo_entra()
    {
        using var arquivo = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(arquivo, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            static Level Nivel(int i, NumberFormatValues f) => new(new NumberingFormat { Val = f }) { LevelIndex = i };
            main.AddNewPart<NumberingDefinitionsPart>().Numbering = new Numbering(
                new AbstractNum(Nivel(0, NumberFormatValues.Decimal), Nivel(1, NumberFormatValues.Decimal)) { AbstractNumberId = 10 },
                new AbstractNum(Nivel(0, NumberFormatValues.Bullet)) { AbstractNumberId = 20 },
                new NumberingInstance(new AbstractNumId { Val = 10 }) { NumberID = 1 },
                new NumberingInstance(new AbstractNumId { Val = 20 }) { NumberID = 2 });
            static Paragraph Item(int num, int nivel, string texto) => new(
                new ParagraphProperties(new NumberingProperties(new NumberingLevelReference { Val = nivel }, new NumberingId { Val = num })),
                new Run(new Text(texto)));
            main.Document = new Document(new Body(
                new SdtBlock(new SdtContentBlock(
                    Item(1, 0, "Objeto"), Item(1, 0, "Prazo"), Item(1, 1, "Entrega"),
                    Item(1, 0, "Preço"), new Paragraph(new Run(new Text("Observações"))), Item(2, 0, "marcador")))));
        }
        arquivo.Position = 0;
        var r = ImportadorTexto.Importar("contrato.docx", arquivo);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal("<ol><li>Objeto</li><li>Prazo</li><ol><li>Entrega</li></ol><li>Preço</li></ol><p>Observações</p><ul><li>marcador</li></ul>", r.Valor.Html);

        // e o PDF numera: 1. 2. 2.1. 3.
        Assert.Equal(["1.", "2.", "2.1.", "3.", null, "•"],
            LabControl.Services.Propostas.PropostaPdf.TextoSimples(r.Valor.Html).Select(p => p.Marcador));
    }

    /// <summary>6.3 — .txt em ANSI (Windows-1252) mantém os acentos; .docx com XML corrompido dá a mensagem, não erro 500.</summary>
    [Fact]
    public void Texto_ansi_mantem_acentos_e_word_com_xml_corrompido_avisa()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var ansi = System.Text.Encoding.GetEncoding(1252).GetBytes("CLÁUSULA 1 — serviço até 5 dias");
        Assert.Equal("<p>CLÁUSULA 1 — serviço até 5 dias</p>", ImportadorTexto.Importar("c.txt", new MemoryStream(ansi)).Valor.Html);

        using var arquivo = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(arquivo, WordprocessingDocumentType.Document))
            doc.AddMainDocumentPart().Document = new Document(new Body(new Paragraph(new Run(new Text("x")))));
        using (var zip = new System.IO.Compression.ZipArchive(arquivo, System.IO.Compression.ZipArchiveMode.Update, leaveOpen: true))
        {
            var parte = zip.Entries.Single(e => e.FullName.EndsWith("document.xml"));
            var nome = parte.FullName;
            parte.Delete();
            using var w = new StreamWriter(zip.CreateEntry(nome).Open());
            w.Write("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p>");
        }
        arquivo.Position = 0;
        Assert.Equal("Não foi possível ler o arquivo do Word (está corrompido ou não é um .docx).",
            ImportadorTexto.Importar("contrato.docx", arquivo).Mensagem);
    }
}
