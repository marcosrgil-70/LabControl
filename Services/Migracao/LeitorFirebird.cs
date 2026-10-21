using System.Globalization;
using FirebirdSql.Data.FirebirdClient;

namespace LabControl.Services.Migracao;

/// <summary>Uma linha lida do Firebird, com leitura tolerante (nulos, textos aparados, números em texto).</summary>
public sealed class LinhaFb(Dictionary<string, object?> valores)
{
    private object? Bruto(string coluna) =>
        valores.TryGetValue(coluna, out var v) ? v is DBNull ? null : v
            : throw new KeyNotFoundException($"Coluna {coluna} não veio na consulta do Firebird.");

    /// <summary>Texto aparado; vazio vira null.</summary>
    public string? Texto(string coluna) => Bruto(coluna) switch
    {
        null => null,
        byte[] b => Infra.Textos.Limpo(System.Text.Encoding.Latin1.GetString(b)),
        var v => Infra.Textos.Limpo(Convert.ToString(v, CultureInfo.InvariantCulture)),
    };

    public int? Int(string coluna) => Bruto(coluna) switch
    {
        null => null,
        string s => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null,
        var v => Convert.ToInt32(v, CultureInfo.InvariantCulture),
    };

    public int IntObrigatorio(string coluna) => Int(coluna) ?? throw new InvalidOperationException($"{coluna} nulo no legado.");

    public decimal? Decimal(string coluna) => Bruto(coluna) switch
    {
        null => null,
        var v => Convert.ToDecimal(v, CultureInfo.InvariantCulture),
    };

    public DateOnly? Data(string coluna) => Bruto(coluna) switch
    {
        null => null,
        DateTime d => DateOnly.FromDateTime(d),
        _ => null,
    };

    /// <summary>'S'/'N' (ou 1/0) → bool; nulo → false.</summary>
    public bool Flag(string coluna) => Conversoes.Flag(Texto(coluna));
}

/// <summary>
/// Conexão de leitura com o Firebird do legado (<c>DBMAIS.FDB</c>). Só SELECT: a migração nunca altera a origem.
/// Charset ISO8859_1, como a v1 (os textos chegam com os acentos certos).
/// </summary>
public sealed class LeitorFirebird(FbConnection conexao) : IAsyncDisposable
{
    public static async Task<LeitorFirebird> AbrirAsync(string caminho, string usuario, string senha, CancellationToken ct = default)
    {
        var cs = new FbConnectionStringBuilder
        {
            DataSource = "localhost", Port = 3050, Database = caminho, UserID = usuario, Password = senha,
            Charset = "ISO8859_1", ServerType = FbServerType.Default, Pooling = false,
        };
        var con = new FbConnection(cs.ToString());
        await con.OpenAsync(ct);
        return new LeitorFirebird(con);
    }

    /// <summary>Todas as linhas da consulta (as tabelas da migração cabem na memória: a maior da parte 1 tem 1.915).</summary>
    public async Task<List<LinhaFb>> LerAsync(string sql, CancellationToken ct = default)
    {
        await using var cmd = new FbCommand(sql, conexao);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var linhas = new List<LinhaFb>();
        while (await r.ReadAsync(ct))
        {
            var v = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < r.FieldCount; i++) v[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            linhas.Add(new LinhaFb(v));
        }
        return linhas;
    }

    public async Task<int> ContarAsync(string tabelaOuSql, CancellationToken ct = default)
    {
        var sql = tabelaOuSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ? tabelaOuSql : $"SELECT COUNT(*) FROM {tabelaOuSql}";
        await using var cmd = new FbCommand(sql, conexao);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    public ValueTask DisposeAsync() => conexao.DisposeAsync();
}
