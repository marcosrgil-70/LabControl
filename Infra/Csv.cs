using System.Text;

namespace LabControl.Infra;

/// <summary>CSV para o Excel em português: <c>;</c>, campos entre aspas, UTF-8 com BOM (relatórios, auditoria, migração).</summary>
public sealed class Csv
{
    private readonly StringBuilder _texto = new();

    /// <summary>
    /// Texto que o Excel leria como fórmula (<c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, tab, CR — mesmo entre aspas) sai com
    /// <c>'</c> na frente: a auditoria exporta logins digitados por quem nem entrou. Número com sinal ("-1,5") fica como está.
    /// </summary>
    public static string Celula(string? s)
    {
        if (s is null) return "";
        if (s.Length > 0 && s[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            && !((s[0] is '+' or '-') && decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out _)))
            s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    public Csv Linha(params IEnumerable<string?> celulas)
    {
        _texto.AppendJoin(';', celulas.Select(Celula)).Append("\r\n");
        return this;
    }

    public byte[] Bytes()
    {
        var preambulo = Encoding.UTF8.GetPreamble();
        var bytes = new byte[preambulo.Length + Encoding.UTF8.GetByteCount(_texto.ToString())];
        preambulo.CopyTo(bytes, 0);
        Encoding.UTF8.GetBytes(_texto.ToString(), bytes.AsSpan(preambulo.Length));
        return bytes;
    }
}
