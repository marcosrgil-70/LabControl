using System.Text;

namespace LabControl.Services.Relatorios;

/// <summary>Papel da linha na tabela: dado, cabeçalho de grupo, subtotal do grupo ou total geral.</summary>
public enum TipoLinha { Dado, Grupo, Subtotal, Total }

public sealed record ColunaRelatorio(string Titulo, bool Numero = false, float Largura = 1);

/// <summary>Células já formatadas (pt-BR): a tela, o PDF e o CSV mostram o mesmo texto (RN-09.09).</summary>
public sealed record LinhaRelatorio(IReadOnlyList<string?> Celulas, TipoLinha Tipo = TipoLinha.Dado);

public sealed class TabelaRelatorio
{
    public string? Titulo { get; init; }
    public required IReadOnlyList<ColunaRelatorio> Colunas { get; init; }
    public List<LinhaRelatorio> Linhas { get; } = [];

    public int Dados => Linhas.Count(l => l.Tipo == TipoLinha.Dado);

    public void Dado(params string?[] celulas) => Linhas.Add(new(celulas));
    public void Grupo(string texto) => Linhas.Add(new([texto], TipoLinha.Grupo));
    public void Subtotal(params string?[] celulas) => Linhas.Add(new(celulas, TipoLinha.Subtotal));
    public void Total(params string?[] celulas) => Linhas.Add(new(celulas, TipoLinha.Total));
}

/// <summary>
/// Resultado de um relatório gerencial (T30-00): título, filtros usados e uma ou mais tabelas (a principal e os resumos).
/// O mesmo objeto vira a tela, o PDF (<see cref="RelatorioPdf"/>) e o CSV (<see cref="Csv"/>).
/// </summary>
public sealed class RelatorioDados
{
    public required string Nome { get; init; }
    public required string Titulo { get; init; }
    /// <summary>Período e filtros informados, numa linha (subtítulo do PDF).</summary>
    public required string Subtitulo { get; init; }
    public List<TabelaRelatorio> Tabelas { get; } = [];
    /// <summary>Observações de rodapé (ex.: regra de contagem).</summary>
    public List<string> Notas { get; } = [];

    /// <summary>CSV em UTF-8 com BOM e <c>;</c> (abre direto no Excel em português); tabelas separadas por uma linha em branco.</summary>
    public byte[] Csv()
    {
        static string C(string? s) => s is null ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
        var csv = new StringBuilder();
        csv.Append(C(Titulo)).Append("\r\n").Append(C(Subtitulo)).Append("\r\n");
        foreach (var t in Tabelas)
        {
            csv.Append("\r\n");
            if (t.Titulo is not null) csv.Append(C(t.Titulo)).Append("\r\n");
            csv.AppendJoin(';', t.Colunas.Select(c => C(c.Titulo))).Append("\r\n");
            foreach (var l in t.Linhas) csv.AppendJoin(';', l.Celulas.Select(C)).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }
}
