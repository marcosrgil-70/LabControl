namespace LabControl.Infra;

/// <summary>Normalização dos textos digitados (os cadastros gravam aparado e, quase sempre, em maiúsculas como o legado).</summary>
public static class Textos
{
    /// <summary>Aparado; vazio ou só espaços vira null.</summary>
    public static string? Limpo(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Aparado e em maiúsculas; vazio vira null.</summary>
    public static string? Maiusculas(string? s) => Limpo(s)?.ToUpperInvariant();
}
