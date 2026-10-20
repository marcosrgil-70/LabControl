namespace LabControl.Infra;

/// <summary>Documentos brasileiros (CNPJ): validação do dígito verificador e formatação.</summary>
public static class Documentos
{
    public static string SoDigitos(string? s) => new((s ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>CNPJ com 14 dígitos e dígitos verificadores corretos (máscara opcional).</summary>
    public static bool CnpjValido(string? cnpj)
    {
        var d = SoDigitos(cnpj);
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            int[] pesos = tamanho == 12 ? [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2] : [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (d[i] - '0') * pesos[i];
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(12) == d[12] - '0' && Digito(13) == d[13] - '0';
    }

    /// <summary>00.000.000/0000-00 (formato gravado em EMPRESAS.CNPJ).</summary>
    public static string FormatarCnpj(string cnpj)
    {
        var d = SoDigitos(cnpj);
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
    }
}
