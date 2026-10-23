using System.Text.RegularExpressions;

namespace LabControl.Infra;

/// <summary>
/// Texto rico das telas (itens solicitados/adquiridos, observação, contrato): HTML só com formatação simples. Tudo
/// fora da lista (e todo atributo — <c>onerror</c>, <c>style</c>, <c>href</c>…) sai antes de gravar e antes de exibir,
/// porque o editor da página põe o HTML direto no <c>innerHTML</c>.
/// </summary>
public static partial class HtmlSimples
{
    private static readonly HashSet<string> Permitidas = ["p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "div"];

    /// <summary>HTML limpo; vazio (sem texto, só parágrafos/quebras) vira null.</summary>
    public static string? Limpar(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        // conteúdo de script/style some inteiro; os demais elementos perdem só a tag
        var s = BlocoPerigoso().Replace(html, "");
        s = Comentario().Replace(s, "");
        s = Tag().Replace(s, m =>
        {
            var nome = m.Groups["nome"].Value.ToLowerInvariant();
            if (!Permitidas.Contains(nome)) return "";
            return nome == "br" ? "<br>" : m.Groups["fecha"].Success ? $"</{nome}>" : $"<{nome}>";
        });
        var texto = Tag().Replace(s, "").Replace("&nbsp;", " ");
        return string.IsNullOrWhiteSpace(System.Net.WebUtility.HtmlDecode(texto)) ? null : s.Trim();
    }

    [GeneratedRegex(@"<(script|style|iframe|object|embed)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlocoPerigoso();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comentario();

    [GeneratedRegex(@"<(?<fecha>/)?\s*(?<nome>[a-zA-Z][a-zA-Z0-9]*)\b[^>]*>")]
    private static partial Regex Tag();
}
