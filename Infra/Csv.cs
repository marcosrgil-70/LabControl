using System.Text;

namespace LabControl.Infra;

/// <summary>CSV para o Excel em português: <c>;</c>, campos entre aspas, UTF-8 com BOM (relatórios, auditoria, migração).</summary>
public sealed class Csv
{
    private readonly StringBuilder _texto = new();

    public static string Celula(string? s) => s is null ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";

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
