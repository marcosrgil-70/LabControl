using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-16 — resultados (M-16-resultados.md). Revisão renumerada 0..n por amostra física (raiz nova), na ordem revisão da
/// amostra → revisão do legado → ID; <c>ULTIMA_REVISAO</c> na maior, origem = anterior; gravados camada a camada (a FK
/// de origem, do resultado e do parâmetro, precisa do anterior). Os textos formatados saem da <c>TG_RESPAR_BI</c>, que
/// calcula também com <c>@LC_MIGRACAO = 1</c>. Erros do legado corrigidos (decisões do usuário, 5.1): data do
/// resultado antes do início → início = resultado; teste de outra amostra → o equivalente da amostra do resultado;
/// o mesmo teste duas vezes no resultado → outro teste equivalente livre ou, sem ele, uma linha só.
/// </summary>
public sealed class M16Resultados : IEtapaMigracao
{
    public string Codigo => "M-16";
    public string Nome => "Resultados";

    /// <summary>Parâmetro do legado descartado (repetido no resultado) → o que ficou no lugar dele (usado pela M-17).</summary>
    public const string Fundidos = "RESULTADOS_PARAM_FUNDIDOS";

    private static readonly string[] ColunasResultado =
    [
        "ID_EMPRESAS", "ID_LAB_HIST_AMOSTRAS", "REVISAO", "ID_RESULTADO_ORIGEM", "ULTIMA_REVISAO", "ID_ENTIDADES_FUNC_DIG", "ID_LEGADO",
    ];
    private static readonly string[] ColunasParam =
    [
        "ID_EMPRESAS", "ID_LAB_RESULTADOS_ANALISES", "ID_LAB_HIST_AMOSTRAS_TESTES", "DT_INICIAL", "DT_RESULTADO",
        "ID_ENTIDADES_FUNC_ANALISE", "ID_ENTIDADES_FUNC_DIG", "QT_AMOSTRA", "ID_UNIDADES_AMOSTRA", "TIPO_RESULTADO",
        "SIMBOLO_GRANDEZA", "VR_RESULTADO", "VR_EXP_RESULTADO", "ID_UNIDADES", "ID_LAB_TIPOS_RESULTADOS", "TIPO_RESULTADO_ESPECIF",
        "SIMBOLO_GR_ESPECIF", "VR_RESULTADO_ESPECIF", "VR_EXP_ESPECIF", "ID_UNIDADES_ESPECIF", "ID_LAB_TIPO_RESULTADOS_ESP",
        "VR_SATISFEITO", "ID_PARAM_ORIGEM", "ID_LEGADO",
    ];

    private sealed record Teste(int Id, int Amostra, int Parametro, int Metodo);

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var resultados = await c.Fb.LerAsync(
            "SELECT ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRAS, ID_ENTIDADES_FUNC_DIG, REVISAO FROM LAB_RESULTADOS_ANALISES ORDER BY ID_LAB_RESULTADOS_ANALISES", ct);
        var parametros = (await c.Fb.LerAsync("""
            SELECT ID_LAB_RESULTADOS_PARAM, ID_LAB_RESULTADOS_ANALISES, ID_LAB_HIST_AMOSTRA_TESTES, DT_INICIAL, DT_RESULTADO,
                   ID_ENTIDADES_FUNC_ANALISE, ID_ENTIDADES_FUNC_DIG, QT_AMOSTRA, ID_UNIDADES_AMOSTRA, TIPO_RESULTADO, SIMBOLO_GRANDEZA,
                   VR_RESULTADO, VR_EXP_RESULTADO, ID_UNIDADES, ID_LAB_TIPOS_RESULTADOS, TIPO_RESULTADO_ESPECIF, SIMBOLO_GR_ESPECIF,
                   VR_RESULTADO_ESPECIF, VR_EXP_ESPECIF, ID_UNIDADES_ESPECIF, ID_LAB_TIPO_RESULTADOS_ESP, VR_SATISFEITO
              FROM LAB_RESULTADOS_PARAM ORDER BY ID_LAB_RESULTADOS_PARAM
            """, ct)).ToLookup(l => l.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES"));
        var testes = (await c.Fb.LerAsync(
            "SELECT ID_LAB_HIST_AMOSTRAS_TESTES, ID_LAB_HIST_AMOSTRAS, ID_LAB_PARAMETROS_ANALISES, ID_ANALISES_METODOS FROM LAB_HIST_AMOSTRAS_TESTES", ct))
            .Select(l => new Teste(l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS_TESTES"), l.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"),
                l.IntObrigatorio("ID_LAB_PARAMETROS_ANALISES"), l.IntObrigatorio("ID_ANALISES_METODOS")))
            .ToDictionary(t => t.Id);
        var testesDaAmostra = testes.Values.ToLookup(t => t.Amostra);
        // parâmetro impresso num boletim: entre repetidos, é ele que fica
        var noBoletim = (await c.Fb.LerAsync("SELECT DISTINCT ID_LAB_RESULTADOS_PARAM FROM LAB_BOLETIM_PARAMETROS", ct))
            .Select(l => l.IntObrigatorio("ID_LAB_RESULTADOS_PARAM")).ToHashSet();
        var amostras = c.Compartilhado<Dictionary<int, LinhaFb>>(M14Amostras.Legado);
        var raiz = c.Compartilhado<Dictionary<int, int>>(M14Amostras.Raiz);
        var mapaRes = c.Mapa("LAB_RESULTADOS_ANALISES");
        var mapaPar = c.Mapa("LAB_RESULTADOS_PARAM");
        var fundidos = c.Compartilhado<Dictionary<int, int>>(Fundidos);

        // cadeias de revisão por amostra física
        var cadeias = new List<List<LinhaFb>>();
        var semAmostra = new List<int>(); // resultados de amostra que não migrou: anomalia, fora das contagens
        foreach (var g in resultados.GroupBy(r => raiz.TryGetValue(r.IntObrigatorio("ID_LAB_HIST_AMOSTRAS"), out var rz) ? rz : 0))
        {
            if (g.Key == 0)
            {
                foreach (var r in g)
                {
                    semAmostra.Add(r.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES"));
                    c.Anomalia($"resultado {r.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")}: amostra {r.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")} não migrada — não migrado.");
                }
                continue;
            }
            cadeias.Add(g.OrderBy(r => amostras[r.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")].IntObrigatorio("REVISAO"))
                .ThenBy(r => r.Int("REVISAO") ?? 0).ThenBy(r => r.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")).ToList());
        }
        var maisDeUm = cadeias.Where(cd => cd.Count > 1).ToList();
        if (maisDeUm.Count > 0)
            c.Anomalia($"{maisDeUm.Count} amostra(s) física(s) com mais de um resultado no legado — todos migrados como revisões: " +
                string.Join(", ", maisDeUm.Select(cd => amostras[cd[0].IntObrigatorio("ID_LAB_HIST_AMOSTRAS")].IntObrigatorio("COD_AMOSTRA") + "/" +
                    amostras[cd[0].IntObrigatorio("ID_LAB_HIST_AMOSTRAS")].IntObrigatorio("ANO_AMOSTRA"))) + ".");

        // resultado novo → (parâmetro, método) do legado → parâmetros novos, na ordem (origem da revisão seguinte)
        var porResultado = new Dictionary<int, Dictionary<(int, int), List<int>>>();
        int datas = 0, anos = 0, outraAmostra = 0, repetidos = 0, totalPar = 0;
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        for (var camada = 0; camada < (cadeias.Count == 0 ? 0 : cadeias.Max(cd => cd.Count)); camada++)
        {
            var linhas = cadeias.Where(cd => cd.Count > camada).Select(cd => (Cadeia: cd, R: cd[camada])).ToList();
            var valores = linhas.Select(x => new object?[]
            {
                c.IdEmpresa, c.Mapa("LAB_HIST_AMOSTRAS")[x.R.IntObrigatorio("ID_LAB_HIST_AMOSTRAS")], camada,
                camada == 0 ? null : mapaRes[x.Cadeia[camada - 1].IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")], camada == x.Cadeia.Count - 1,
                c.Novo("ENTIDADES_FUNCIONARIOS", x.R.Int("ID_ENTIDADES_FUNC_DIG"), $"resultado {x.R.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")}")
                    ?? throw new InvalidOperationException($"resultado {x.R.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")}: digitador não migrado."),
                x.R.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES"),
            }).ToList();
            var ids = await c.InserirEmLoteAsync("LAB_RESULTADOS_ANALISES", "ID_LAB_RESULTADOS_ANALISES", ColunasResultado, valores, ct);
            foreach (var (x, id) in linhas.Zip(ids)) mapaRes[x.R.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")] = id;

            var valoresPar = new List<object?[]>();
            var chaves = new List<(int Antigo, int Resultado, (int, int) Par)>();
            foreach (var (cadeia, r) in linhas)
            {
                var antigo = r.IntObrigatorio("ID_LAB_RESULTADOS_ANALISES");
                var amostra = r.IntObrigatorio("ID_LAB_HIST_AMOSTRAS");
                var daOrigem = camada == 0 ? null : porResultado.GetValueOrDefault(mapaRes[cadeia[camada - 1].IntObrigatorio("ID_LAB_RESULTADOS_ANALISES")]);
                var usados = new Dictionary<int, LinhaFb>(); // teste do legado → parâmetro que ficou com ele
                var ocorrencia = new Dictionary<(int, int), int>();
                foreach (var p in parametros[antigo])
                {
                    var idPar = p.IntObrigatorio("ID_LAB_RESULTADOS_PARAM");
                    var onde = $"parâmetro {idPar} do resultado {antigo}";
                    var teste = testes[p.IntObrigatorio("ID_LAB_HIST_AMOSTRA_TESTES")];
                    if (teste.Amostra != amostra)
                    {
                        var equivalentes = testesDaAmostra[amostra].Where(t => (t.Parametro, t.Metodo) == (teste.Parametro, teste.Metodo)).ToList();
                        outraAmostra++;
                        if (equivalentes.Count == 1)
                        {
                            c.Anomalia($"{onde}: teste {teste.Id} é da amostra {teste.Amostra}, não da {amostra} — ligado ao teste equivalente {equivalentes[0].Id}.");
                            teste = equivalentes[0];
                        }
                        else c.Anomalia($"{onde}: teste {teste.Id} é da amostra {teste.Amostra}, não da {amostra}, sem equivalente único — mantido.");
                    }
                    if (usados.TryGetValue(teste.Id, out var outro))
                    {
                        repetidos++;
                        var livre = testesDaAmostra[teste.Amostra].FirstOrDefault(t => (t.Parametro, t.Metodo) == (teste.Parametro, teste.Metodo) && !usados.ContainsKey(t.Id));
                        if (livre is not null)
                        {
                            c.Anomalia($"{onde}: teste {teste.Id} repetido no resultado — ligado ao outro teste equivalente {livre.Id}.");
                            teste = livre;
                        }
                        else
                        {
                            // uma linha só: a impressa no boletim; senão a mais recente
                            var idOutro = outro.IntObrigatorio("ID_LAB_RESULTADOS_PARAM");
                            var fica = noBoletim.Contains(idOutro) && !noBoletim.Contains(idPar) ? outro : p;
                            var sai = ReferenceEquals(fica, p) ? outro : p;
                            var igual = Texto(fica) == Texto(sai);
                            c.Anomalia($"{onde}: teste {teste.Id} repetido no resultado — fica o parâmetro {fica.IntObrigatorio("ID_LAB_RESULTADOS_PARAM")}" +
                                (igual ? " (mesmo valor)." : $" (\"{Texto(fica)}\"; descartado \"{Texto(sai)}\")."));
                            fundidos[sai.IntObrigatorio("ID_LAB_RESULTADOS_PARAM")] = fica.IntObrigatorio("ID_LAB_RESULTADOS_PARAM");
                            usados[teste.Id] = fica;
                            continue;
                        }
                    }
                    usados[teste.Id] = p;
                }

                foreach (var (idTeste, p) in usados)
                {
                    var idPar = p.IntObrigatorio("ID_LAB_RESULTADOS_PARAM");
                    var onde = $"parâmetro {idPar} do resultado {antigo}";
                    var teste = testes[idTeste];
                    var par = (teste.Parametro, teste.Metodo);
                    var k = ocorrencia[par] = ocorrencia.GetValueOrDefault(par) + 1;
                    int? origem = daOrigem is not null && daOrigem.TryGetValue(par, out var os) && os.Count >= k ? os[k - 1] : null;
                    var (inicio, fim) = (p.Data("DT_INICIAL")!.Value, p.Data("DT_RESULTADO")!.Value);
                    if (Conversoes.AnoDigitadoErrado(inicio, fim, hoje) is { Corrigiu: true } certo)
                    {
                        anos++;
                        c.Anomalia($"{onde}: ano digitado errado — início {inicio:dd/MM/yyyy} e resultado {fim:dd/MM/yyyy} gravados como {certo.Inicio:dd/MM/yyyy} e {certo.Resultado:dd/MM/yyyy}.");
                        (inicio, fim) = (certo.Inicio, certo.Resultado);
                    }
                    else if (inicio.Year < 2010 || fim.Year < 2010 || inicio > hoje || fim > hoje)
                        c.Anomalia($"{onde}: data com ano impossível ({inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}) sem como corrigir — migrada como está; corrigir pela T26 (revisão do resultado).");
                    if (fim < inicio)
                    {
                        datas++;
                        c.Anomalia($"{onde}: resultado em {fim:dd/MM/yyyy} antes do início {inicio:dd/MM/yyyy} — início gravado como {fim:dd/MM/yyyy}.");
                        inicio = fim;
                    }
                    int? Id(string tabela, string coluna) => p.Int(coluna) is > 0 and var v ? c.Novo(tabela, v, onde) : null;
                    valoresPar.Add(
                    [
                        c.IdEmpresa, mapaRes[antigo],
                        c.Mapa("LAB_HIST_AMOSTRAS_TESTES").TryGetValue(idTeste, out var t) ? t : throw new InvalidOperationException($"{onde}: teste {idTeste} não migrado."),
                        inicio, fim, Id("ENTIDADES_FUNCIONARIOS", "ID_ENTIDADES_FUNC_ANALISE"), Id("ENTIDADES_FUNCIONARIOS", "ID_ENTIDADES_FUNC_DIG"),
                        p.Decimal("QT_AMOSTRA") is { } q ? decimal.Round(q, 4) : null, Id("UNIDADES", "ID_UNIDADES_AMOSTRA"),
                        p.Int("TIPO_RESULTADO") == 1, p.Texto("SIMBOLO_GRANDEZA"), p.Texto("VR_RESULTADO"), Expoente(p, "VR_EXP_RESULTADO"),
                        Id("UNIDADES", "ID_UNIDADES"), Id("LAB_TIPOS_RESULTADOS", "ID_LAB_TIPOS_RESULTADOS"),
                        p.Int("TIPO_RESULTADO_ESPECIF") == 1, p.Texto("SIMBOLO_GR_ESPECIF"), p.Texto("VR_RESULTADO_ESPECIF"), Expoente(p, "VR_EXP_ESPECIF"),
                        Id("UNIDADES", "ID_UNIDADES_ESPECIF"), Id("LAB_TIPOS_RESULTADOS", "ID_LAB_TIPO_RESULTADOS_ESP"),
                        p.Int("VR_SATISFEITO") is { } s ? s == 1 : null, origem, idPar,
                    ]);
                    chaves.Add((idPar, mapaRes[antigo], par));
                }
            }
            var idsPar = await c.InserirEmLoteAsync("LAB_RESULTADOS_PARAM", "ID_LAB_RESULTADOS_PARAM", ColunasParam, valoresPar, ct);
            foreach (var ((antigo, resultado, par), id) in chaves.Zip(idsPar))
            {
                mapaPar[antigo] = id;
                if (!porResultado.TryGetValue(resultado, out var d)) porResultado[resultado] = d = [];
                (d.TryGetValue(par, out var lista) ? lista : d[par] = []).Add(id);
            }
            totalPar += idsPar.Count;
        }
        foreach (var sai in fundidos.Keys)
        {
            var fica = fundidos[sai];
            while (fundidos.TryGetValue(fica, out var seguinte)) fica = seguinte; // 3 ou mais repetidos
            mapaPar[sai] = mapaPar[fica];
        }

        if (outraAmostra > 0) c.Log($"{outraAmostra} parâmetro(s) com teste de outra amostra no legado (ver anomalias).");
        c.Conferir("resultados do legado (− sem amostra migrada)", resultados.Count - semAmostra.Count, await c.Db.LabResultadosAnalises.CountAsync(ct));
        c.Conferir("parâmetros (legado − repetidos fundidos − de resultado sem amostra migrada)",
            parametros.Sum(g => g.Count()) - fundidos.Count - semAmostra.Sum(r => parametros[r].Count()), await c.Db.LabResultadosParam.CountAsync(ct));
        c.Conferir("amostras físicas com exatamente uma última revisão do resultado", 0,
            await c.Db.LabResultadosAnalises.GroupBy(r => r.LabHistAmostra.IdAmostraRaiz ?? r.IdLabHistAmostras)
                .Select(g => g.Sum(r => r.UltimaRevisao ? 1 : 0)).CountAsync(n => n != 1, ct));
        c.Conferir("parâmetros com valor e sem texto formatado", 0,
            await c.Db.LabResultadosParam.CountAsync(p => p.ResultadoFormatado == null
                && (p.TipoResultado ? p.VrResultado != null && p.VrResultado != "" : p.IdLabTiposResultados != null), ct));
        c.Log($"{mapaRes.Count} resultados, {totalPar} parâmetros; {datas} data(s) inicial(is) corrigida(s), {anos} ano(s) digitado(s) errado(s) corrigido(s), {repetidos} teste(s) repetido(s) no mesmo resultado.");
        return mapaRes.Count + totalPar;
    }

    private static short? Expoente(LinhaFb l, string coluna) => l.Decimal(coluna) is { } e ? (short)decimal.Round(e) : null;

    private static string Texto(LinhaFb p) => p.Int("TIPO_RESULTADO") == 1
        ? $"{p.Texto("SIMBOLO_GRANDEZA")} {p.Texto("VR_RESULTADO")}e{p.Decimal("VR_EXP_RESULTADO"):0} u{p.Int("ID_UNIDADES")}".Trim()
        : $"tipo {p.Int("ID_LAB_TIPOS_RESULTADOS")}";
}
