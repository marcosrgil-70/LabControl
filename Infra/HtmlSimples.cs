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
        // monta a saída só com as tags permitidas (sem atributos); entre elas, "<" e ">" viram texto — uma tag sem o
        // ">" final ("<img src=x onerror=…") seria fechada pelo navegador com a marcação seguinte (security-review 9.3)
        var saida = new System.Text.StringBuilder(s.Length);
        var texto = new System.Text.StringBuilder(s.Length);
        var pos = 0;
        foreach (Match m in Tag().Matches(s))
        {
            Texto(s[pos..m.Index]);
            var nome = m.Groups["nome"].Value.ToLowerInvariant();
            if (Permitidas.Contains(nome))
                saida.Append(nome == "br" ? "<br>" : m.Groups["fecha"].Success ? $"</{nome}>" : $"<{nome}>");
            pos = m.Index + m.Length;
        }
        Texto(s[pos..]);
        return string.IsNullOrWhiteSpace(System.Net.WebUtility.HtmlDecode(texto.Replace("&nbsp;", " ").ToString())) ? null : saida.ToString().Trim();

        void Texto(string t)
        {
            t = t.Replace("<", "&lt;").Replace(">", "&gt;");
            saida.Append(t);
            texto.Append(t);
        }
    }

    [GeneratedRegex(@"<(script|style|iframe|object|embed)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlocoPerigoso();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comentario();

    [GeneratedRegex(@"<(?<fecha>/)?\s*(?<nome>[a-zA-Z][a-zA-Z0-9]*)\b[^>]*>")]
    private static partial Regex Tag();
}
