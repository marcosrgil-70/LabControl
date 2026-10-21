using System.Globalization;
using System.Text;
using LabControl.Infra;
using LabControl.Models;

namespace LabControl.Services.Migracao;

/// <summary>Conversões de valores do legado (Firebird) para o v2 — puras, testadas em <c>MigracaoConversoesTests</c>.</summary>
public static class Conversoes
{
    /// <summary>
    /// <c>TColor</c> do Delphi (inteiro BGR: R = byte baixo) → <c>#RRGGBB</c>. Negativa = cor de sistema do Windows
    /// (ex.: -16777214 clBtnFace), sem equivalente → null.
    /// </summary>
    public static string? Cor(string? tcolor)
    {
        if (!long.TryParse(tcolor?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) || c < 0 || c > 0xFFFFFF) return null;
        return $"#{c & 0xFF:X2}{(c >> 8) & 0xFF:X2}{(c >> 16) & 0xFF:X2}";
    }

    /// <summary><c>ESTADO_CIVIL</c> 0..4 do legado → S, C, D, P, V.</summary>
    public static EstadoCivil? EstadoCivil(int? valor) => valor switch
    {
        0 => Models.EstadoCivil.Solteiro,
        1 => Models.EstadoCivil.Casado,
        2 => Models.EstadoCivil.Divorciado,
        3 => Models.EstadoCivil.Separado,
        4 => Models.EstadoCivil.Viuvo,
        _ => null,
    };

    public static Sexo? Sexo(string? valor) => valor?.Trim().ToUpperInvariant() switch
    {
        "M" => Models.Sexo.Masculino,
        "F" => Models.Sexo.Feminino,
        _ => null,
    };

    /// <summary>'S'/'N' (ou 1/0) → bool; nulo → false.</summary>
    public static bool Flag(string? valor) => valor?.Trim().ToUpperInvariant() is "S" or "1" or "T";

    /// <summary>CPF (11 dígitos) ou CNPJ (14) formatados; outro tamanho fica como digitado (aparado) e é anomalia.</summary>
    public static string? Documento(string? valor)
    {
        var d = Documentos.SoDigitos(valor);
        return d.Length switch
        {
            0 => null,
            11 => Documentos.FormatarCpf(d),
            14 => Documentos.FormatarCnpj(d),
            _ => Textos.Limpo(valor),
        };
    }

    /// <summary>
    /// Nome de cidade comparável: sem acento, maiúsculas, espaços simples e sem apóstrofo/hífen (o legado e o IBGE
    /// escrevem "D'OESTE"/"DOESTE", "SANTA BÁRBARA D OESTE"…).
    /// </summary>
    public static string NomeComparavel(string? nome)
    {
        var s = (nome ?? "").Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch is '\'' or '-' or '´' or '`' ? ' ' : ch);
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// E-mail do legado limpo: só o primeiro endereço (corta em vírgula, ponto e vírgula ou espaço), sem &lt; &gt;,
    /// em minúsculas. Devolve null se não sobrar algo com "@".
    /// </summary>
    public static string? Email(string? valor)
    {
        var s = (valor ?? "").Trim().Replace("<", "").Replace(">", "");
        var corte = s.IndexOfAny([',', ';', ' ']);
        if (corte >= 0) s = s[..corte];
        s = s.Trim().ToLowerInvariant();
        return s.Contains('@') ? s : null;
    }
}
