using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-02 — países, estados e cidades (M-02-geografia.md). O seed já tem o Brasil, as 27 UFs e os 5.571 municípios do
/// IBGE (D-21): aqui só se casa o legado com eles. UF pela sigla; cidade pelo IBGE (quando é um código válido) ou por
/// nome + UF — no legado só 1 das 190 tem código IBGE de verdade. Sem casamento: inclui sem código e lista.
/// </summary>
public sealed class M02Geografia : IEtapaMigracao
{
    public string Codigo => "M-02";
    public string Nome => "Geografia";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var brasil = await c.Db.Paises.SingleAsync(p => p.CodigoBacen == "1058", ct);
        foreach (var p in await c.Fb.LerAsync("SELECT ID_PAISES, NOME FROM PAISES", ct))
        {
            if (Conversoes.NomeComparavel(p.Texto("NOME")) != "BRASIL") c.Anomalia($"País {p.Texto("NOME")} do legado tratado como Brasil.");
            c.Mapa("PAISES")[p.IntObrigatorio("ID_PAISES")] = brasil.IdPaises;
        }

        var ufs = await c.Db.Estados.Where(e => e.IdPaises == brasil.IdPaises).ToDictionaryAsync(e => e.Sigla, ct);
        var estados = await c.Fb.LerAsync("SELECT ID_ESTADOS, SIGLA, NOME FROM ESTADOS ORDER BY ID_ESTADOS", ct);
        foreach (var e in estados)
        {
            if (e.Texto("SIGLA")?.ToUpperInvariant() is { } sigla && ufs.TryGetValue(sigla, out var uf)) c.Mapa("ESTADOS")[e.IntObrigatorio("ID_ESTADOS")] = uf.IdEstados;
            else c.Anomalia($"Estado {e.IntObrigatorio("ID_ESTADOS")} \"{e.Texto("NOME")}\" (sigla \"{e.Texto("SIGLA")}\") não é uma UF: ignorado.");
        }

        var seed = await c.Db.Cidades.AsNoTracking().Select(x => new { x.IdCidades, x.IdEstados, x.Nome, x.CodigoIbge }).ToListAsync(ct);
        var porIbge = seed.Where(x => x.CodigoIbge != null).ToDictionary(x => x.CodigoIbge!, x => x.IdCidades);
        var porNome = seed.GroupBy(x => (x.IdEstados, Conversoes.NomeComparavel(x.Nome))).ToDictionary(g => g.Key, g => g.First().IdCidades);

        var cidades = await c.Fb.LerAsync("SELECT ID_CIDADES, ID_ESTADOS, NOME, CODIGO_IBGE FROM CIDADES ORDER BY ID_CIDADES", ct);
        int peloIbge = 0, peloNome = 0;
        var novas = new Dictionary<(int Uf, string Nome), Cidade>(); // a mesma cidade repetida no legado (ARIRANHA ×3) entra uma vez
        var mapaNovas = new List<(int Antigo, Cidade Cidade)>();
        foreach (var l in cidades)
        {
            var antigo = l.IntObrigatorio("ID_CIDADES");
            var nome = l.Texto("NOME") ?? "";
            if (c.Novo("ESTADOS", l.Int("ID_ESTADOS"), $"cidade {antigo} {nome}") is not { } uf) continue;
            if (l.Texto("CODIGO_IBGE") is { Length: 7 } ibge && ibge.All(char.IsAsciiDigit) && porIbge.TryGetValue(ibge, out var porCodigo))
            { c.Mapa("CIDADES")[antigo] = porCodigo; peloIbge++; continue; }
            var chave = (uf, Conversoes.NomeComparavel(nome));
            if (porNome.TryGetValue(chave, out var porNomeUf))
            { c.Mapa("CIDADES")[antigo] = porNomeUf; peloNome++; continue; }
            if (!novas.TryGetValue(chave, out var cidade))
            {
                cidade = new Cidade { IdEstados = uf, Nome = c.Cortar<Cidade>(nameof(Cidade.Nome), nome.ToUpperInvariant(), $"cidade {antigo}")! };
                c.Db.Add(cidade);
                novas[chave] = cidade;
                c.Anomalia($"Cidade {antigo} \"{cidade.Nome}\" não encontrada no IBGE: incluída sem código (conferir o nome na T11-03).");
            }
            mapaNovas.Add((antigo, cidade));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, cidade) in mapaNovas) c.Mapa("CIDADES")[antigo] = cidade.IdCidades;

        c.Log($"Cidades: {peloIbge} pelo IBGE, {peloNome} por nome + UF, {novas.Count} incluídas sem código.");
        c.Conferir("UFs do legado casadas pela sigla", estados.Count(e => e.Texto("SIGLA")?.Length == 2), c.Mapa("ESTADOS").Count);
        var usadasNoLegado = await c.Fb.ContarAsync("""
            SELECT COUNT(DISTINCT B.ID_CIDADES) FROM ENTIDADES_ENDERECOS EE
              JOIN LOGRADOUROS L ON L.ID_LOGRADOUROS = EE.ID_LOGRADOUROS
              JOIN CIDADES_BAIRROS B ON B.ID_CIDADES_BAIRROS = L.ID_CIDADES_BAIRROS
            """, ct);
        var usadas = (await c.Fb.LerAsync("""
            SELECT DISTINCT B.ID_CIDADES FROM ENTIDADES_ENDERECOS EE
              JOIN LOGRADOUROS L ON L.ID_LOGRADOUROS = EE.ID_LOGRADOUROS
              JOIN CIDADES_BAIRROS B ON B.ID_CIDADES_BAIRROS = L.ID_CIDADES_BAIRROS
            """, ct)).Count(l => c.Mapa("CIDADES").ContainsKey(l.IntObrigatorio("ID_CIDADES")));
        c.Conferir("cidades usadas em endereços com correspondente", usadasNoLegado, usadas);
        return novas.Count;
    }
}
