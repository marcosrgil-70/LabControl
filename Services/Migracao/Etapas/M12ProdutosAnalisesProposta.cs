using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>Item da proposta migrado: ID novo, produto novo e quantidade (usados pelo pedido e pela amostra).</summary>
public sealed record ItemProposta(int Id, int IdProduto, int Qtde);

/// <summary>
/// M-12 — produtos e análises da proposta (M-12-produtos-analises-proposta.md). <c>ITEM</c> = antigo
/// <c>ID_LAB_PROPOSTAS_PRODUTOS</c> (o nº do item). Valores <b>copiados</b> (INSERT direto: o EF não envia subtotal/total,
/// que fora da migração a trigger calcula). Itens que as amostras citam e a proposta não tem (propostas do legado sem
/// produtos) são criados com o produto da amostra, para o número e o vínculo da amostra continuarem válidos.
/// </summary>
public sealed class M12ProdutosAnalisesProposta : IEtapaMigracao
{
    public string Codigo => "M-12";
    public string Nome => "Produtos e análises da proposta";

    /// <summary><c>(proposta, item) do legado → item novo</c>.</summary>
    public const string Itens = "ITENS_PROPOSTA";
    /// <summary><c>(proposta, item, parâmetro, método) do legado → análises novas</c> (o teste da amostra aponta para ela).</summary>
    public const string Analises = "ANALISES_PROPOSTA";
    /// <summary><c>(proposta, item) do legado → Σ VR_TOTAL das análises</c> (valor do item do pedido).</summary>
    public const string TotalItem = "TOTAL_ITEM_PROPOSTA";

    private static readonly string[] ColunasItem = ["ID_EMPRESAS", "ID_LAB_PROPOSTAS", "ITEM", "ID_PRODUTOS", "QTDE_AMOSTRAS", "ID_ORIGEM"];
    private static readonly string[] ColunasAnalise =
    [
        "ID_EMPRESAS", "ID_LAB_PROPOSTAS", "ID_LAB_PROPOSTAS_PRODUTOS", "ID_LAB_PARAMETROS_ANALISES", "ID_ANALISES_METODOS",
        "ID_IDIOMAS", "ID_PRAZOS", "TIPO_DOCUMENTO", "QTDE_AMOSTRAS", "VR_UNITARIO", "VR_SUBTOTAL", "PORC_DESCONTO",
        "VR_DESCONTO", "VR_TOTAL", "ST_REV_ANTERIOR", "ID_ORIGEM",
    ];

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var propostas = c.Compartilhado<Dictionary<int, LinhaFb>>(M11Propostas.Legado);
        var itensLegado = (await c.Fb.LerAsync("SELECT ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_PRODUTOS, QTDE_AMOSTRAS FROM LAB_PROPOSTAS_PRODUTOS", ct))
            .ToLookup(l => l.IntObrigatorio("ID_LAB_PROPOSTAS"));
        var analisesLegado = (await c.Fb.LerAsync("""
            SELECT ID_LAB_PROPOSTAS_PRODANALISES, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_LAB_PARAMETROS_ANALISES,
                   ID_ANALISES_METODOS, ID_IDIOMAS, ID_PRAZOS, TIPO_DOCUMENTO, QTDE_AMOSTRAS, VR_UNITARIO, VR_SUBTOTAL,
                   PORC_DESCONTO, VR_DESCONTO, VR_TOTAL, ST_REV_ANTERIOR
              FROM LAB_PROPOSTAS_PRODANALISES ORDER BY ID_LAB_PROPOSTAS_PRODANALISES
            """, ct)).ToLookup(l => l.IntObrigatorio("ID_LAB_PROPOSTAS"));
        // itens citados por amostras e ausentes na proposta: o produto mais frequente entre as amostras do item
        var orfaos = (await c.Fb.LerAsync("""
            SELECT A.ID_LAB_PROPOSTAS, A.ID_LAB_PROPOSTAS_PRODUTOS AS ITEM, A.ID_PRODUTOS, COUNT(*) AS N
              FROM LAB_HIST_AMOSTRAS A
             WHERE NOT EXISTS (SELECT 1 FROM LAB_PROPOSTAS_PRODUTOS P
                                WHERE P.ID_LAB_PROPOSTAS = A.ID_LAB_PROPOSTAS AND P.ID_LAB_PROPOSTAS_PRODUTOS = A.ID_LAB_PROPOSTAS_PRODUTOS)
             GROUP BY A.ID_LAB_PROPOSTAS, A.ID_LAB_PROPOSTAS_PRODUTOS, A.ID_PRODUTOS
            """, ct)).GroupBy(l => (Proposta: l.IntObrigatorio("ID_LAB_PROPOSTAS"), Item: l.IntObrigatorio("ITEM")))
            .ToLookup(g => g.Key.Proposta);

        var itens = c.Compartilhado<Dictionary<(int Proposta, int Item), ItemProposta>>(Itens);
        var analises = c.Compartilhado<Dictionary<(int Proposta, int Item, int Parametro, int Metodo), List<int>>>(Analises);
        var totalItem = c.Compartilhado<Dictionary<(int Proposta, int Item), decimal>>(TotalItem);
        var porParametro = new Dictionary<(int Proposta, int Item, int Parametro), List<int>>();
        var (camadas, anterior) = M11Propostas.Revisoes(propostas.Values);
        int nItensLegado = 0, nItensCriados = 0, nAnalises = 0;

        foreach (var camada in camadas)
        {
            // itens
            var valores = new List<object?[]>();
            var chaves = new List<((int, int) Chave, int Produto, int Qtde)>();
            foreach (var p in camada)
            {
                var antigo = p.IntObrigatorio("ID_LAB_PROPOSTAS");
                if (c.Novo("LAB_PROPOSTAS", antigo, $"proposta {antigo}") is not { } idProposta) continue;
                var ant = anterior.TryGetValue(antigo, out var a) ? a : (int?)null;
                void Item(int item, int? produtoLegado, int qtde, string onde)
                {
                    var produto = c.Novo("PRODUTOS", produtoLegado, onde)
                        ?? throw new InvalidOperationException($"{onde}: produto {produtoLegado} não migrado.");
                    var origem = ant is { } x && itens.TryGetValue((x, item), out var o) ? o.Id : (int?)null;
                    valores.Add([c.IdEmpresa, idProposta, item, produto, qtde, origem]);
                    chaves.Add(((antigo, item), produto, qtde));
                }
                foreach (var l in itensLegado[antigo])
                {
                    var item = l.IntObrigatorio("ID_LAB_PROPOSTAS_PRODUTOS");
                    var onde = $"proposta {antigo} item {item}";
                    var qtde = l.Int("QTDE_AMOSTRAS") ?? 0;
                    if (qtde <= 0) { c.Anomalia($"{onde}: quantidade de amostras {qtde} — gravada como 1."); qtde = 1; }
                    Item(item, l.Int("ID_PRODUTOS"), qtde, onde);
                    nItensLegado++;
                }
                foreach (var g in orfaos[antigo])
                {
                    var onde = $"proposta {antigo} item {g.Key.Item}";
                    var maisFrequente = g.OrderByDescending(l => l.IntObrigatorio("N")).First();
                    if (g.Count() > 1) c.Anomalia($"{onde}: amostras do item com produtos diferentes — item criado com o produto {maisFrequente.Int("ID_PRODUTOS")}.");
                    var qtde = g.Sum(l => l.IntObrigatorio("N"));
                    c.Anomalia($"{onde}: citado por {qtde} amostra(s) e ausente na proposta do legado — item criado com o produto da amostra, sem análises.");
                    Item(g.Key.Item, maisFrequente.Int("ID_PRODUTOS"), qtde, onde);
                    nItensCriados++;
                }
            }
            var ids = await c.InserirEmLoteAsync("LAB_PROPOSTAS_PRODUTOS", "ID_LAB_PROPOSTAS_PRODUTOS", ColunasItem, valores, ct);
            foreach (var ((chave, produto, qtde), id) in chaves.Zip(ids)) itens[chave] = new ItemProposta(id, produto, qtde);

            // análises
            valores.Clear();
            var antigas = new List<(int Proposta, int Item, int Parametro, int Metodo, decimal Total)>();
            foreach (var p in camada)
            {
                var antigo = p.IntObrigatorio("ID_LAB_PROPOSTAS");
                if (c.Novo("LAB_PROPOSTAS", antigo, $"proposta {antigo}") is not { } idProposta) continue;
                var ant = anterior.TryGetValue(antigo, out var a) ? a : (int?)null;
                var vistos = new HashSet<(int, int)>();
                foreach (var l in analisesLegado[antigo])
                {
                    var onde = $"análise {l.IntObrigatorio("ID_LAB_PROPOSTAS_PRODANALISES")} da proposta {antigo}";
                    var item = l.IntObrigatorio("ID_LAB_PROPOSTAS_PRODUTOS");
                    if (!itens.TryGetValue((antigo, item), out var it)) { c.Anomalia($"{onde}: item {item} não existe — análise não migrada."); continue; }
                    var (parametro, metodo) = (l.IntObrigatorio("ID_LAB_PARAMETROS_ANALISES"), l.IntObrigatorio("ID_ANALISES_METODOS"));
                    if (!vistos.Add((item, parametro))) c.Anomalia($"{onde}: parâmetro {parametro} repetido no item {item} — migrado.");
                    var origem = ant is { } x && porParametro.TryGetValue((x, item, parametro), out var os) && os.Count == 1 ? os[0] : (int?)null;
                    var total = decimal.Round(l.Decimal("VR_TOTAL") ?? 0, 2);
                    valores.Add(
                    [
                        c.IdEmpresa, idProposta, it.Id,
                        c.Novo("LAB_PARAMETROS_ANALISES", parametro, onde) ?? throw new InvalidOperationException($"{onde}: parâmetro {parametro} não migrado."),
                        c.Novo("ANALISES_METODOS", metodo, onde) ?? throw new InvalidOperationException($"{onde}: método {metodo} não migrado."),
                        c.Novo("IDIOMAS", l.Int("ID_IDIOMAS"), onde) ?? throw new InvalidOperationException($"{onde}: idioma não migrado."),
                        c.Novo("PRAZOS", l.Int("ID_PRAZOS"), onde) ?? throw new InvalidOperationException($"{onde}: prazo não migrado."),
                        TipoDocumento(l.Texto("TIPO_DOCUMENTO")), l.Int("QTDE_AMOSTRAS") is > 0 and var q ? q : it.Qtde,
                        decimal.Round(l.Decimal("VR_UNITARIO") ?? 0, 2), decimal.Round(l.Decimal("VR_SUBTOTAL") ?? 0, 2),
                        decimal.Round(l.Decimal("PORC_DESCONTO") ?? 0, 4), decimal.Round(l.Decimal("VR_DESCONTO") ?? 0, 2), total,
                        l.Int("ST_REV_ANTERIOR") == 1, origem,
                    ]);
                    antigas.Add((antigo, item, parametro, metodo, total));
                }
            }
            ids = await c.InserirEmLoteAsync("LAB_PROPOSTAS_PRODANALISES", "ID_LAB_PROPOSTAS_PRODANALISES", ColunasAnalise, valores, ct);
            foreach (var ((proposta, item, parametro, metodo, total), id) in antigas.Zip(ids))
            {
                (analises.TryGetValue((proposta, item, parametro, metodo), out var l1) ? l1 : analises[(proposta, item, parametro, metodo)] = []).Add(id);
                (porParametro.TryGetValue((proposta, item, parametro), out var l2) ? l2 : porParametro[(proposta, item, parametro)] = []).Add(id);
                totalItem[(proposta, item)] = totalItem.GetValueOrDefault((proposta, item)) + total;
                nAnalises++;
            }
        }

        // itens/análises de propostas excluídas no legado (sem cabeçalho) não têm onde entrar
        var semProposta = itensLegado.Where(g => !propostas.ContainsKey(g.Key)).ToList();
        var analisesSemProposta = analisesLegado.Where(g => !propostas.ContainsKey(g.Key)).Sum(g => g.Count());
        if (semProposta.Count > 0 || analisesSemProposta > 0)
            c.Anomalia($"{semProposta.Sum(g => g.Count())} produto(s) e {analisesSemProposta} análise(s) de {semProposta.Count} proposta(s) excluída(s) no legado " +
                $"({string.Join(", ", semProposta.Select(g => g.Key).Order())}) — não migrados.");
        c.Conferir("produtos da proposta do legado (− propostas excluídas)", itensLegado.Sum(g => g.Count()) - semProposta.Sum(g => g.Count()), nItensLegado);
        c.Conferir("análises da proposta do legado (− propostas excluídas)", analisesLegado.Sum(g => g.Count()) - analisesSemProposta, nAnalises);
        // a soma das análises bate com o total do cabeçalho onde já batia no legado (cópia fiel, sem recálculo)
        var divergentesLegado = propostas.Values.Count(p =>
            analisesLegado[p.IntObrigatorio("ID_LAB_PROPOSTAS")].Sum(l => decimal.Round(l.Decimal("VR_TOTAL") ?? 0, 2)) != decimal.Round(p.Decimal("VR_SOMA_VR_TOT_ANALIS") ?? 0, 2));
        var somas = await c.Db.LabPropostasProdanalises.GroupBy(a => a.IdLabPropostas)
            .Select(g => new { g.Key, Soma = g.Sum(a => a.VrTotal) }).ToDictionaryAsync(x => x.Key, x => x.Soma, ct);
        var divergentes = (await c.Db.LabPropostas.Select(p => new { p.IdLabPropostas, p.VrSomaVrTotAnalis }).ToListAsync(ct))
            .Count(p => somas.GetValueOrDefault(p.IdLabPropostas) != p.VrSomaVrTotAnalis);
        c.Conferir("propostas com Σ das análises ≠ total das análises (igual ao legado)", divergentesLegado, divergentes);
        c.Log($"{nItensLegado} itens do legado + {nItensCriados} criados para amostras; {nAnalises} análises.");
        return nItensLegado + nItensCriados + nAnalises;
    }

    /// <summary>"Boletim"/"Relatorio" do legado → valor do ENUM (vazio = BOLETIM).</summary>
    public static string TipoDocumento(string? valor) =>
        Conversoes.NomeComparavel(valor) == "RELATORIO" ? "RELATORIO" : "BOLETIM";
}
