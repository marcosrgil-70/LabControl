using System.Text.RegularExpressions;

namespace LabControl.Infra;

/// <summary>Texto do resultado partido para impressão: <c>Antes × 10^Expoente Depois</c> (sem expoente: só <c>Antes</c>).</summary>
public sealed record ResultadoImpresso(string Antes, string? Expoente, string Depois)
{
    /// <summary>Em texto corrido, com o expoente em sobrescrito Unicode (ex.: <c>&lt; 1,0 × 10² UFC/g</c>).</summary>
    public override string ToString() => Expoente is null ? Antes : $"{Antes} × 10{ResultadoTexto.Sobrescrito(Expoente)}{Depois}";
}

/// <summary>
/// O banco grava o resultado formatado com o expoente em "e" (<c>FN_FORMATA_RESULTADO</c>: <c>&lt; 1,0e2 UFC/g</c>); a
/// tela e o boletim mostram <c>&lt; 1,0 × 10² UFC/g</c>. Descritivo ("Ausência em 25 g") passa como está.
/// </summary>
public static partial class ResultadoTexto
{
    public static ResultadoImpresso? Partes(string? formatado)
    {
        if (string.IsNullOrWhiteSpace(formatado)) return null;
        var m = Expoente().Match(formatado);
        return m.Success ? new(m.Groups["antes"].Value, m.Groups["exp"].Value, m.Groups["depois"].Value) : new(formatado, null, "");
    }

    public static string? Impresso(string? formatado) => Partes(formatado)?.ToString();

    /// <summary>Para as telas: HTML com o expoente em <c>&lt;sup&gt;</c> (texto codificado); vazio vira <paramref name="vazio"/>.</summary>
    public static Microsoft.AspNetCore.Html.IHtmlContent Html(string? formatado, string vazio = "")
    {
        var e = System.Text.Encodings.Web.HtmlEncoder.Default;
        return new Microsoft.AspNetCore.Html.HtmlString(Partes(formatado) switch
        {
            null => vazio,
            { Expoente: null } p => e.Encode(p.Antes),
            var p => $"{e.Encode(p.Antes)} × 10<sup>{e.Encode(p.Expoente)}</sup>{e.Encode(p.Depois)}",
        });
    }

    private const string Digitos = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    public static string Sobrescrito(string expoente) =>
        string.Concat(expoente.Select(c => c == '-' ? '⁻' : char.IsAsciiDigit(c) ? Digitos[c - '0'] : c));

    // número (com vírgula) seguido de e±n, no início do texto: "< 1,0e2 UFC/g", "3,5e-3"
    [GeneratedRegex(@"^(?<antes>[^\d]*-?\d+(?:,\d+)?)e(?<exp>-?\d+)(?<depois>.*)$")]
    private static partial Regex Expoente();
}
