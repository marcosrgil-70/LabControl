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

    /// <summary>
    /// Data parcial (validade/fabricação) do legado no formato que a <c>TG_AMOSTRAS_BI</c> aceita mesmo na migração:
    /// 0 → nulo; mês fora de 1–12 ou sem ano cai (com o dia); dia sem mês, fora de 1–31 ou além do fim do mês cai.
    /// </summary>
    public static (int? Dia, int? Mes, int? Ano) DataParcial(int? dia, int? mes, int? ano)
    {
        int? d = dia is > 0 ? dia : null, m = mes is > 0 ? mes : null, a = ano is > 0 and <= 9999 ? ano : null;
        if (m is not (>= 1 and <= 12) || a is null) m = null;
        if (d is not (>= 1 and <= 31) || m is null || d > DateTime.DaysInMonth(a!.Value, m.Value)) d = null;
        return (d, m, a);
    }

    // só o que o HTML exige (WebUtility.HtmlEncode troca os acentos por &#nnn;)
    private static string Escapar(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static readonly Encoding Cp1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

    // destinos do RTF que não são texto do documento
    private static readonly HashSet<string> GruposIgnorados =
    [
        "fonttbl", "colortbl", "stylesheet", "info", "pict", "header", "footer", "headerl", "headerr", "footerl", "footerr",
        "generator", "listtable", "listoverridetable", "themedata", "latentstyles", "rsidtbl", "xmlnstbl", "mmathPr",
        "datastore", "colorschememapping", "fldinst", "object", "bkmkstart", "bkmkend",
    ];

    /// <summary>
    /// RTF do <c>TRichEdit</c> do legado (contrato, observações, itens) → HTML simples: parágrafos, tab, negrito,
    /// itálico e sublinhado; <c>\'hh</c> em cp1252 e <c>\uN</c>. Sem texto (só parágrafos vazios) → null. Texto que não
    /// começa com <c>{\rtf</c> vira um parágrafo por linha. Lança <see cref="FormatException"/> se o RTF estiver truncado.
    /// </summary>
    public static string? RtfParaHtml(string? rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf)) return null;
        if (!rtf.TrimStart().StartsWith(@"{\rtf", StringComparison.Ordinal))
            return string.Concat(rtf.Trim().Replace("\r\n", "\n").Split('\n')
                .Select(l => l.Trim().Length == 0 ? "<p><br></p>" : $"<p>{Escapar(l.TrimEnd())}</p>"));

        var paragrafos = new List<string>();
        var atual = new StringBuilder();
        var pilha = new Stack<(bool Ignorar, bool B, bool I, bool U, int Uc)>();
        (bool Ignorar, bool B, bool I, bool U, int Uc) estado = (false, false, false, false, 1);
        (bool B, bool I, bool U) aberto = (false, false, false);
        var pularFallback = 0;
        var temTexto = false;

        void Fechar()
        {
            if (aberto.U) atual.Append("</u>");
            if (aberto.I) atual.Append("</em>");
            if (aberto.B) atual.Append("</strong>");
            aberto = (false, false, false);
        }
        void Texto(string t)
        {
            if (estado.Ignorar) return;
            if ((estado.B, estado.I, estado.U) != aberto)
            {
                Fechar();
                if (estado.B) atual.Append("<strong>");
                if (estado.I) atual.Append("<em>");
                if (estado.U) atual.Append("<u>");
                aberto = (estado.B, estado.I, estado.U);
            }
            atual.Append(t);
            if (!string.IsNullOrWhiteSpace(t) && t != "&emsp;") temTexto = true;
        }
        void Paragrafo()
        {
            if (estado.Ignorar) return;
            Fechar();
            paragrafos.Add(atual.ToString());
            atual.Clear();
        }

        for (var i = 0; i < rtf.Length; i++)
        {
            var ch = rtf[i];
            if (ch == '{') { pilha.Push(estado); continue; }
            if (ch == '}')
            {
                if (pilha.Count == 0) break; // fim do documento
                estado = pilha.Pop();
                continue;
            }
            if (ch is '\r' or '\n') continue;
            if (ch != '\\')
            {
                if (pularFallback > 0) { pularFallback--; continue; }
                Texto(Escapar(ch.ToString()));
                continue;
            }
            if (++i >= rtf.Length) throw new FormatException("RTF truncado.");
            var c = rtf[i];
            if (c is '\\' or '{' or '}') { Texto(Escapar(c.ToString())); continue; }
            if (c == '*') { estado.Ignorar = true; continue; }
            if (c == '~') { Texto("&nbsp;"); continue; }
            if (c is '\r' or '\n') { Paragrafo(); continue; }
            if (c == '\'')
            {
                if (i + 2 >= rtf.Length) throw new FormatException("RTF truncado.");
                var b = Convert.ToByte(rtf.Substring(i + 1, 2), 16);
                i += 2;
                if (pularFallback > 0) { pularFallback--; continue; }
                Texto(Escapar(Cp1252.GetString([b])));
                continue;
            }
            if (!char.IsAsciiLetter(c)) continue; // outros símbolos de controle (\-, \_, \|…)
            var inicio = i;
            while (i < rtf.Length && char.IsAsciiLetter(rtf[i])) i++;
            var palavra = rtf[inicio..i];
            int? parametro = null;
            var p0 = i;
            if (i < rtf.Length && (rtf[i] == '-' || char.IsAsciiDigit(rtf[i])))
            {
                i++;
                while (i < rtf.Length && char.IsAsciiDigit(rtf[i])) i++;
                parametro = int.Parse(rtf[p0..i], CultureInfo.InvariantCulture);
            }
            if (!(i < rtf.Length && rtf[i] == ' ')) i--; // o espaço depois da palavra de controle é delimitador

            if (GruposIgnorados.Contains(palavra)) { estado.Ignorar = true; continue; }
            switch (palavra)
            {
                case "par": case "line": Paragrafo(); break;
                case "tab": Texto("&emsp;"); break;
                case "b": estado.B = parametro != 0; break;
                case "i": estado.I = parametro != 0; break;
                case "ul": estado.U = parametro != 0; break;
                case "ulnone": estado.U = false; break;
                case "plain": estado.B = estado.I = estado.U = false; break;
                case "uc": estado.Uc = parametro ?? 1; break;
                case "u" when parametro is { } u:
                    Texto(Escapar(((char)(u < 0 ? u + 65536 : u)).ToString()));
                    pularFallback = estado.Uc;
                    break;
            }
        }
        Paragrafo();
        if (!temTexto) return null;
        while (paragrafos.Count > 0 && string.IsNullOrWhiteSpace(paragrafos[^1])) paragrafos.RemoveAt(paragrafos.Count - 1);
        return string.Concat(paragrafos.Select(p => p.Length == 0 ? "<p><br></p>" : $"<p>{p}</p>"));
    }
}
