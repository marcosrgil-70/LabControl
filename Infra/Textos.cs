using System.Globalization;
using System.Text.RegularExpressions;

namespace LabControl.Infra;

/// <summary>Normalização dos textos digitados (os cadastros gravam aparado e, quase sempre, em maiúsculas como o legado).</summary>
public static class Textos
{
    /// <summary>Aparado; vazio ou só espaços vira null.</summary>
    public static string? Limpo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Aparado e em maiúsculas; vazio vira null.</summary>
    public static string? Maiusculas(string? s) => Limpo(s)?.ToUpperInvariant();

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>
    /// Número digitado na tela: com vírgula é pt-BR ("1.234,56"); sem vírgula, "1.000"/"12.500" (grupos de 3) são milhar
    /// e "12.5" é ponto decimal. Sem sinal; o texto precisa vir aparado (<see cref="Limpo"/>).
    /// </summary>
    public static bool Decimal(string texto, out decimal valor) =>
        texto.Contains(',') || Regex.IsMatch(texto, @"^\d{1,3}(\.\d{3})+$")
            ? decimal.TryParse(texto, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, PtBr, out valor)
            : decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);
}
