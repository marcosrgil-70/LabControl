using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace LabControl.Infra;

/// <summary>Texto importado para um editor rico: o HTML simples e o aviso do que ficou de fora (null = nada).</summary>
public sealed record TextoImportado(string? Html, string? Aviso);

/// <summary>
/// "Importar arquivo" do contrato da proposta (T20 › Contrato; decisão do usuário, 6.1): <c>.html/.htm</c>, <c>.txt</c>
/// e Word <c>.docx</c> viram o HTML simples dos editores (<see cref="HtmlSimples"/>). Do Word vêm parágrafos, negrito,
/// itálico, sublinhado e listas (com marcador ou numeradas); tabelas e imagens não — e isso é avisado.
/// </summary>
public static partial class ImportadorTexto
{
    public const long TamanhoMaximo = 2 * 1024 * 1024;
    public static readonly IReadOnlyList<string> Extensoes = [".docx", ".html", ".htm", ".txt"];

    static ImportadorTexto() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Windows-1252

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
        catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException or IOException or FormatException or XmlException)
        {
            return Resultado.Erro("Não foi possível ler o arquivo do Word (está corrompido ou não é um .docx).");
        }
    }

    /// <summary>
    /// Com BOM, a codificação do BOM; sem BOM, UTF-8 se for válido, senão Windows-1252 (o "ANSI" do Bloco de Notas
    /// antigo e de sistemas legados: lido como UTF-8, "CLÁUSULA" virava "CL�USULA" — 6.3).
    /// </summary>
    private static string Ler(Stream s)
    {
        using var copia = new MemoryStream();
        s.CopyTo(copia);
        var bytes = copia.ToArray();
        using (var comBom = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            var texto = comBom.ReadToEnd();
            if (!Equals(comBom.CurrentEncoding, Encoding.UTF8) || bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble)) return texto;
        }
        try { return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.GetEncoding(1252).GetString(bytes); }
    }

    /// <summary>Cada bloco separado por linha em branco vira um parágrafo; quebras simples viram &lt;br&gt;.</summary>
    public static string? DeTexto(string texto)
    {
        var blocos = texto.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim('\n')).Where(b => !string.IsNullOrWhiteSpace(b))
            .Select(b => "<p>" + string.Join("<br>", b.Split('\n').Select(Escapar)) + "</p>");
        return HtmlSimples.Limpar(string.Concat(blocos));
    }

    /// <summary>
    /// Texto que pode ter sido gravado como HTML (editor, testes) ou digitado puro (o &lt;textarea&gt; do contrato
    /// padrão na T05): com tag do editor é limpo como HTML; senão é convertido como texto (parágrafos e quebras).
    /// </summary>
    public static string? DeTextoOuHtml(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : TagDoEditor().IsMatch(texto) ? HtmlSimples.Limpar(texto) : DeTexto(texto);

    [GeneratedRegex(@"<\s*/?\s*(p|br|div|ul|ol|li|strong|b|em|i|u|span)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TagDoEditor();

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
        var numeracao = doc.MainDocumentPart!.NumberingDefinitionsPart?.Numbering;

        var html = new StringBuilder();
        var abertas = new Stack<string>(); // "ul"/"ol" abertas, uma por nível da lista do Word
        int tabelas = 0, imagens = corpo.Descendants<Drawing>().Count();
        foreach (var bloco in Blocos(corpo))
        {
            if (bloco is Table) { tabelas++; continue; }
            var p = (Paragraph)bloco;
            var num = p.ParagraphProperties?.NumberingProperties; // parágrafo numerado/marcado = item de lista
            var nivel = num is null ? 0 : (num.NumberingLevelReference?.Val?.Value ?? 0) + 1;
            var tag = num is null ? null : Numerada(numeracao, num) ? "ol" : "ul";
            while (abertas.Count > nivel) html.Append("</").Append(abertas.Pop()).Append('>');
            if (tag is not null && abertas.Count == nivel && abertas.Peek() != tag) html.Append("</").Append(abertas.Pop()).Append('>');
            while (tag is not null && abertas.Count < nivel) { html.Append('<').Append(tag).Append('>'); abertas.Push(tag); }

            var texto = Trechos(p);
            if (tag is not null) html.Append("<li>").Append(texto).Append("</li>");
            else if (texto.Length > 0) html.Append("<p>").Append(texto).Append("</p>");
        }
        while (abertas.Count > 0) html.Append("</").Append(abertas.Pop()).Append('>');

        var fora = new List<string>();
        if (tabelas > 0) fora.Add($"{tabelas} tabela(s)");
        if (imagens > 0) fora.Add($"{imagens} imagem(ns)");
        return new TextoImportado(HtmlSimples.Limpar(html.ToString()),
            fora.Count == 0 ? null : $"Importado sem {string.Join(" e ", fora)} do Word (o editor guarda só texto formatado).");
    }

    /// <summary>
    /// Parágrafos e tabelas do corpo, inclusive os de dentro de controles de conteúdo (<c>SdtBlock</c>) e XML
    /// personalizado — comuns em modelos do Word e antes descartados sem aviso (6.3).
    /// </summary>
    private static IEnumerable<OpenXmlElement> Blocos(OpenXmlElement pai)
    {
        foreach (var e in pai.ChildElements)
        {
            if (e is Paragraph or Table) yield return e;
            else if (e is SdtBlock or SdtContentBlock or CustomXmlBlock)
                foreach (var filho in Blocos(e)) yield return filho;
        }
    }

    /// <summary>Lista numerada (1., a., I.…) ou com marcador, pelo formato do nível na definição de numeração do Word.</summary>
    private static bool Numerada(Numbering? numeracao, NumberingProperties num)
    {
        var idNum = num.NumberingId?.Val?.Value;
        var nivel = num.NumberingLevelReference?.Val?.Value ?? 0;
        var instancia = numeracao?.Elements<NumberingInstance>().FirstOrDefault(n => n.NumberID?.Value == idNum);
        var idAbstrato = instancia?.AbstractNumId?.Val?.Value;
        var formato = instancia?.Elements<LevelOverride>().FirstOrDefault(o => o.LevelIndex?.Value == nivel)?.Level?.NumberingFormat?.Val
                      ?? numeracao?.Elements<AbstractNum>().FirstOrDefault(a => a.AbstractNumberId?.Value == idAbstrato)
                             ?.Elements<Level>().FirstOrDefault(l => l.LevelIndex?.Value == nivel)?.NumberingFormat?.Val;
        return formato is { } f && f.Value != NumberFormatValues.Bullet && f.Value != NumberFormatValues.None;
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
