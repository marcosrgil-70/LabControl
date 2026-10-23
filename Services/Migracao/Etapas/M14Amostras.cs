using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-14 — amostras (M-14-amostras.md). Ano/código/revisão do legado preservados (INSERT direto; a trigger calcula
/// <c>NR_AMOSTRA</c> com os códigos dos tipos = IDs antigos) e <b>todos</b> os números conferidos com a
/// <c>VW_LAB_HIST_AMOSTRA</c> do legado — divergência para a etapa. Inclusão por revisão: raiz = revisão 0, origem =
/// anterior, <c>ULTIMA_REVISAO</c> na maior; revisões sem coletor/data da coleta copiam da raiz. Sem pedido no legado
/// (0) → o pedido da proposta, como o recebimento do v2 faz.
/// </summary>
public sealed class M14Amostras : IEtapaMigracao
{
    public string Codigo => "M-14";
    public string Nome => "Amostras";

    /// <summary>Linhas de LAB_HIST_AMOSTRAS do legado por ID (usadas pela M-15).</summary>
    public const string Legado = "AMOSTRAS_LEGADO";
    /// <summary><c>amostra do legado → raiz nova</c> (movimentos, localização e saldo são da amostra física).</summary>
    public const string Raiz = "AMOSTRA_RAIZ";
    /// <summary><c>amostra do legado → revisão anterior do legado</c> (origem dos testes).</summary>
    public const string Anterior = "AMOSTRA_ANTERIOR";

    private static readonly string[] Colunas =
    [
        "ID_EMPRESAS", "ANO_AMOSTRA", "COD_AMOSTRA", "REVISAO", "ID_AMOSTRAS_TIPO", "ID_ANALISES_TIPO", "ID_AMOSTRA_ORIGEM",
        "ID_AMOSTRA_RAIZ", "ULTIMA_REVISAO", "TIPO_DOCUMENTO", "ID_ENTIDADES", "NOME_CONTATO", "ID_LAB_PROPOSTAS",
        "ID_LAB_PROPOSTAS_PRODUTOS", "ID_PEDIDOS_VENDA", "ID_PRODUTOS", "DT_ENTREGA", "HR_ENTREGA", "LOCAL_RECEBIMENTO",
        "ID_EMBALAGENS_TIPOS", "QTDE_EMBALAGENS_ENTREGUE", "NR_LOTE", "FABRICACAO_DIA", "FABRICACAO_MES", "FABRICACAO_ANO",
        "VALIDADE_DIA", "VALIDADE_MES", "VALIDADE_ANO", "DT_FABRICACAO", "DT_VALIDADE", "NOTA_ROTULO",
        "DT_ENTREGA_VERIFICACAO", "HR_ENTREGA_VERIFICACAO", "ESPECIE_AMOSTRA", "ASPECTO_AMOSTRA", "COR",
        "OUTRAS_CARACTERISTICAS", "QTDE_AMOSTRA_VERIFICACAO", "TEMPERATURA_VERIFICACAO", "OBSERVACAO_VERIFICACAO",
        "ACOMPANHA_FICHA_TECNICA", "ACOMPANHA_PADRAO_ANALITICO", "ACOMPANHA_CA_CLIENTE", "ENVIAR_OUTRO_LABORATORIO",
        "QTDE_ENVIO_OUTRO_LABORATORIO", "ID_AMOSTRAS_STATUS", "ID_ENTIDADES_FUNC_RESP", "ID_ENTIDADES_FUNC_DIG",
        "NOME_COLETOR", "DT_HR_COLETA", "TX_OBSERVACAO", "ID_LEGADO",
    ];

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT ID_LAB_HIST_AMOSTRAS, ID_AMOSTRAS_TIPO, COD_AMOSTRA, ID_ANALISES_TIPO, ANO_AMOSTRA, TIPO_DOCUMENTO, ID_ENTIDADES,
                   NOME_CONTATO, ID_LAB_PROPOSTAS, ID_LAB_PROPOSTAS_PRODUTOS, ID_LAB_PEDIDOS_VENDAS, DT_ENTREGA, HR_ENTREGA,
                   LOCAL_RECEBIMENTO, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGENS_ENTREGUE, ID_PRODUTOS, NR_LOTE, DT_FABRICACAO,
                   DT_VALIDADE, NOTA_ROTULO, DT_ENTREGA_VERIFICACAO, HR_ENTREGA_VERIFICACAO, ESPECIE_AMOSTRA, ASPECTO_AMOSTRA,
                   COR, OUTRAS_CARACTERISTICAS, QTDE_AMOSTRA_VERIFICACAO, TEMPERATURA_VERFIFICACAO, OBSERVACAO_VERIFICACAO,
                   ACOMPANHA_FICHA_TECNICA, ACOMPANHA_PADRA_ANALITICO, ACOMPANHA_CA_CLIENTE, ENVIAR_OUTRO_LABORATORIO,
                   QTDE_ENVIO_OUTRO_LABORATORIO, ID_AMOSTRAS_STATUS, ID_ENTIDADES_FUNC_RESP, ID_ENTIDADES_FUNC_DIG,
                   VALIDADE_DIA, VALIDADE_MES, VALIDADE_ANO, FABRICACAO_DIA, FABRICACAO_MES, FABRICACAO_ANO, REVISAO,
                   COLETOR, DATAHORACOLETA
              FROM LAB_HIST_AMOSTRAS ORDER BY ID_LAB_HIST_AMOSTRAS
            """, ct);
        var observacoes = (await c.Fb.LerAsync("SELECT ID_LAB_HIST_AMOSTRAS, TX_OBSERVACAO FROM LAB_HIST_AMOSTRAS_OBSERVACAO", ct))
            .ToDictionary(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"), l => l.Texto("TX_OBSERVACAO"));
        var propostas = c.Compartilhado<Dictionary<int, LinhaFb>>(M11Propostas.Legado);
        var itens = c.Compartilhado<Dictionary<(int Proposta, int Item), ItemProposta>>(M12ProdutosAnalisesProposta.Itens);
        var pedidoDaProposta = c.Compartilhado<Dictionary<(int Ano, int Cod), int>>(M13PedidosVenda.PedidoDaProposta);
        var legado = c.Compartilhado<Dictionary<int, LinhaFb>>(Legado);
        foreach (var l in linhas) legado[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")] = l;

        // ano com 4 dígitos: 0 → ano da entrega (1 caso); diferente do ano da entrega fica (é o que foi impresso)
        var anos = new Dictionary<int, int>();
        foreach (var l in linhas)
        {
            var antigo = l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
            var entrega = l.Data("DT_ENTREGA") ?? throw new InvalidOperationException($"amostra {antigo}: sem data de entrega.");
            var ano = l.IntObrigatorio("ANO_AMOSTRA") is 0 ? entrega.Year : M11Propostas.Ano(l.IntObrigatorio("ANO_AMOSTRA"));
            if (l.IntObrigatorio("ANO_AMOSTRA") == 0) c.Anomalia($"amostra {antigo}: ano 0 no legado — gravada no ano da entrega ({ano}).");
            else if (ano != entrega.Year) c.Anomalia($"amostra {antigo}: ano {ano} diferente do ano da entrega {entrega:dd/MM/yyyy} — mantido (é o número impresso).");
            anos[antigo] = ano;
        }

        // cadeias de revisão por (ano, código): raiz = menor revisão (sempre a 0 no legado real)
        var anterior = c.Compartilhado<Dictionary<int, int>>(Anterior);
        var raizLegado = new Dictionary<int, int>();
        var ultima = new HashSet<int>();
        foreach (var cadeia in linhas.GroupBy(l => (anos[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")], l.IntObrigatorio("COD_AMOSTRA"))))
        {
            var ordem = cadeia.OrderBy(l => l.IntObrigatorio("REVISAO")).ThenBy(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")).ToList();
            if (ordem[0].IntObrigatorio("REVISAO") != 0) c.Anomalia($"amostra {ordem[0].IntObrigatorio("ID_LAB_HIST_AMOSTRAS")}: cadeia sem revisão 0 — a menor revisão é a raiz.");
            if (ordem.Select(l => l.IntObrigatorio("REVISAO")).Distinct().Count() != ordem.Count)
                throw new InvalidOperationException($"amostra {ordem[0].IntObrigatorio("ID_LAB_HIST_AMOSTRAS")}: revisão repetida no mesmo número.");
            var raiz = ordem[0].IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
            for (var i = 0; i < ordem.Count; i++)
            {
                var id = ordem[i].IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
                raizLegado[id] = raiz;
                if (i > 0) anterior[id] = ordem[i - 1].IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
            }
            ultima.Add(ordem[^1].IntObrigatorio("ID_LAB_HIST_AMOSTRAS"));
        }

        var mapa = c.Mapa("LAB_HIST_AMOSTRAS");
        var raizNova = c.Compartilhado<Dictionary<int, int>>(Raiz);
        int ajustadas = 0, coletaCopiada = 0;
        foreach (var camada in linhas.GroupBy(l => l.IntObrigatorio("REVISAO")).OrderBy(g => g.Key))
        {
            var valores = new List<object?[]>();
            var antigos = new List<int>();
            foreach (var l in camada)
            {
                var antigo = l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
                var onde = $"amostra {antigo}";
                var ehRaiz = raizLegado[antigo] == antigo;
                var raiz = legado[raizLegado[antigo]];
                var proposta = l.IntObrigatorio("ID_LAB_PROPOSTAS");
                var item = itens.TryGetValue((proposta, l.IntObrigatorio("ID_LAB_PROPOSTAS_PRODUTOS")), out var it) ? it
                    : throw new InvalidOperationException($"{onde}: item {l.Int("ID_LAB_PROPOSTAS_PRODUTOS")} da proposta {proposta} não migrado.");
                var p = propostas[proposta];
                int? pedido = l.Int("ID_LAB_PEDIDOS_VENDAS") is > 0 and var n
                    ? c.Novo("PEDIDOS_VENDA", n, onde)
                    : pedidoDaProposta.TryGetValue((M11Propostas.Ano(p.IntObrigatorio("ANO_PROPOSTA")), p.IntObrigatorio("COD_PROPOSTA")), out var pp) ? pp : null;
                var fab = Parcial(c, onde, "fabricação", l.Int("FABRICACAO_DIA"), l.Int("FABRICACAO_MES"), l.Int("FABRICACAO_ANO"), ref ajustadas);
                var val = Parcial(c, onde, "validade", l.Int("VALIDADE_DIA"), l.Int("VALIDADE_MES"), l.Int("VALIDADE_ANO"), ref ajustadas);
                var (coletor, coleta) = (l.Texto("COLETOR"), l.DataHora("DATAHORACOLETA"));
                if (!ehRaiz && coletor is null && coleta is null && (raiz.Texto("COLETOR") ?? (object?)raiz.DataHora("DATAHORACOLETA")) is not null)
                {
                    (coletor, coleta) = (raiz.Texto("COLETOR"), raiz.DataHora("DATAHORACOLETA"));
                    coletaCopiada++;
                }
                string? Texto(string coluna, string propriedade) => c.Cortar<LabHistAmostra>(propriedade, l.Texto(coluna), onde);
                int Obrigatorio(string tabela, string coluna) => c.Novo(tabela, l.Int(coluna), onde)
                    ?? throw new InvalidOperationException($"{onde}: {coluna} {l.Int(coluna)} não migrado.");

                valores.Add(
                [
                    c.IdEmpresa, anos[antigo], l.IntObrigatorio("COD_AMOSTRA"), l.IntObrigatorio("REVISAO"),
                    Obrigatorio("AMOSTRAS_TIPO", "ID_AMOSTRAS_TIPO"), Obrigatorio("ANALISES_TIPO", "ID_ANALISES_TIPO"),
                    anterior.TryGetValue(antigo, out var ant) ? mapa[ant] : null, ehRaiz ? null : raizNova[antigo] = mapa[raizLegado[antigo]],
                    ultima.Contains(antigo), M12ProdutosAnalisesProposta.TipoDocumento(l.Texto("TIPO_DOCUMENTO")),
                    Obrigatorio("ENTIDADES", "ID_ENTIDADES"), Texto("NOME_CONTATO", nameof(LabHistAmostra.NomeContato)),
                    c.Novo("LAB_PROPOSTAS", proposta, onde), item.Id, pedido, Obrigatorio("PRODUTOS", "ID_PRODUTOS"),
                    l.Data("DT_ENTREGA"), l.Hora("HR_ENTREGA") ?? TimeOnly.MinValue,
                    Texto("LOCAL_RECEBIMENTO", nameof(LabHistAmostra.LocalRecebimento)) ?? "",
                    Obrigatorio("EMBALAGENS_TIPOS", "ID_EMBALAGENS_TIPOS"), Qtde(l, "QTDE_EMBALAGENS_ENTREGUE") ?? 0m,
                    Texto("NR_LOTE", nameof(LabHistAmostra.NrLote)), fab.Dia, fab.Mes, fab.Ano, val.Dia, val.Mes, val.Ano,
                    l.Data("DT_FABRICACAO"), l.Data("DT_VALIDADE"), Texto("NOTA_ROTULO", nameof(LabHistAmostra.NotaRotulo)),
                    l.Data("DT_ENTREGA_VERIFICACAO"), l.Hora("HR_ENTREGA_VERIFICACAO"),
                    Texto("ESPECIE_AMOSTRA", nameof(LabHistAmostra.EspecieAmostra)), Texto("ASPECTO_AMOSTRA", nameof(LabHistAmostra.AspectoAmostra)),
                    Texto("COR", nameof(LabHistAmostra.Cor)), Texto("OUTRAS_CARACTERISTICAS", nameof(LabHistAmostra.OutrasCaracteristicas)),
                    Qtde(l, "QTDE_AMOSTRA_VERIFICACAO"), l.Decimal("TEMPERATURA_VERFIFICACAO") is { } t ? decimal.Round(t, 2) : null,
                    Texto("OBSERVACAO_VERIFICACAO", nameof(LabHistAmostra.ObservacaoVerificacao)),
                    l.Flag("ACOMPANHA_FICHA_TECNICA"), l.Flag("ACOMPANHA_PADRA_ANALITICO"), l.Flag("ACOMPANHA_CA_CLIENTE"),
                    l.Flag("ENVIAR_OUTRO_LABORATORIO"), Qtde(l, "QTDE_ENVIO_OUTRO_LABORATORIO") ?? 0m,
                    Obrigatorio("AMOSTRAS_STATUS", "ID_AMOSTRAS_STATUS"),
                    Obrigatorio("ENTIDADES_FUNCIONARIOS", "ID_ENTIDADES_FUNC_RESP"), Obrigatorio("ENTIDADES_FUNCIONARIOS", "ID_ENTIDADES_FUNC_DIG"),
                    c.Cortar<LabHistAmostra>(nameof(LabHistAmostra.NomeColetor), coletor, onde), coleta,
                    observacoes.TryGetValue(antigo, out var obs) ? M11Propostas.Html(c, obs, $"{onde} (observação)") : null, antigo,
                ]);
                antigos.Add(antigo);
            }
            var ids = await c.InserirEmLoteAsync("LAB_HIST_AMOSTRAS", "ID_LAB_HIST_AMOSTRAS", Colunas, valores, ct);
            foreach (var (antigo, id) in antigos.Zip(ids))
            {
                mapa[antigo] = id;
                if (raizLegado[antigo] == antigo) raizNova[antigo] = id;
            }
        }

        foreach (var (ano, ultimo) in linhas.GroupBy(l => anos[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")]).Select(g => (g.Key, g.Max(l => l.IntObrigatorio("COD_AMOSTRA")))))
            await c.Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO) VALUES ({0}, 'AMOSTRA', {1}, {2})
                ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = GREATEST(ULTIMO_NUMERO, VALUES(ULTIMO_NUMERO))
                """, [c.IdEmpresa, ano, ultimo], ct);

        await ConferirNumerosAsync(c, linhas.Where(l => l.IntObrigatorio("ANO_AMOSTRA") == 0).Select(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")).ToHashSet(), ct);
        c.Conferir("amostras do legado", linhas.Count, mapa.Count);
        c.Conferir("amostras por ano",
            string.Join("; ", anos.Values.GroupBy(a => a).OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.Count()}")),
            string.Join("; ", (await c.Db.LabHistAmostras.GroupBy(a => a.AnoAmostra).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct))
                .OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.N}")));
        c.Conferir("números com exatamente uma última revisão", 0,
            await c.Db.LabHistAmostras.GroupBy(a => new { a.AnoAmostra, a.CodAmostra }).Select(g => g.Sum(a => a.UltimaRevisao ? 1 : 0)).CountAsync(n => n != 1, ct));
        c.Log($"{mapa.Count} amostras; {ajustadas} data(s) parcial(is) ajustada(s); coletor/coleta copiados da raiz em {coletaCopiada} revisão(ões).");
        return mapa.Count;
    }

    private static decimal? Qtde(LinhaFb l, string coluna) => l.Decimal(coluna) is { } q ? decimal.Round(q, 4) : null;

    // validade/fabricação parcial no formato que a trigger aceita; o que cair vira anomalia
    private static (int? Dia, int? Mes, int? Ano) Parcial(ContextoMigracao c, string onde, string qual, int? dia, int? mes, int? ano, ref int ajustadas)
    {
        var r = Conversoes.DataParcial(dia, mes, ano);
        if (r != (dia is > 0 ? dia : null, mes is > 0 ? mes : null, ano is > 0 ? ano : null))
        {
            ajustadas++;
            c.Anomalia($"{onde}: {qual} {dia}/{mes}/{ano} inconsistente no legado — gravada como {FormatarParcial(r)}.");
        }
        return r;
    }

    private static string FormatarParcial((int? Dia, int? Mes, int? Ano) d) =>
        d switch { (null, null, null) => "vazia", (null, null, _) => $"{d.Ano}", (null, _, _) => $"{d.Mes:00}/{d.Ano}", _ => $"{d.Dia:00}/{d.Mes:00}/{d.Ano}" };

    // todos os números iguais ao impresso pelo legado; divergência para a etapa (é a identificação da amostra física)
    private static async Task ConferirNumerosAsync(ContextoMigracao c, HashSet<int> anoZero, CancellationToken ct)
    {
        var doLegado = (await c.Fb.LerAsync("SELECT DISTINCT ID_LAB_HIST_AMOSTRAS, NR_AMOSTRA FROM VW_LAB_HIST_AMOSTRA", ct))
            .GroupBy(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")).ToDictionary(g => g.Key, g => g.Select(l => l.Texto("NR_AMOSTRA")).ToList());
        var novos = await c.Db.LabHistAmostras.Select(a => new { a.IdLegado, a.NrAmostra }).ToListAsync(ct);
        var divergentes = new List<string>();
        var semVisao = 0;
        foreach (var a in novos)
        {
            if (!doLegado.TryGetValue(a.IdLegado!.Value, out var nrs)) { semVisao++; c.Anomalia($"amostra {a.IdLegado}: fora da VW_LAB_HIST_AMOSTRA do legado — número {a.NrAmostra} não conferido."); continue; }
            if (anoZero.Contains(a.IdLegado.Value))
            {
                c.Anomalia($"amostra {a.IdLegado}: impressa como {string.Join("/", nrs)} (ano 0) — no v2 {a.NrAmostra}, ano da entrega (doc 10 §8).");
                continue;
            }
            if (!nrs.Contains(a.NrAmostra)) divergentes.Add($"{a.IdLegado}: legado {string.Join("/", nrs)} × v2 {a.NrAmostra}");
        }
        c.Conferir("números de amostra iguais ao legado", 0, divergentes.Count);
        if (divergentes.Count > 0)
            throw new InvalidOperationException($"{divergentes.Count} número(s) de amostra diferentes do legado, ex.: {string.Join("; ", divergentes.Take(5))}.");
        c.Log($"{novos.Count - semVisao} números de amostra conferidos com o legado ({semVisao} fora da visão do legado).");
    }
}
