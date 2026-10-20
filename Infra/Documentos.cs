namespace LabControl.Infra;

/// <summary>Documentos brasileiros (CPF, CNPJ, CEP): validação do dígito verificador e formatação.</summary>
public static class Documentos
{
    public static string SoDigitos(string? s) => new((s ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>CPF com 11 dígitos e dígitos verificadores corretos (máscara opcional).</summary>
    public static bool CpfValido(string? cpf)
    {
        var d = SoDigitos(cpf);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (d[i] - '0') * (tamanho + 1 - i);
            var resto = soma * 10 % 11;
            return resto == 10 ? 0 : resto;
        }
        return Digito(9) == d[9] - '0' && Digito(10) == d[10] - '0';
    }

    /// <summary>000.000.000-00.</summary>
    public static string FormatarCpf(string cpf)
    {
        var d = SoDigitos(cpf);
        return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    }

    /// <summary>CEP com 8 dígitos → 00000-000 (null se inválido).</summary>
    public static string? FormatarCep(string? cep) => SoDigitos(cep) is { Length: 8 } d ? $"{d[..5]}-{d[5..]}" : null;

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
