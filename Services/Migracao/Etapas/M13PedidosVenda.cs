using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-13 — pedidos de venda (M-13-pedidos-venda.md). No legado o nº do pedido ficava na proposta e a revisão seguinte
/// o copiava: aqui é <b>1 pedido por proposta</b> (cód./ano), com o número da maior revisão que o tem e apontando para
/// ela; números a mais na mesma proposta apontam para esse pedido (anomalia). <c>NR_PEDIDO</c> preservado (INSERT
/// direto); itens = produtos dessa revisão com Σ das análises. CONCLUIDO depende dos boletins (M-17, 5.1).
/// </summary>
public sealed class M13PedidosVenda : IEtapaMigracao
{
    public string Codigo => "M-13";
    public string Nome => "Pedidos de venda";

    /// <summary><c>(ano, cód.) da proposta → pedido novo</c> (amostras sem pedido no legado herdam o da proposta).</summary>
    public const string PedidoDaProposta = "PEDIDO_DA_PROPOSTA";

    private static readonly string[] Colunas =
    [
        "ID_EMPRESAS", "NR_PEDIDO", "DT_PEDIDO", "ID_LAB_PROPOSTAS", "COD_PROPOSTA", "ANO_PROPOSTA", "ID_ENTIDADES",
        "ID_ENT_COBRANCA", "ID_CONDICOES_PAGTOS", "ID_MOEDAS", "DT_AUTORIZACAO", "VR_TOTAL", "SITUACAO",
    ];
    private static readonly string[] ColunasItem = ["ID_EMPRESAS", "ID_PEDIDOS_VENDA", "ID_LAB_PROPOSTAS_PRODUTOS", "ID_PRODUTOS", "QTDE_CONTRATADA", "VR_TOTAL"];

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var propostas = c.Compartilhado<Dictionary<int, LinhaFb>>(M11Propostas.Legado);
        var itens = c.Compartilhado<Dictionary<(int Proposta, int Item), ItemProposta>>(M12ProdutosAnalisesProposta.Itens);
        var totalItem = c.Compartilhado<Dictionary<(int Proposta, int Item), decimal>>(M12ProdutosAnalisesProposta.TotalItem);
        var itensPorProposta = itens.ToLookup(i => i.Key.Proposta); // uma passada (code-review 4.6: antes era pedidos × itens)
        // propostas (cód./ano, qualquer revisão) que já receberam amostras
        var comAmostras = (await c.Fb.LerAsync("SELECT DISTINCT ID_LAB_PROPOSTAS FROM LAB_HIST_AMOSTRAS", ct))
            .Where(l => propostas.ContainsKey(l.IntObrigatorio("ID_LAB_PROPOSTAS"))) // amostra de proposta excluída: a M-14 trata
            .Select(l => propostas[l.IntObrigatorio("ID_LAB_PROPOSTAS")])
            .Select(p => (M11Propostas.Ano(p.IntObrigatorio("ANO_PROPOSTA")), p.IntObrigatorio("COD_PROPOSTA"))).ToHashSet();
        var novas = await c.Db.LabPropostas.Select(p => new { p.IdLabPropostas, p.IdEntidades, p.IdEntCobranca, p.IdCondicoesPagtos, p.IdMoedas, p.VrTotalProposta })
            .ToDictionaryAsync(p => p.IdLabPropostas, ct);

        var cadeias = propostas.Values
            .Where(p => p.Int("ID_PEDIDOS_VENDAS") is > 0)
            .GroupBy(p => (Ano: M11Propostas.Ano(p.IntObrigatorio("ANO_PROPOSTA")), Cod: p.IntObrigatorio("COD_PROPOSTA")))
            .OrderBy(g => g.Key.Ano).ThenBy(g => g.Key.Cod).ToList();
        var numeros = new HashSet<int>();
        var valores = new List<object?[]>();
        var escolhidas = new List<(int Numero, int Proposta, IEnumerable<int> Outros, (int, int) Chave)>();
        foreach (var cadeia in cadeias)
        {
            var p = cadeia.MaxBy(l => l.IntObrigatorio("REV_PROPOSTA"))!;
            var antigo = p.IntObrigatorio("ID_LAB_PROPOSTAS");
            var onde = $"pedido da proposta {antigo}";
            var numero = p.IntObrigatorio("ID_PEDIDOS_VENDAS");
            var outros = cadeia.Select(l => l.IntObrigatorio("ID_PEDIDOS_VENDAS")).Distinct().Where(n => n != numero).ToList();
            foreach (var n in outros) c.Anomalia($"{onde}: a mesma proposta tem também o pedido {n} em outra revisão — fica só o pedido {numero}.");
            if (!numeros.Add(numero)) throw new InvalidOperationException($"{onde}: o número {numero} já é de outra proposta.");
            var nova = novas[c.Mapa("LAB_PROPOSTAS")[antigo]];
            var autorizacao = p.Data("DT_AUTORIZACAO");
            if (autorizacao is null)
            {
                autorizacao = p.Data("DT_SOLICITACAO");
                c.Anomalia($"{onde}: pedido {numero} sem data de autorização no legado — usada a data da solicitação.");
            }
            var temAmostras = comAmostras.Contains(cadeia.Key);
            valores.Add(
            [
                c.IdEmpresa, numero, autorizacao, nova.IdLabPropostas, cadeia.Key.Cod, cadeia.Key.Ano, nova.IdEntidades,
                nova.IdEntCobranca, nova.IdCondicoesPagtos, nova.IdMoedas, autorizacao, nova.VrTotalProposta,
                temAmostras ? "EM_EXECUCAO" : "ABERTO",
            ]);
            escolhidas.Add((numero, antigo, outros, cadeia.Key));
        }
        var ids = await c.InserirEmLoteAsync("PEDIDOS_VENDA", "ID_PEDIDOS_VENDA", Colunas, valores, ct);

        var mapa = c.Mapa("PEDIDOS_VENDA");
        var daProposta = c.Compartilhado<Dictionary<(int Ano, int Cod), int>>(PedidoDaProposta);
        var valoresItens = new List<object?[]>();
        foreach (var ((numero, proposta, outros, chave), id) in escolhidas.Zip(ids))
        {
            mapa[numero] = id;
            foreach (var n in outros) mapa[n] = id;
            daProposta[chave] = id;
            foreach (var (k, item) in itensPorProposta[proposta].OrderBy(i => i.Key.Item))
                valoresItens.Add([c.IdEmpresa, id, item.Id, item.IdProduto, item.Qtde, totalItem.GetValueOrDefault(k)]);
        }
        await c.InserirEmLoteAsync("PEDIDOS_VENDA_ITENS", "ID_PEDIDOS_VENDA_ITENS", ColunasItem, valoresItens, ct);

        var ultimo = numeros.DefaultIfEmpty(0).Max();
        await c.Db.Database.ExecuteSqlRawAsync("""
            INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO) VALUES ({0}, 'PEDIDO_VENDA', 0, {1})
            ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = GREATEST(ULTIMO_NUMERO, VALUES(ULTIMO_NUMERO))
            """, [c.IdEmpresa, ultimo], ct);

        c.Conferir("números de pedido distintos do legado", propostas.Values.Select(p => p.Int("ID_PEDIDOS_VENDAS")).Where(n => n is > 0).Distinct().Count(), mapa.Count);
        c.Conferir("pedidos (1 por proposta com pedido)", cadeias.Count, await c.Db.PedidosVenda.CountAsync(ct));
        c.Log($"{ids.Count} pedidos, {valoresItens.Count} itens; próximo número depois de {ultimo}.");
        return ids.Count + valoresItens.Count;
    }
}
