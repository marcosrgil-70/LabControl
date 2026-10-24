using System.Text.Json;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-15 — testes, estoque, arquivo e log (M-15-testes-estoque-arquivo.md). Testes por revisão da amostra (origem = teste
/// equivalente da revisão anterior por parâmetro e método, repetidos casados pela ordem); <c>DT_PREVISTA</c> pela
/// <c>TG_TESTES_BI</c>. Movimentos só da amostra física (os das revisões eram entradas duplicadas); localização na raiz,
/// a mais recente quando há duas; saldo das raízes pela <c>TG_MOV_AI</c> (+ zero para quem não tem movimento). O log
/// do legado não tem data nem usuário: entra marcado (<c>"ORIGEM": "LEGADO"</c>), na ordem original (decisão do usuário).
/// </summary>
public sealed class M15TestesEstoqueArquivo : IEtapaMigracao
{
    public string Codigo => "M-15";
    public string Nome => "Testes, estoque, arquivo e log";

    private static readonly string[] ColunasTeste =
    [
        "ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "ID_LAB_PROPOSTAS_PRODANALISES", "ID_LAB_PARAMETROS_ANALISES", "ID_ANALISES_TIPO",
        "ID_ANALISES_METODOS", "ID_IDIOMAS", "ID_PRAZOS", "TX_ANALISES_TIPO", "ID_TESTE_ORIGEM",
    ];
    private static readonly string[] ColunasMov = ["ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "DATA_MOV", "QTDE", "E_S", "JUSTIFICATIVA", "AUTOMATICA", "ID_ENTIDADES_FUNC"];
    private static readonly string[] ColunasLocal =
    [
        "ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "STATUS", "DT_HR_ARQUIVO", "NR_ARMARIO", "NR_PRATELEIRA", "NR_CAIXA",
        "OBSERVACAO", "DT_HR_DESCARTE", "ID_ENTIDADES_FUNC_DESCARTE",
    ];
    private static readonly string[] ColunasLog = ["ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "ACAO", "DADOS", "DT_LOG"];

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var amostras = c.Compartilhado<Dictionary<int, LinhaFb>>(M14Amostras.Legado);
        var testes = await MigrarTestesAsync(c, amostras, ct);
        var movimentos = await MigrarMovimentosAsync(c, amostras, ct);
        var locais = await MigrarLocalizacaoAsync(c, amostras, ct);
        await ConferirSaldoAsync(c, ct);
        var logs = await MigrarLogAsync(c, amostras, ct);
        c.Log($"{testes} testes, {movimentos} movimentos, {locais} localizações, {logs} linhas de log.");
        return testes + movimentos + locais + logs;
    }

    private static async Task<int> MigrarTestesAsync(ContextoMigracao c, Dictionary<int, LinhaFb> amostras, CancellationToken ct)
    {
        var linhas = (await c.Fb.LerAsync("""
            SELECT ID_LAB_HIST_AMOSTRAS_TESTES, ID_LAB_HIST_AMOSTRAS, ID_ANALISES_TIPO, TX_ANALISES_TIPO, ID_ANALISES_METODOS,
                   ID_IDIOMAS, ID_PRAZOS, ID_LAB_PARAMETROS_ANALISES
              FROM LAB_HIST_AMOSTRAS_TESTES ORDER BY ID_LAB_HIST_AMOSTRAS_TESTES
            """, ct)).ToLookup(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"));
        var analises = c.Compartilhado<Dictionary<(int Proposta, int Item, int Parametro, int Metodo), List<int>>>(M12ProdutosAnalisesProposta.Analises);
        var anterior = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Anterior);
        var mapa = c.Mapa("LAB_HIST_AMOSTRAS_TESTES");
        // amostra do legado → testes novos por (parâmetro, método), na ordem (origem da revisão seguinte)
        var porAmostra = new Dictionary<int, Dictionary<(int, int), List<int>>>();
        int semAnalise = 0, repetidos = 0;

        foreach (var camada in amostras.Values.GroupBy(a => a.IntObrigatorio("REVISAO")).OrderBy(g => g.Key))
        {
            var valores = new List<object?[]>();
            var chaves = new List<(int Amostra, int Antigo, (int, int) Par)>();
            foreach (var a in camada)
            {
                var amostra = a.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
                var idAmostra = c.Mapa("LAB_HIST_AMOSTRAS")[amostra];
                var (proposta, item) = (a.IntObrigatorio("ID_LAB_PROPOSTAS"), a.IntObrigatorio("ID_LAB_PROPOSTAS_PRODUTOS"));
                var daAnterior = anterior.TryGetValue(amostra, out var ant) ? porAmostra.GetValueOrDefault(ant) : null;
                var ocorrencia = new Dictionary<(int, int), int>();
                foreach (var l in linhas[amostra])
                {
                    var antigo = l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS_TESTES");
                    var onde = $"teste {antigo} da amostra {amostra}";
                    var par = (Parametro: l.IntObrigatorio("ID_LAB_PARAMETROS_ANALISES"), Metodo: l.IntObrigatorio("ID_ANALISES_METODOS"));
                    var k = ocorrencia[par] = ocorrencia.GetValueOrDefault(par) + 1;
                    if (k == 2) repetidos++;
                    int? analise = analises.TryGetValue((proposta, item, par.Parametro, par.Metodo), out var an) && an.Count == 1 ? an[0] : null;
                    if (analise is null) semAnalise++;
                    int? origem = daAnterior is not null && daAnterior.TryGetValue(par, out var os) && os.Count >= k ? os[k - 1] : null;
                    int Obrigatorio(string tabela, string coluna) => c.Novo(tabela, l.Int(coluna), onde)
                        ?? throw new InvalidOperationException($"{onde}: {coluna} {l.Int(coluna)} não migrado.");
                    valores.Add(
                    [
                        c.IdEmpresa, idAmostra, analise, Obrigatorio("LAB_PARAMETROS_ANALISES", "ID_LAB_PARAMETROS_ANALISES"),
                        Obrigatorio("ANALISES_TIPO", "ID_ANALISES_TIPO"), Obrigatorio("ANALISES_METODOS", "ID_ANALISES_METODOS"),
                        Obrigatorio("IDIOMAS", "ID_IDIOMAS"), Obrigatorio("PRAZOS", "ID_PRAZOS"), l.Texto("TX_ANALISES_TIPO"), origem,
                    ]);
                    chaves.Add((amostra, antigo, par));
                }
            }
            var ids = await c.InserirEmLoteAsync("LAB_HIST_AMOSTRAS_TESTES", "ID_LAB_HIST_AMOSTRAS_TESTES", ColunasTeste, valores, ct);
            foreach (var ((amostra, antigo, par), id) in chaves.Zip(ids))
            {
                mapa[antigo] = id;
                if (!porAmostra.TryGetValue(amostra, out var d)) porAmostra[amostra] = d = [];
                (d.TryGetValue(par, out var lista) ? lista : d[par] = []).Add(id);
            }
        }
        if (repetidos > 0) c.Anomalia($"{repetidos} amostra(s) com o mesmo parâmetro/método duas vezes nos testes — migrados; origem casada pela ordem.");
        if (semAnalise > 0) c.Anomalia($"{semAnalise} teste(s) sem análise correspondente (única) na proposta — gravados sem o vínculo com a análise.");
        c.Conferir("testes do legado", linhas.Sum(g => g.Count()), mapa.Count);
        return mapa.Count;
    }

    private static async Task<int> MigrarMovimentosAsync(ContextoMigracao c, Dictionary<int, LinhaFb> amostras, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT ID_LAB_MOV_AMOSTRAS, ID_LAB_HIST_AMOSTRAS, DATA_MOV, QTDE, E_S, JUSTIFICATIVA, AMOSTRA_COMPLEMENTAR, ID_ENTIDADES_FUNC
              FROM LAB_MOV_AMOSTRAS ORDER BY ID_LAB_MOV_AMOSTRAS
            """, ct);
        var anterior = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Anterior);
        var raiz = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Raiz);
        var valores = new List<object?[]>();
        int descartados = 0, orfaos = 0;
        foreach (var l in linhas)
        {
            var amostra = l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
            var onde = $"movimento {l.IntObrigatorio("ID_LAB_MOV_AMOSTRAS")}";
            var automatica = l.Texto("AMOSTRA_COMPLEMENTAR") == "A";
            // revisão: a entrada automática é duplicada (o legado repetia a entrada a cada revisão); um movimento manual
            // lançado na revisão é real e vai para a amostra física (raiz), como o v2 faz (TG_MOV_BI)
            if (anterior.ContainsKey(amostra) && automatica) { descartados++; continue; }
            if (!raiz.TryGetValue(amostra, out var id)) { orfaos++; c.Anomalia($"{onde}: amostra {amostra} não existe — não migrado."); continue; }
            valores.Add(
            [
                c.IdEmpresa, id, l.DataHora("DATA_MOV") ?? amostras[amostra].DataHora("DT_ENTREGA"), decimal.Round(l.Decimal("QTDE") ?? 0, 4),
                l.Texto("E_S") == "S" ? "S" : "E", c.Cortar<LabMovAmostra>(nameof(LabMovAmostra.Justificativa), l.Texto("JUSTIFICATIVA"), onde),
                automatica,
                c.Novo("ENTIDADES_FUNCIONARIOS", l.Int("ID_ENTIDADES_FUNC"), onde) ?? throw new InvalidOperationException($"{onde}: profissional não migrado."),
            ]);
        }
        if (descartados > 0) c.Anomalia($"{descartados} entrada(s) automática(s) de revisões de amostra descartadas (duplicadas no legado; o estoque é da amostra física).");
        await c.InserirEmLoteAsync("LAB_MOV_AMOSTRAS", "ID_LAB_MOV_AMOSTRAS", ColunasMov, valores, ct);
        c.Conferir("movimentos (legado − entradas duplicadas das revisões − órfãos)", linhas.Count - descartados - orfaos, await c.Db.LabMovAmostras.CountAsync(ct));
        return valores.Count;
    }

    private static async Task<int> MigrarLocalizacaoAsync(ContextoMigracao c, Dictionary<int, LinhaFb> amostras, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT ID_LAB_LOCAL_AMOSTRAS, ID_LAB_HIST_AMOSTRAS, STATUS, DT_HR_ARQUIVO, DT_HR_DESCATE, OBSERVACAO, NR_ARMARIO,
                   NR_PRATELEIRA, NR_CAIXA, ID_FUNCIONARIO_DESCARTE
              FROM LAB_LOCAL_AMOSTRAS ORDER BY ID_LAB_LOCAL_AMOSTRAS
            """, ct);
        var raiz = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Raiz);
        var valores = new List<object?[]>();
        // localização de amostra excluída no legado: não tem onde entrar (anomalia, não erro da etapa)
        foreach (var orfa in linhas.Where(l => !raiz.ContainsKey(l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"))))
            c.Anomalia($"localização {orfa.IntObrigatorio("ID_LAB_LOCAL_AMOSTRAS")}: amostra {orfa.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")} não existe — não migrada.");
        linhas = linhas.Where(l => raiz.ContainsKey(l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"))).ToList();
        foreach (var g in linhas.GroupBy(l => raiz[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")]))
        {
            var ordem = g.OrderByDescending(l => l.DataHora("DT_HR_ARQUIVO")).ThenByDescending(l => l.IntObrigatorio("ID_LAB_LOCAL_AMOSTRAS")).ToList();
            var l = ordem[0];
            var onde = $"localização {l.IntObrigatorio("ID_LAB_LOCAL_AMOSTRAS")}";
            if (ordem.Count > 1)
                c.Anomalia($"{onde}: amostra {l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")} com {ordem.Count} localizações — fica a mais recente.");
            var descartada = l.Int("STATUS") == 1;
            string? Texto(string coluna, string propriedade) => c.Cortar<LabLocalAmostra>(propriedade, l.Texto(coluna), onde);
            valores.Add(
            [
                c.IdEmpresa, g.Key, descartada ? "DESCARTADA" : "ARQUIVADA",
                l.DataHora("DT_HR_ARQUIVO") ?? amostras[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")].DataHora("DT_ENTREGA"),
                Texto("NR_ARMARIO", nameof(LabLocalAmostra.NrArmario)), Texto("NR_PRATELEIRA", nameof(LabLocalAmostra.NrPrateleira)),
                Texto("NR_CAIXA", nameof(LabLocalAmostra.NrCaixa)), Texto("OBSERVACAO", nameof(LabLocalAmostra.Observacao)),
                l.DataHora("DT_HR_DESCATE"), descartada ? c.Novo("ENTIDADES_FUNCIONARIOS", l.Int("ID_FUNCIONARIO_DESCARTE"), onde) : null,
            ]);
        }
        await c.InserirEmLoteAsync("LAB_LOCAL_AMOSTRAS", "ID_LAB_LOCAL_AMOSTRAS", ColunasLocal, valores, ct);
        c.Conferir("amostras com localização", linhas.Select(l => raiz[l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")]).Distinct().Count(), await c.Db.LabLocalAmostras.CountAsync(ct));
        return valores.Count;
    }

    // a TG_MOV_AI já gravou o saldo de quem tem movimento; as raízes sem movimento ficam com zero
    private static async Task ConferirSaldoAsync(ContextoMigracao c, CancellationToken ct)
    {
        await c.Db.Database.ExecuteSqlRawAsync("""
            INSERT INTO LAB_HIST_AMOSTRAS_SALDO (ID_LAB_HIST_AMOSTRAS, ID_EMPRESAS, SALDO_ATUAL)
            SELECT A.ID_LAB_HIST_AMOSTRAS, A.ID_EMPRESAS, 0 FROM LAB_HIST_AMOSTRAS A
             WHERE A.ID_EMPRESAS = {0} AND A.ID_AMOSTRA_RAIZ IS NULL
               AND NOT EXISTS (SELECT 1 FROM LAB_HIST_AMOSTRAS_SALDO S WHERE S.ID_LAB_HIST_AMOSTRAS = A.ID_LAB_HIST_AMOSTRAS)
            """, [c.IdEmpresa], ct);
        var anterior = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Anterior);
        var doLegado = (await c.Fb.LerAsync("SELECT ID_LAB_HIST_AMOSTRAS, SALDO_ATUAL FROM LAB_HIST_AMOSTRAS_SALDO", ct))
            .Where(l => !anterior.ContainsKey(l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")))
            .GroupBy(l => l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")).ToDictionary(g => g.Key, g => decimal.Round(g.First().Decimal("SALDO_ATUAL") ?? 0, 4));
        var novos = await c.Db.LabHistAmostrasSaldo.ToDictionaryAsync(s => s.IdLabHistAmostras, s => s.SaldoAtual, ct);
        var divergentes = 0;
        foreach (var (antigo, saldo) in doLegado)
        {
            if (!c.Mapa("LAB_HIST_AMOSTRAS").TryGetValue(antigo, out var id)) continue;
            var novo = novos.GetValueOrDefault(id);
            if (novo == saldo) continue;
            divergentes++;
            c.Anomalia($"amostra {antigo}: saldo {saldo:0.####} no legado × {novo:0.####} pelos movimentos migrados.");
        }
        c.Conferir("raízes com saldo diferente do legado", 0, divergentes);
        c.Conferir("raízes com saldo", await c.Db.LabHistAmostras.CountAsync(a => a.IdAmostraRaiz == null, ct), novos.Count);
    }

    private static async Task<int> MigrarLogAsync(ContextoMigracao c, Dictionary<int, LinhaFb> amostras, CancellationToken ct)
    {
        // sem chave nem data no legado: a ordem física (RDB$DB_KEY) é a ordem em que foi gravado
        var linhas = await c.Fb.LerAsync("SELECT L.* FROM LAB_HIST_AMOSTRAS_LOG L ORDER BY L.RDB$DB_KEY", ct);
        var valores = new List<object?[]>();
        var sequencia = new Dictionary<int, int>();
        var orfaos = 0;
        foreach (var l in linhas)
        {
            var amostra = l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
            if (!c.Mapa("LAB_HIST_AMOSTRAS").TryGetValue(amostra, out var id)) { orfaos++; continue; }
            var a = amostras[amostra];
            var n = sequencia[amostra] = sequencia.GetValueOrDefault(amostra) + 1;
            // DT_LOG só ordena: data/hora da entrega + n ms (o legado não guardava quando nem quem)
            var quando = (a.Data("DT_ENTREGA")!.Value.ToDateTime(a.Hora("HR_ENTREGA") ?? TimeOnly.MinValue)).AddMilliseconds(n);
            var campos = l.Colunas.Where(kv => kv.Key != "ACAO")
                .ToDictionary(kv => kv.Key, kv => kv.Value is string s ? s.Trim() : kv.Value is byte[] b ? System.Text.Encoding.Latin1.GetString(b) : kv.Value);
            var dados = JsonSerializer.Serialize(new Dictionary<string, object?> { ["ORIGEM"] = "LEGADO", ["SEM_DATA"] = true, ["CAMPOS"] = campos });
            var acao = l.Texto("ACAO") is ("I" or "A" or "E") and var ac ? ac : "A";
            valores.Add([c.IdEmpresa, id, acao, dados, quando]);
        }
        if (orfaos > 0) c.Anomalia($"{orfaos} linha(s) do log do legado de amostras excluídas — não migradas.");
        await c.InserirEmLoteAsync("LAB_HIST_AMOSTRAS_LOG", "ID_LAB_HIST_AMOSTRAS_LOG", ColunasLog, valores, ct);
        c.Conferir("log (legado − amostras excluídas)", linhas.Count - orfaos, await c.Db.LabHistAmostrasLog.CountAsync(ct));
        return valores.Count;
    }
}
