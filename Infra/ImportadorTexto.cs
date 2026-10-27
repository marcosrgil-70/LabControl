using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LabControl.Infra;

/// <summary>Texto importado para um editor rico: o HTML simples e o aviso do que ficou de fora (null = nada).</summary>
public sealed record TextoImportado(string? Html, string? Aviso);

/// <summary>
/// "Importar arquivo" do contrato da proposta (T20 › Contrato; decisão do usuário, 6.1): <c>.html/.htm</c>, <c>.txt</c>
/// e Word <c>.docx</c> viram o HTML simples dos editores (<see cref="HtmlSimples"/>). Do Word vêm parágrafos, negrito,
/// itálico, sublinhado e listas; tabelas e imagens não — e isso é avisado.
/// </summary>
public static class ImportadorTexto
{
    public const long TamanhoMaximo = 2 * 1024 * 1024;
    public static readonly IReadOnlyList<string> Extensoes = [".docx", ".html", ".htm", ".txt"];

    public static Resultado<TextoImportado> Importar(string nomeArquivo, Stream conteudo)
    {
        var ext = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        try
        {
            return ext switch
            {
                ".txt" => Resultado.Ok(new TextoImportado(DeTexto(Ler(conteudo)), null)),
                ".html" or ".htm" => Resultado.Ok(new TextoImportado(HtmlSimples.Limpar(Ler(conteudo)), null)),
                ".docx" => Resultado.Ok(DeWord(conteudo)),
                _ => Resultado.Erro($"Formato não aceito: use {string.Join(", ", Extensoes)}."),
            };
        }
        catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException or IOException or FormatException)
        {
            return Resultado.Erro("Não foi possível ler o arquivo do Word (está corrompido ou não é um .docx).");
        }
    }

    private static string Ler(Stream s)
    {
        using var leitor = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return leitor.ReadToEnd();
    }

    /// <summary>Cada bloco separado por linha em branco vira um parágrafo; quebras simples viram &lt;br&gt;.</summary>
    public static string? DeTexto(string texto)
    {
        var blocos = texto.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim('\n')).Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => "<p>" + string.Join("<br>", b.Split('\n').Select(Escapar)) + "</p>");
        return HtmlSimples.Limpar(string.Concat(blocos));
    }

    // só o que quebra o HTML; acentos ficam como texto (WebUtility.HtmlEncode viraria "&#225;")
    private static string Escapar(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static TextoImportado DeWord(Stream conteudo)
    {
        using var copia = new MemoryStream();
        conteudo.CopyTo(copia); // o OpenXml precisa de stream com Seek
        copia.Position = 0;
        using var doc = WordprocessingDocument.Open(copia, false);
        var corpo = doc.MainDocumentPart?.Document?.Body;
        if (corpo is null) return new TextoImportado(null, "O documento do Word está vazio.");

        var html = new StringBuilder();
        var lista = false;
        int tabelas = 0, imagens = corpo.Descendants<Drawing>().Count();
        foreach (var bloco in corpo.ChildElements)
        {
            if (bloco is Table) { tabelas++; continue; }
            if (bloco is not Paragraph p) continue;
            var item = p.ParagraphProperties?.NumberingProperties is not null; // parágrafo numerado/marcado = item de lista
            if (item != lista) { html.Append(item ? "<ul>" : "</ul>"); lista = item; }
            var texto = Trechos(p);
            if (item) html.Append("<li>").Append(texto).Append("</li>");
            else if (texto.Length > 0) html.Append("<p>").Append(texto).Append("</p>");
        }
        if (lista) html.Append("</ul>");

        var fora = new List<string>();
        if (tabelas > 0) fora.Add($"{tabelas} tabela(s)");
        if (imagens > 0) fora.Add($"{imagens} imagem(ns)");
        return new TextoImportado(HtmlSimples.Limpar(html.ToString()),
            fora.Count == 0 ? null : $"Importado sem {string.Join(" e ", fora)} do Word (o editor guarda só texto formatado).");
    }

    // trechos do parágrafo com negrito/itálico/sublinhado; tabulação e quebra de linha do Word mantidas
    private static string Trechos(Paragraph p)
    {
        var sb = new StringBuilder();
        foreach (var r in p.Descendants<Run>())
        {
            var t = new StringBuilder();
            foreach (var e in r.ChildElements)
                t.Append(e switch
                {
                    Text x => Escapar(x.Text),
                    TabChar => " ",
                    Break => "<br>",
                    _ => "",
                });
            if (t.Length == 0) continue;
            var props = r.RunProperties;
            var s = t.ToString();
            if (props?.Underline?.Val?.Value is { } u && u != UnderlineValues.None) s = $"<u>{s}</u>";
            if (props?.Italic is { } i && (i.Val is null || i.Val.Value)) s = $"<em>{s}</em>";
            if (props?.Bold is { } b && (b.Val is null || b.Val.Value)) s = $"<strong>{s}</strong>";
            sb.Append(s);
        }
        return sb.ToString();
    }
}
