namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// Uma etapa da migração (docs/processos/migracao/M-xx). Roda dentro da transação aberta pelo
/// <see cref="MigracaoService"/>; grava pelo EF (IDs novos relidos para os dicionários) e registra conferências e
/// anomalias no contexto. Devolve quantos registros gravou.
/// </summary>
public interface IEtapaMigracao
{
    string Codigo { get; }
    string Nome { get; }
    Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct);
}

/// <summary>Apoio comum às etapas.</summary>
internal static class Migrar
{
    /// <summary>
    /// Auxiliar do legado → tabela do v2 (M-03): uma linha por registro, mapa pelo ID devolvido no INSERT (os
    /// auxiliares não têm ID_LEGADO). Chave única repetida no legado (comparada como o MySQL: sem acento/maiúsculas)
    /// aponta para o mesmo registro e vira anomalia — a UNIQUE do destino recusaria a 2ª linha.
    /// </summary>
    public static async Task<int> AuxiliarAsync<T>(ContextoMigracao c, string tabela, string colunaId, string colunas,
        Func<LinhaFb, int, T> criar, Func<T, string>? chave, Func<T, int> id, CancellationToken ct) where T : class
    {
        var linhas = await c.Fb.LerAsync($"SELECT {colunaId}, {colunas} FROM {tabela} ORDER BY {colunaId}", ct);
        var porChave = new Dictionary<string, T>();
        var todos = new List<(int Antigo, T Novo)>();
        foreach (var l in linhas)
        {
            var antigo = l.IntObrigatorio(colunaId);
            var novo = criar(l, antigo);
            var k = chave is null ? antigo.ToString() : Conversoes.NomeComparavel(chave(novo)); // sem UNIQUE no destino: um por ID
            if (porChave.TryGetValue(k, out var existente))
            {
                c.Anomalia($"{tabela} {antigo}: \"{chave!(novo)}\" repetido no legado — aponta para o mesmo registro.");
                todos.Add((antigo, existente));
                continue;
            }
            porChave[k] = novo;
            c.Db.Add(novo);
            todos.Add((antigo, novo));
        }
        await c.Db.SaveChangesAsync(ct);
        var mapa = c.Mapa(tabela);
        foreach (var (antigo, novo) in todos) mapa[antigo] = id(novo);
        c.Conferir($"{tabela}: registros do legado mapeados", linhas.Count, mapa.Count);
        return porChave.Count;
    }
}
