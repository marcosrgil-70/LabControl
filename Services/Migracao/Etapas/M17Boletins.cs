using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-17 — boletins (M-17-boletins.md). Todos <b>emitidos</b> (<c>DH_EMISSAO = DH_GERACAO</c>, D-11) e com o status de
/// emitido da empresa; revisão renumerada 0..n por código (a amostra física), origem = anterior; textos da
/// <c>LAB_BOLETIM_OBS</c>. O boletim fica na amostra do resultado (a <c>TG_BOLETIM_BI</c> exige; 7 do legado apontavam
/// para a vizinha — decisão do usuário, 5.1) e o código sai da trigger (número base da amostra), conferido com o
/// impresso. Nas linhas, <c>RESULTADO_CONCAT</c> como impresso; parâmetro apagado ou de outro resultado é trocado pelo
/// equivalente do resultado do boletim; unidade, especificação, descritivo e acreditação vêm do parâmetro vinculado (o
/// legado lia isso ao vivo na impressão).
/// </summary>
public sealed class M17Boletins : IEtapaMigracao
{
    public string Codigo => "M-17";
    public string Nome => "Boletins";

    private static readonly string[] ColunasBoletim =
    [
        "ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "ID_LAB_RESULTADOS_ANALISES", "COD_BOLETIM", "REVISAO", "ID_BOLETIM_ORIGEM",
        "ULTIMA_REVISAO", "ID_BOLETINS_STATUS", "DT_BOLETIM", "DH_GERACAO", "DH_EMISSAO", "ID_ENTIDADES_FUNC_GER",
        "ID_ENTIDADES_FUNC_EMI", "ID_ENTIDADES_FUNC_RESP", "ID_ENTIDADES_FUNC_RESP2", "TXT_OBSERVACAO", "TXT_LEGENDA",
        "TXT_METODOLOGIA_UTIL", "TXT_METODOLOGIA_ACRED", "ID_LEGADO",
    ];
    private static readonly string[] ColunasParam =
        ["ID_EMPRESAS", "ID_LAB_BOLETIM_ANALISES", "ID_LAB_RESULTADOS_PARAM", "ORDEM", "DESC_PARAMETRO", "RESULTADO_CONCAT"];

    private sealed record Param(int Id, int Resultado, int Parametro, int Metodo, string Descricao);

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var boletins = await c.Fb.LerAsync("""
            SELECT ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADO_ANALISES, ID_LAB_HIST_AMOSTRAS, COD_BOLETIM, REVISAO, DH_GERACAO,
                   DT_BOLETIM, ID_ENTIDADES_FUNC_GER, ID_ENTIDADES_FUNC_EMI, ID_ENTIDADES_FUNC_RESP, ID_ENTIDADES_FUNC_RESP2
              FROM LAB_BOLETIM_ANALISES ORDER BY ID_LAB_BOLETIM_ANALISES
            """, ct);
        var obs = new Dictionary<int, LinhaFb>();
        foreach (var o in await c.Fb.LerAsync("""
            SELECT ID_LAB_BOLETIM_OBS, ID_LAB_BOLETIM_ANALISES, TXT_OBSERVACAO, TXT_LEGENDA, TXT_METODOLOGIA_UTIL, TXT_METODOLOGIA_ACRED
              FROM LAB_BOLETIM_OBS ORDER BY ID_LAB_BOLETIM_OBS
            """, ct))
        {
            var bol = o.IntObrigatorio("ID_LAB_BOLETIM_ANALISES");
            if (obs.ContainsKey(bol)) c.Anomalia($"boletim {bol}: mais de uma observação no legado — fica a mais recente ({o.IntObrigatorio("ID_LAB_BOLETIM_OBS")}).");
            obs[bol] = o;
        }
        var linhas = (await c.Fb.LerAsync("""
            SELECT ID_LAB_BOLETIM_PARAMETROS, ID_LAB_BOLETIM_ANALISES, ID_LAB_RESULTADOS_PARAM, DESC_PARAMETRO, RESULTADO_CONCAT
              FROM LAB_BOLETIM_PARAMETROS ORDER BY ID_LAB_BOLETIM_PARAMETROS
            """, ct)).ToLookup(l => l.IntObrigatorio("ID_LAB_BOLETIM_ANALISES"));

        var status = await c.Db.EmpresasParametros.Select(p => p.IdBoletinsStatusEmitido).SingleAsync(ct);
        if (status is null) c.Anomalia("Status de boletim emitido não definido nos parâmetros: boletins migrados sem status (defina na T05).");
        // resultado novo → amostra e raiz (o boletim fica na amostra do resultado; o código é o da amostra física)
        var resultados = await c.Db.LabResultadosAnalises
            .Select(r => new { r.IdLabResultadosAnalises, r.IdLabHistAmostras, Raiz = r.LabHistAmostra.IdAmostraRaiz ?? r.IdLabHistAmostras })
            .ToDictionaryAsync(r => r.IdLabResultadosAnalises, ct);
        var parametros = (await c.Db.LabResultadosParam
            .Select(p => new Param(p.IdLabResultadosParam, p.IdLabResultadosAnalises, p.LabHistAmostraTeste.IdLabParametrosAnalises,
                p.LabHistAmostraTeste.IdAnalisesMetodos, p.LabHistAmostraTeste.LabParametroAnalise.DescReduzida))
            .ToListAsync(ct)).ToDictionary(p => p.Id);
        var doResultado = parametros.Values.ToLookup(p => p.Resultado);
        var mapaPar = c.Mapa("LAB_RESULTADOS_PARAM");
        var mapa = c.Mapa("LAB_BOLETIM_ANALISES");

        var migrar = new List<(LinhaFb B, int Resultado, int Amostra)>();
        var amostraTrocada = new HashSet<int>();
        foreach (var b in boletins)
        {
            var antigo = b.IntObrigatorio("ID_LAB_BOLETIM_ANALISES");
            if (c.Novo("LAB_RESULTADOS_ANALISES", b.Int("ID_LAB_RESULTADO_ANALISES"), $"boletim {antigo}") is not { } res)
                continue; // anomalia já registrada
            var amostra = resultados[res].IdLabHistAmostras;
            if (c.Mapa("LAB_HIST_AMOSTRAS").GetValueOrDefault(b.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")) != amostra)
            {
                amostraTrocada.Add(antigo);
                c.Anomalia($"boletim {antigo} ({b.Texto("COD_BOLETIM")}): amostra {b.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")} diferente da do resultado " +
                    $"{b.IntObrigatorio("ID_LAB_RESULTADO_ANALISES")} — gravado na amostra do resultado.");
            }
            migrar.Add((b, res, amostra));
        }
        // cadeia de revisões por código = por amostra física
        var cadeias = migrar.GroupBy(x => resultados[x.Resultado].Raiz)
            .Select(g => g.OrderBy(x => x.B.Int("REVISAO") ?? 0).ThenBy(x => x.B.IntObrigatorio("ID_LAB_BOLETIM_ANALISES")).ToList()).ToList();

        int trocados = 0, naoMigrados = 0, totalPar = 0;
        for (var camada = 0; camada < (cadeias.Count == 0 ? 0 : cadeias.Max(cd => cd.Count)); camada++)
        {
            var nivel = cadeias.Where(cd => cd.Count > camada).Select(cd => (Cadeia: cd, X: cd[camada])).ToList();
            var valores = new List<object?[]>();
            foreach (var (cadeia, (b, res, amostra)) in nivel)
            {
                var antigo = b.IntObrigatorio("ID_LAB_BOLETIM_ANALISES");
                var onde = $"boletim {antigo}";
                var dt = b.Data("DT_BOLETIM") ?? DateOnly.FromDateTime(b.DataHora("DH_GERACAO")!.Value);
                var geracao = b.DataHora("DH_GERACAO") ?? dt.ToDateTime(TimeOnly.MinValue);
                var o = obs.GetValueOrDefault(antigo);
                int? Func(string coluna) => b.Int(coluna) is > 0 and var v ? c.Novo("ENTIDADES_FUNCIONARIOS", v, onde) : null;
                valores.Add(
                [
                    c.IdEmpresa, amostra, res, "", camada, camada == 0 ? null : mapa[cadeia[camada - 1].B.IntObrigatorio("ID_LAB_BOLETIM_ANALISES")],
                    camada == cadeia.Count - 1, status, dt, geracao, geracao,
                    Func("ID_ENTIDADES_FUNC_GER") ?? throw new InvalidOperationException($"{onde}: quem gerou não foi migrado."),
                    Func("ID_ENTIDADES_FUNC_EMI"), Func("ID_ENTIDADES_FUNC_RESP"), Func("ID_ENTIDADES_FUNC_RESP2"),
                    o?.Texto("TXT_OBSERVACAO"), o?.Texto("TXT_LEGENDA"), o?.Texto("TXT_METODOLOGIA_UTIL"), o?.Texto("TXT_METODOLOGIA_ACRED"), antigo,
                ]);
            }
            var ids = await c.InserirEmLoteAsync("LAB_BOLETIM_ANALISES", "ID_LAB_BOLETIM_ANALISES", ColunasBoletim, valores, ct);
            foreach (var (x, id) in nivel.Zip(ids)) mapa[x.X.B.IntObrigatorio("ID_LAB_BOLETIM_ANALISES")] = id;

            var valoresPar = new List<object?[]>();
            foreach (var (_, (b, res, _)) in nivel)
            {
                var antigo = b.IntObrigatorio("ID_LAB_BOLETIM_ANALISES");
                // o legado deixava no boletim as linhas de parâmetros apagados e relançados: a linha ligada ao próprio
                // resultado tem a vez; a antiga só entra (religada ao equivalente) se o parâmetro ainda não estiver lá
                var resolvidas = new List<(LinhaFb L, int? Par, bool Direta, string Onde)>();
                foreach (var l in linhas[antigo])
                {
                    var onde = $"boletim {antigo} ({b.Texto("COD_BOLETIM")}), linha {l.IntObrigatorio("ID_LAB_BOLETIM_PARAMETROS")}";
                    var desc = l.Texto("DESC_PARAMETRO") ?? "";
                    int? par = mapaPar.TryGetValue(l.IntObrigatorio("ID_LAB_RESULTADOS_PARAM"), out var p) ? p : null;
                    if (par is { } atual && parametros[atual].Resultado == res) { resolvidas.Add((l, par, true, onde)); continue; }
                    var eq = par is { } outro
                        ? doResultado[res].Where(x => (x.Parametro, x.Metodo) == (parametros[outro].Parametro, parametros[outro].Metodo)).ToList()
                        : doResultado[res].Where(x => string.Equals(x.Descricao, desc, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (eq.Count == 1) par = eq[0].Id;
                    else if (par is not null)
                        c.Anomalia($"{onde}: parâmetro \"{desc}\" é de outro resultado e não tem equivalente único no resultado do boletim — mantido.");
                    resolvidas.Add((l, par, false, onde));
                }
                var usados = new HashSet<int>();
                var entram = new HashSet<LinhaFb>();
                foreach (var (l, par, direta, onde) in resolvidas.OrderBy(r => r.Direta ? 0 : 1))
                {
                    var desc = l.Texto("DESC_PARAMETRO");
                    var concat = l.Texto("RESULTADO_CONCAT");
                    if (par is null)
                        c.Anomalia($"{onde}: parâmetro \"{desc}\" (impresso \"{concat}\") foi apagado no legado e não tem equivalente no resultado — linha não migrada.");
                    else if (!usados.Add(par.Value))
                        c.Anomalia(direta
                            ? $"{onde}: parâmetro \"{desc}\" (impresso \"{concat}\") repetido no boletim — linha não migrada."
                            : $"{onde}: linha antiga do parâmetro \"{desc}\" (impresso \"{concat}\"), apagado ou de outro resultado, já coberta pela linha atual — não migrada.");
                    else
                    {
                        entram.Add(l);
                        if (!direta) trocados++;
                    }
                }
                naoMigrados += resolvidas.Count - entram.Count;
                var ordem = 0;
                foreach (var (l, par, _, onde) in resolvidas.Where(r => entram.Contains(r.L)))
                    valoresPar.Add([c.IdEmpresa, mapa[antigo], par, ++ordem,
                        c.Cortar<LabBoletimParametro>(nameof(LabBoletimParametro.DescParametro), l.Texto("DESC_PARAMETRO") ?? "", onde), l.Texto("RESULTADO_CONCAT")]);
            }
            totalPar += (await c.InserirEmLoteAsync("LAB_BOLETIM_PARAMETROS", "ID_LAB_BOLETIM_PARAMETROS", ColunasParam, valoresPar, ct)).Count;
        }

        // o que o legado lia do resultado na hora de imprimir fica congelado na linha
        await c.Db.Database.ExecuteSqlRawAsync("""
            UPDATE LAB_BOLETIM_PARAMETROS BP
              JOIN LAB_RESULTADOS_PARAM RP ON RP.ID_LAB_RESULTADOS_PARAM = BP.ID_LAB_RESULTADOS_PARAM
              JOIN LAB_HIST_AMOSTRAS_TESTES T ON T.ID_LAB_HIST_AMOSTRAS_TESTES = RP.ID_LAB_HIST_AMOSTRAS_TESTES
              JOIN LAB_PARAMETROS_ANALISES PA ON PA.ID_LAB_PARAMETROS_ANALISES = T.ID_LAB_PARAMETROS_ANALISES
              LEFT JOIN UNIDADES U ON U.ID_UNIDADES = RP.ID_UNIDADES
              LEFT JOIN LAB_TIPOS_RESULTADOS TR ON TR.ID_LAB_TIPOS_RESULTADOS = RP.ID_LAB_TIPOS_RESULTADOS
               SET BP.TIPO_RESULTADO_DESC = CASE WHEN RP.TIPO_RESULTADO = 0 THEN TR.DESCRICAO END,
                   BP.UNIDADE_SG = CASE WHEN RP.TIPO_RESULTADO = 1 THEN U.SIGLA END,
                   BP.ESPECIFICACAO = RP.ESPECIF_FORMATADA,
                   BP.ACREDITADO = PA.AUDITADO
             WHERE BP.ID_EMPRESAS = {0}
            """, [c.IdEmpresa], ct);

        var codigos = await c.Db.LabBoletimAnalises.Select(b => new { b.IdLegado, b.CodBoletim }).ToListAsync(ct);
        var impresso = boletins.ToDictionary(b => b.IntObrigatorio("ID_LAB_BOLETIM_ANALISES"), b => Base(b.Texto("COD_BOLETIM")));
        // diferença explicada: amostra corrigida acima, amostra do ano 0 (renumerada na M-14) ou código impresso que já não
        // era o número da própria amostra no legado
        var amostras = c.Compartilhado<Dictionary<int, LinhaFb>>(M14Amostras.Legado);
        var amostraDoBoletim = boletins.ToDictionary(b => b.IntObrigatorio("ID_LAB_BOLETIM_ANALISES"), b => amostras.GetValueOrDefault(b.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")));
        var diferentes = 0;
        foreach (var b in codigos.Where(b => impresso[b.IdLegado!.Value] != b.CodBoletim))
        {
            var antigo = b.IdLegado!.Value;
            var a = amostraDoBoletim[antigo];
            var motivo = amostraTrocada.Contains(antigo) ? "amostra corrigida"
                : a?.Int("ANO_AMOSTRA") == 0 ? "amostra do ano 0"
                : a is not null && impresso[antigo] != NumeroNoLegado(a) ? $"o número da amostra no legado era {NumeroNoLegado(a)}"
                : null;
            if (motivo is null) diferentes++;
            c.Anomalia($"boletim {antigo}: código impresso {impresso[antigo]} — no v2 {b.CodBoletim}{(motivo is null ? "" : $" ({motivo})")}.");
        }
        c.Conferir("boletins com código diferente do impresso sem explicação", 0, diferentes);
        c.Conferir("boletins do legado", boletins.Count, codigos.Count);
        c.Conferir("linhas de boletim (legado − não migradas)", linhas.Sum(g => g.Count()) - naoMigrados, await c.Db.LabBoletimParametros.CountAsync(ct));
        c.Conferir("códigos com exatamente uma última revisão", 0,
            await c.Db.LabBoletimAnalises.GroupBy(b => b.CodBoletim).Select(g => g.Sum(b => b.UltimaRevisao ? 1 : 0)).CountAsync(n => n != 1, ct));
        c.Conferir("boletins não emitidos", 0, await c.Db.LabBoletimAnalises.CountAsync(b => b.DhEmissao == null, ct));
        c.Log($"{codigos.Count} boletins, {totalPar} linhas; {trocados} linha(s) religada(s) ao parâmetro equivalente, {naoMigrados} não migrada(s).");
        await ConcluirPedidosAsync(c, ct);
        return codigos.Count + totalPar;
    }

    /// <summary>
    /// Pendência 23 (5.5): a M-13 deixou para cá o <c>CONCLUIDO</c> dos pedidos (P08: itens recebidos + boletins emitidos).
    /// Concluído = em execução, cada item recebeu ao menos a quantidade contratada (amostras revisão 0 do item, como a
    /// <c>VW_PEDIDOS_VENDA_SALDO</c>) e toda amostra vigente do pedido tem o boletim vigente da amostra física emitido.
    /// Com tudo emitido mas faltando amostra, continua em execução (anomalia). Calculado aqui com consultas simples: a
    /// view, correlacionada item a item, passava dos 30 s nos 3.318 itens do legado.
    /// </summary>
    private static async Task ConcluirPedidosAsync(ContextoMigracao c, CancellationToken ct)
    {
        var emExecucao = await c.Db.PedidosVenda.Where(p => p.Situacao == SituacaoPedido.EmExecucao)
            .Select(p => new { p.IdPedidosVenda, p.AnoProposta, p.CodProposta }).ToListAsync(ct);
        var itens = await c.Db.PedidosVendaItens
            .Select(i => new { i.IdPedidosVenda, i.LabPropostaProduto.Item, i.QtdeContratada }).ToListAsync(ct);
        var recebidas = (await c.Db.LabHistAmostras.Where(a => a.Revisao == 0)
                .Select(a => new { a.LabProposta.AnoProposta, a.LabProposta.CodProposta, a.LabPropostaProduto.Item }).ToListAsync(ct))
            .GroupBy(a => a).ToDictionary(g => g.Key, g => g.Count());
        var vigentes = (await c.Db.LabHistAmostras.Where(a => a.UltimaRevisao && a.IdPedidosVenda != null)
            .Select(a => new { Pedido = a.IdPedidosVenda!.Value, Raiz = a.IdAmostraRaiz ?? a.IdLabHistAmostras }).ToListAsync(ct)).ToLookup(a => a.Pedido, a => a.Raiz);
        var emitidas = (await c.Db.LabBoletimAnalises.Where(b => b.UltimaRevisao && b.DhEmissao != null)
            .Select(b => b.LabHistAmostra.IdAmostraRaiz ?? b.IdLabHistAmostras).ToListAsync(ct)).ToHashSet();
        var itensDoPedido = itens.ToLookup(i => i.IdPedidosVenda);

        var concluir = new List<int>();
        var faltando = 0;
        foreach (var p in emExecucao)
        {
            if (!vigentes[p.IdPedidosVenda].All(emitidas.Contains)) continue;
            var tudoRecebido = itensDoPedido[p.IdPedidosVenda].All(i =>
                recebidas.GetValueOrDefault(new { p.AnoProposta, p.CodProposta, i.Item }) >= i.QtdeContratada);
            if (tudoRecebido) concluir.Add(p.IdPedidosVenda);
            else faltando++;
        }
        var concluidos = concluir.Count == 0 ? 0 : await c.Db.PedidosVenda.Where(p => concluir.Contains(p.IdPedidosVenda))
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.Situacao, SituacaoPedido.Concluido), ct);
        if (faltando > 0)
            c.Anomalia($"{faltando} pedido(s) com todas as amostras com boletim emitido, mas com item recebido abaixo do contratado — ficam em execução (concluir na T21, se for o caso).");
        c.Conferir("pedidos concluídos (itens recebidos e boletins emitidos)", concluir.Count, concluidos);
        c.Log($"{concluidos} pedido(s) de venda concluídos (itens recebidos e boletins emitidos); {faltando} em execução com item faltando.");
    }

    // número base como o legado montava (VW_LAB_HIST_AMOSTRA; os códigos dos tipos são os IDs do legado)
    private static string NumeroNoLegado(LinhaFb a) =>
        $"{a.IntObrigatorio("ID_AMOSTRAS_TIPO"):00}-{a.IntObrigatorio("COD_AMOSTRA"):00000}-{a.IntObrigatorio("ID_ANALISES_TIPO"):00}/{a.IntObrigatorio("ANO_AMOSTRA") % 100:00}";

    // "02-01220-01/18 R01" → "02-01220-01/18"
    private static string? Base(string? codigo) => codigo is null ? null : codigo.Split(" R", 2)[0].Trim();
}
