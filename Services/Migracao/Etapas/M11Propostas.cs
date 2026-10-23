using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-11 — propostas e os 5 textos (M-11-propostas.md). INSERT direto (<see cref="ContextoMigracao.InserirEmLoteAsync"/>):
/// o EF não envia ano/código nem os totais, que vêm do legado. Inclusão por revisão (0, 1, 2…) para a origem já ter ID;
/// <c>ULTIMA_REVISAO</c> só na maior. A sequência PROPOSTA por ano continua do maior código (sem os 12 provisórios).
/// </summary>
public sealed class M11Propostas : IEtapaMigracao
{
    public string Codigo => "M-11";
    public string Nome => "Propostas";

    /// <summary>Linhas de LAB_PROPOSTAS do legado por ID (usadas também por M-12…M-14).</summary>
    public const string Legado = "PROPOSTAS_LEGADO";

    private static readonly string[] Colunas =
    [
        "ID_EMPRESAS", "COD_PROPOSTA", "ANO_PROPOSTA", "REV_PROPOSTA", "ID_LAB_PROPOSTAS_ORIGEM", "ULTIMA_REVISAO",
        "ID_ENTIDADES", "ID_ENT_CONTATO", "ID_ENTIDADES_COMERC", "ID_ENTIDADES_FUNC", "ID_ENT_END_LAUDO", "ID_ENT_CONTATO_RESULTADO",
        "ID_ENT_COBRANCA", "ID_ENT_END_COBRANCA", "ID_ENT_CONTATO_NF", "DT_SOLICITACAO", "DT_VALIDADE", "DT_ENVIO_CLIENTE",
        "DT_AUTORIZACAO", "ID_LAB_PROPOSTAS_STATUS", "ID_CONDICOES_PAGTOS", "ID_MOEDAS", "VR_SOMA_VR_ANALIS",
        "VR_SOMA_DESC_ANALIS", "VR_SOMA_VR_TOT_ANALIS", "PORC_DESCONTO", "VR_DESCONTO", "VR_TOTAL_PROPOSTA", "ID_LEGADO",
    ];

    /// <summary>Ano de 2 dígitos do legado → 4 dígitos.</summary>
    public static int Ano(int ano) => ano < 100 ? 2000 + ano : ano;

    /// <summary>
    /// Propostas do legado por revisão (camada 0, 1, 2…) e, para cada uma, a revisão anterior do mesmo número (ID do
    /// legado) — usado pelas etapas que gravam por revisão para apontar a origem.
    /// </summary>
    public static (IOrderedEnumerable<IGrouping<int, LinhaFb>> Camadas, Dictionary<int, int> Anterior) Revisoes(IEnumerable<LinhaFb> propostas)
    {
        var anterior = new Dictionary<int, int>();
        foreach (var cadeia in propostas.GroupBy(l => (Ano(l.IntObrigatorio("ANO_PROPOSTA")), l.IntObrigatorio("COD_PROPOSTA"))))
        {
            var ordem = cadeia.OrderBy(l => l.IntObrigatorio("REV_PROPOSTA")).Select(l => l.IntObrigatorio("ID_LAB_PROPOSTAS")).ToList();
            for (var i = 1; i < ordem.Count; i++) anterior[ordem[i]] = ordem[i - 1];
        }
        return (propostas.GroupBy(l => l.IntObrigatorio("REV_PROPOSTA")).OrderBy(g => g.Key), anterior);
    }

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT ID_LAB_PROPOSTAS, ID_ENTIDADES, COD_PROPOSTA, ANO_PROPOSTA, REV_PROPOSTA, DT_SOLICITACAO, DT_VALIDADE,
                   ID_STATUS_PROPOSTAS, VR_TOTAL_PROPOSTA, PORC_DESCONTO, VR_DESCONTO, ID_ENTIDADES_FUNC, ID_CONDICOES_PAGTOS,
                   DT_AUTORIZACAO, ID_PEDIDOS_VENDAS, ID_MOEDAS, ID_ENTIDADES_COMERC, ID_ENT_END_LAUDO, ID_ENT_END_NF,
                   VR_SOMA_VR_ANALIS, VR_SOMA_DESC_ANALIS, VR_SOMA_VR_TOT_ANALIS, DT_ENVIO_CLIENTE, ID_ENT_CONTATO,
                   ID_ENT_CONTATO_NF, ID_ENT_CONTATO_RESULTADO, ID_ENT_COBRANCA, ID_END_ENT_COBRANCA
              FROM LAB_PROPOSTAS ORDER BY ID_LAB_PROPOSTAS
            """, ct);
        var legado = c.Compartilhado<Dictionary<int, LinhaFb>>(Legado);
        foreach (var l in linhas) legado[l.IntObrigatorio("ID_LAB_PROPOSTAS")] = l;

        // dono de cada endereço/e-mail no legado (endereço de cobrança de outra entidade; contato inexistente)
        var donoEndereco = (await c.Fb.LerAsync("SELECT ID_ENTIDADES_ENDERECOS, ID_ENTIDADES FROM ENTIDADES_ENDERECOS", ct))
            .ToDictionary(l => l.IntObrigatorio("ID_ENTIDADES_ENDERECOS"), l => l.Int("ID_ENTIDADES"));
        var emails = await c.Fb.LerAsync("SELECT ID_ENTIDADES_EMAILS, ID_ENTIDADES FROM ENTIDADES_EMAILS ORDER BY ID_ENTIDADES_EMAILS", ct);
        var primeiroEmail = emails.GroupBy(l => l.Int("ID_ENTIDADES") ?? 0).ToDictionary(g => g.Key, g => g.First().IntObrigatorio("ID_ENTIDADES_EMAILS"));

        int Obrigatorio(int? valor, string onde, string oque) =>
            valor ?? throw new InvalidOperationException($"{onde}: {oque} não migrado — a proposta não pode ser gravada sem ele.");

        var cadeias = linhas.GroupBy(l => (Ano: Ano(l.IntObrigatorio("ANO_PROPOSTA")), Cod: l.IntObrigatorio("COD_PROPOSTA")))
            .ToDictionary(g => g.Key, g => g.Max(l => l.IntObrigatorio("REV_PROPOSTA")));
        var idPorRevisao = new Dictionary<(int Ano, int Cod), SortedList<int, int>>();
        var mapa = c.Mapa("LAB_PROPOSTAS");
        var provisorias = 0;

        foreach (var camada in linhas.GroupBy(l => l.IntObrigatorio("REV_PROPOSTA")).OrderBy(g => g.Key))
        {
            var valores = new List<object?[]>();
            var antigos = new List<(int Antigo, (int, int) Chave, int Rev)>();
            foreach (var l in camada)
            {
                var antigo = l.IntObrigatorio("ID_LAB_PROPOSTAS");
                var onde = $"proposta {antigo}";
                var (ano, cod, rev) = (Ano(l.IntObrigatorio("ANO_PROPOSTA")), l.IntObrigatorio("COD_PROPOSTA"), camada.Key);
                if (cod == antigo) { provisorias++; c.Anomalia($"{onde}: número provisório (código = ID {cod}) nunca substituído no legado — migrada como está."); }

                var cliente = l.Int("ID_ENTIDADES");
                var contatoResultado = l.Int("ID_ENT_CONTATO_RESULTADO");
                if (contatoResultado is { } cr && !c.Mapa("ENTIDADES_EMAILS").ContainsKey(cr) && primeiroEmail.TryGetValue(cliente ?? 0, out var pe))
                {
                    c.Anomalia($"{onde}: contato do resultado {cr} não existe no legado — usado o 1º e-mail do cliente.");
                    contatoResultado = pe;
                }
                var cobranca = l.Int("ID_ENT_COBRANCA") ?? cliente;
                var endCobranca = l.Int("ID_END_ENT_COBRANCA") ?? l.Int("ID_ENT_END_NF");
                if (endCobranca is { } ec && donoEndereco.GetValueOrDefault(ec) != cobranca)
                {
                    c.Anomalia($"{onde}: endereço de cobrança {ec} é de outra entidade — gravado sem endereço de cobrança.");
                    endCobranca = null;
                }
                var endLaudo = l.Int("ID_ENT_END_LAUDO");
                if (endLaudo is { } el && donoEndereco.GetValueOrDefault(el) != cliente)
                    c.Anomalia($"{onde}: endereço do laudo {el} é de outra entidade no legado — mantido.");
                var solicitacao = l.Data("DT_SOLICITACAO") ?? throw new InvalidOperationException($"{onde}: sem data de solicitação.");
                var validade = l.Data("DT_VALIDADE");
                if (validade < solicitacao)
                {
                    c.Anomalia($"{onde}: validade {validade:dd/MM/yyyy} anterior à solicitação — gravada igual à solicitação.");
                    validade = solicitacao;
                }
                int? Opcional(string tabela, string coluna) => l.Int(coluna) is { } v and not 0 ? c.Novo(tabela, v, $"{onde} ({coluna})") : null;

                valores.Add(
                [
                    c.IdEmpresa, cod, ano, rev, idPorRevisao.TryGetValue((ano, cod), out var revs) ? revs.Values[^1] : null, rev == cadeias[(ano, cod)],
                    Obrigatorio(c.Novo("ENTIDADES", cliente, onde), onde, "cliente"), Opcional("ENTIDADES_EMAILS", "ID_ENT_CONTATO"),
                    Opcional("ENTIDADES", "ID_ENTIDADES_COMERC"), Obrigatorio(c.Novo("ENTIDADES", l.Int("ID_ENTIDADES_FUNC"), onde), onde, "profissional"),
                    Obrigatorio(c.Novo("ENTIDADES_ENDERECOS", endLaudo, onde), onde, "endereço do laudo"),
                    Obrigatorio(c.Novo("ENTIDADES_EMAILS", contatoResultado, onde), onde, "contato do resultado"),
                    Obrigatorio(c.Novo("ENTIDADES", cobranca, onde), onde, "entidade de cobrança"),
                    endCobranca is null ? null : c.Novo("ENTIDADES_ENDERECOS", endCobranca, onde), Opcional("ENTIDADES_EMAILS", "ID_ENT_CONTATO_NF"),
                    solicitacao, validade, l.Data("DT_ENVIO_CLIENTE"), l.Data("DT_AUTORIZACAO"),
                    Obrigatorio(c.Novo("LAB_PROPOSTAS_STATUS", l.Int("ID_STATUS_PROPOSTAS"), onde), onde, "status"),
                    Obrigatorio(c.Novo("CONDICOES_PAGTOS", l.Int("ID_CONDICOES_PAGTOS"), onde), onde, "condição de pagamento"),
                    Opcional("MOEDAS", "ID_MOEDAS"),
                    Valor(l, "VR_SOMA_VR_ANALIS"), Valor(l, "VR_SOMA_DESC_ANALIS"), Valor(l, "VR_SOMA_VR_TOT_ANALIS"),
                    decimal.Round(l.Decimal("PORC_DESCONTO") ?? 0, 4), Valor(l, "VR_DESCONTO"), Valor(l, "VR_TOTAL_PROPOSTA"), antigo,
                ]);
                antigos.Add((antigo, (ano, cod), rev));
            }
            var ids = await c.InserirEmLoteAsync("LAB_PROPOSTAS", "ID_LAB_PROPOSTAS", Colunas, valores, ct);
            foreach (var ((antigo, chave, rev), id) in antigos.Zip(ids))
            {
                mapa[antigo] = id;
                if (!idPorRevisao.TryGetValue(chave, out var revs)) idPorRevisao[chave] = revs = [];
                revs[rev] = id;
            }
        }

        var textos = await MigrarTextosAsync(c, ct);

        // próximo número por ano continua do legado (os provisórios, código = ID, ficam fora)
        foreach (var (ano, ultimo) in linhas.Where(l => l.IntObrigatorio("COD_PROPOSTA") != l.IntObrigatorio("ID_LAB_PROPOSTAS"))
                     .GroupBy(l => Ano(l.IntObrigatorio("ANO_PROPOSTA"))).Select(g => (g.Key, g.Max(l => l.IntObrigatorio("COD_PROPOSTA")))))
            await c.Db.Database.ExecuteSqlRawAsync("""
                INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO) VALUES ({0}, 'PROPOSTA', {1}, {2})
                ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = GREATEST(ULTIMO_NUMERO, VALUES(ULTIMO_NUMERO))
                """, [c.IdEmpresa, ano, ultimo], ct);

        c.Conferir("propostas do legado", linhas.Count, mapa.Count);
        c.Conferir("Σ VR_TOTAL_PROPOSTA por ano",
            string.Join("; ", linhas.GroupBy(l => Ano(l.IntObrigatorio("ANO_PROPOSTA"))).OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.Sum(l => Valor(l, "VR_TOTAL_PROPOSTA")):0.00}")),
            string.Join("; ", (await c.Db.LabPropostas.GroupBy(p => p.AnoProposta).Select(g => new { g.Key, Total = g.Sum(p => p.VrTotalProposta) }).ToListAsync(ct))
                .OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.Total:0.00}")));
        c.Conferir("propostas com exatamente uma última revisão", 0,
            await c.Db.LabPropostas.GroupBy(p => new { p.AnoProposta, p.CodProposta }).Select(g => g.Sum(p => p.UltimaRevisao ? 1 : 0)).CountAsync(n => n != 1, ct));
        c.Log($"{provisorias} proposta(s) com número provisório; {textos} com textos.");
        return mapa.Count;
    }

    private static decimal Valor(LinhaFb l, string coluna) => decimal.Round(l.Decimal(coluna) ?? 0, 2);

    // as 5 tabelas de RTF do legado → LAB_PROPOSTAS_TEXTOS (HTML; o RTF original fica quando a conversão falha)
    private static async Task<int> MigrarTextosAsync(ContextoMigracao c, CancellationToken ct)
    {
        var fontes = new (string Tabela, string Coluna, Action<LabPropostaTextos, string> Gravar)[]
        {
            ("LAB_PROPOSTAS_ITENS_SOL", "TX_ITENS_SOLICITADOS", (t, v) => t.TxItensSolicitados = v),
            ("LAB_PROPOSTAS_ITENS_ADQ", "TX_ITENS_ADQUIRIDOS", (t, v) => t.TxItensAdquiridos = v),
            ("LAB_PROPOSTAS_OBSERVACAO", "TX_OBSERVACAO", (t, v) => t.TxObservacao = v),
            ("LAB_PROPOSTAS_CONTRATOS", "TX_DESCRICAO", (t, v) => t.TxContrato = v),
            ("LAB_PROPOSTAS_AUTORIZACAO", "TX_AUTORIZACAO", (t, v) => t.TxAutorizacao = v),
        };
        var textos = new Dictionary<int, LabPropostaTextos>();
        foreach (var (tabela, coluna, gravar) in fontes)
            foreach (var l in await c.Fb.LerAsync($"SELECT ID_LAB_PROPOSTAS, {coluna} FROM {tabela}", ct))
            {
                var antigo = l.IntObrigatorio("ID_LAB_PROPOSTAS");
                if (c.Novo("LAB_PROPOSTAS", antigo, $"{tabela} {antigo}") is not { } id) continue;
                if (Html(c, l.Texto(coluna), $"proposta {antigo} ({tabela})") is not { } html) continue;
                if (!textos.TryGetValue(id, out var t)) textos[id] = t = new LabPropostaTextos { IdLabPropostas = id, IdEmpresas = c.IdEmpresa };
                gravar(t, html);
            }
        c.Db.AddRange(textos.Values);
        await c.Db.SaveChangesAsync(ct);
        return textos.Count;
    }

    /// <summary>RTF do legado → HTML; RTF inválido fica como está, com anomalia.</summary>
    public static string? Html(ContextoMigracao c, string? rtf, string onde)
    {
        try { return Conversoes.RtfParaHtml(rtf); }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            c.Anomalia($"{onde}: RTF não convertido ({ex.Message}) — guardado o RTF original.");
            return rtf;
        }
    }
}
