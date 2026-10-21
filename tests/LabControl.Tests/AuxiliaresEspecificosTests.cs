using System.Net;
using LabControl.Services.Auxiliares;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 2.2: o específico de cada auxiliar T10-08…T10-20 (o padrão T10-00 está em <see cref="AuxiliaresTests"/>) —
/// campos extras, duplicidade de código/sigla, código imutável (T10-14/16), status que permite resultado (T10-15)
/// e as parcelas da condição de pagamento (T10-11).
/// </summary>
public class AuxiliaresEspecificosTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static AuxiliarDefinicao Def(string tabela) => CatalogoAuxiliares.Encontrar(tabela)!;

    private static AuxiliarFormVM Form(int id = 0, bool ativo = true, params (string Campo, string? Valor)[] valores)
    {
        var vm = new AuxiliarFormVM { Id = id, Ativo = ativo };
        foreach (var (campo, valor) in valores) vm.Valores[campo] = valor;
        return vm;
    }

    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    [Theory]
    [InlineData("UNIDADES"), InlineData("EMBALAGENS_TIPOS"), InlineData("LAB_TIPOS_RESULTADOS"), InlineData("CONDICOES_PAGTOS"),
     InlineData("PRAZOS"), InlineData("MOEDAS"), InlineData("AMOSTRAS_TIPO"), InlineData("AMOSTRAS_STATUS"), InlineData("ANALISES_TIPO"),
     InlineData("ANALISES_STATUS"), InlineData("ANALISES_METODOS"), InlineData("BOLETINS_STATUS"), InlineData("LAB_PROPOSTAS_STATUS")]
    public async Task Cada_auxiliar_lista_e_abre_o_formulario(string tabela)
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);
        var def = Def(tabela);
        var lista = await def.ListarAsync(s, new AuxiliarFiltroVM { Inativos = true });
        Assert.Equal(def.Campos.Count(x => x.NaLista), lista.Colunas.Count);
        Assert.NotNull(await def.CarregarAsync(s, 0));
        if (lista.Itens.Count > 0) Assert.NotNull(await def.CarregarAsync(s, lista.Itens[0].Id));
    }

    [Fact]
    public void Catalogo_tem_os_20_auxiliares()
    {
        Assert.Equal(20, CatalogoAuxiliares.Todos.Count);
        Assert.Equal(20, CatalogoAuxiliares.Todos.Select(d => d.Tabela).Distinct().Count());
    }

    [Fact]
    public async Task Sigla_duplicada_vai_para_o_campo_sigla()
    {
        await using var c = await Cenario.CriarAsync(); // o cenário tem a unidade UFC/g
        var s = new AuxiliarService(c.Db);

        var dup = await Def("UNIDADES").GravarAsync(s, Form(valores: [("Descricao", Unico("UFC")), ("Sigla", "UFC/g")]));
        Assert.Equal(("Já existe uma unidade com esta sigla.", "Sigla"), (dup.Mensagem, dup.Campo));

        var moeda = await Def("MOEDAS").GravarAsync(s, Form(valores: [("Descricao", "EURO"), ("Sigla", "€")]));
        Assert.True(moeda.Sucesso, moeda.Mensagem);
        var dupMoeda = await Def("MOEDAS").GravarAsync(s, Form(valores: [("Descricao", "EURO 2"), ("Sigla", "€")]));
        Assert.Equal(("Já existe uma moeda com esta sigla.", "Sigla"), (dupMoeda.Mensagem, dupMoeda.Campo));

        var cond = await Def("CONDICOES_PAGTOS").GravarAsync(s, Form(valores: [("Codigo", "01"), ("Descricao", Unico("30 DIAS"))]));
        Assert.Equal(("Já existe uma condição de pagamento com este código.", "Codigo"), (cond.Mensagem, cond.Campo));
    }

    [Fact]
    public async Task Prazo_grava_dias_e_dias_uteis()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);
        var prazos = Def("PRAZOS");

        var negativo = await prazos.GravarAsync(s, Form(valores: [("Descricao", Unico("PRAZO")), ("QtdeDias", "-1")]));
        Assert.Equal(("Qtde. de dias: informe de 0 a 3650.", "QtdeDias"), (negativo.Mensagem, negativo.Campo));

        var nome = Unico("15 DIAS");
        var r = await prazos.GravarAsync(s, Form(valores: [("Descricao", nome), ("QtdeDias", "15"), ("DiasUteis", "true")]));
        Assert.True(r.Sucesso, r.Mensagem);
        var p = await c.Db.Prazos.AsNoTracking().SingleAsync(x => x.IdPrazos == r.Valor);
        Assert.Equal((15, true), (p.QtdeDias, p.DiasUteis));
        var linha = (await prazos.ListarAsync(s, new AuxiliarFiltroVM { Busca = nome })).Itens.Single();
        Assert.Equal([nome, "15", "Sim"], linha.Valores);
    }

    [Fact]
    public async Task Status_grava_cor_e_recusa_formato_invalido()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);
        var def = Def("BOLETINS_STATUS");

        var invalida = await def.GravarAsync(s, Form(valores: [("Descricao", Unico("STATUS")), ("Cor", "vermelho")]));
        Assert.Equal(("Cor: use o formato #RRGGBB.", "Cor"), (invalida.Mensagem, invalida.Campo));
        var r = await def.GravarAsync(s, Form(valores: [("Descricao", Unico("STATUS")), ("Cor", "#ff8800")]));
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal("#FF8800", (await c.Db.BoletinsStatus.AsNoTracking().SingleAsync(x => x.IdBoletinsStatus == r.Valor)).Cor);

        var prop = await Def("LAB_PROPOSTAS_STATUS").GravarAsync(s, Form(valores: [("Descricao", Unico("CANCELADA")), ("PermiteAmostra", "false")]));
        Assert.True(prop.Sucesso, prop.Mensagem);
        Assert.False((await c.Db.LabPropostasStatus.AsNoTracking().SingleAsync(x => x.IdLabPropostasStatus == prop.Valor)).PermiteAmostra);
    }

    [Theory]
    [InlineData("AMOSTRAS_TIPO", "tipo de amostra")]
    [InlineData("ANALISES_TIPO", "tipo de análise")]
    public async Task Codigo_de_1_a_99_e_unico(string tabela, string singular)
    {
        await using var c = await Cenario.CriarAsync(); // códigos 4 (amostra) e 1 (análise) já existem
        var s = new AuxiliarService(c.Db);
        var def = Def(tabela);

        foreach (var fora in new[] { "0", "100" })
        {
            var r = await def.GravarAsync(s, Form(valores: [("Codigo", fora), ("Descricao", Unico("TIPO"))]));
            Assert.Equal(("Código: informe de 1 a 99.", "Codigo"), (r.Mensagem, r.Campo));
        }
        var existente = tabela == "AMOSTRAS_TIPO" ? c.TipoAmostra.Codigo : c.TipoAnalise.Codigo;
        var dup = await def.GravarAsync(s, Form(valores: [("Codigo", existente.ToString()), ("Descricao", Unico("TIPO"))]));
        Assert.Equal(($"Já existe um {singular} com este código.", "Codigo"), (dup.Mensagem, dup.Campo));
        Assert.True((await def.GravarAsync(s, Form(valores: [("Codigo", "99"), ("Descricao", Unico("TIPO"))]))).Sucesso);
    }

    [Fact]
    public async Task Codigo_do_tipo_de_amostra_trava_depois_de_usado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);
        var def = Def("AMOSTRAS_TIPO");
        var id = c.TipoAmostra.IdAmostrasTipo;

        // sem amostra: o código muda
        var form = (await def.CarregarAsync(s, id))!;
        Assert.False(form.Campos.Single(x => x.Nome == "Codigo").Bloqueado);
        form.Valores["Codigo"] = "44";
        Assert.True((await def.GravarAsync(s, form)).Sucesso);

        c.Db.Add(c.NovaAmostra(await c.HojeAsync()));
        await c.Db.SaveChangesAsync();
        c.Db.ChangeTracker.Clear();

        form = (await def.CarregarAsync(s, id))!;
        Assert.True(form.Campos.Single(x => x.Nome == "Codigo").Bloqueado);
        Assert.False(form.Campos.Single(x => x.Nome == "Descricao").Bloqueado);
        form.Valores["Codigo"] = "45";
        var r = await def.GravarAsync(s, form);
        Assert.Equal(("Código: não pode ser alterado (já está em uso).", "Codigo"), (r.Mensagem, r.Campo));

        // a descrição continua editável com o mesmo código
        form.Valores["Codigo"] = "44";
        form.Valores["Descricao"] = Unico("ALIMENTO");
        Assert.True((await def.GravarAsync(s, form)).Sucesso);
        Assert.Equal(44, (await c.Db.AmostrasTipo.AsNoTracking().SingleAsync(x => x.IdAmostrasTipo == id)).Codigo);
    }

    [Fact]
    public async Task Codigo_do_tipo_de_analise_trava_quando_um_parametro_usa()
    {
        await using var c = await Cenario.CriarAsync(); // os parâmetros do cenário usam o tipo de análise
        var s = new AuxiliarService(c.Db);
        var def = Def("ANALISES_TIPO");

        var form = (await def.CarregarAsync(s, c.TipoAnalise.IdAnalisesTipo))!;
        Assert.True(form.Campos.Single(x => x.Nome == "Codigo").Bloqueado);
        form.Valores["Codigo"] = "77";
        Assert.Equal("Código: não pode ser alterado (já está em uso).", (await def.GravarAsync(s, form)).Mensagem);

        var livre = await def.GravarAsync(s, Form(valores: [("Codigo", "50"), ("Descricao", Unico("LIVRE"))]));
        form = (await def.CarregarAsync(s, livre.Valor))!;
        Assert.False(form.Campos.Single(x => x.Nome == "Codigo").Bloqueado);
    }

    [Fact]
    public async Task Sempre_resta_um_status_de_amostra_que_permite_resultado()
    {
        await using var c = await Cenario.CriarAsync(); // seed: só "LIBERADA PARA ANÁLISE" permite
        var s = new AuxiliarService(c.Db);
        var def = Def("AMOSTRAS_STATUS");
        var liberada = await c.Db.AmostrasStatus.AsNoTracking().SingleAsync(x => x.PermiteResultado);
        const string Msg = "Deve existir ao menos um status ativo que permite resultado (sem ele nenhuma amostra chega a Resultados e Boletim).";

        var inativar = await def.DefinirAtivoAsync(s, liberada.IdAmostrasStatus, false);
        Assert.Equal(Msg, inativar.Mensagem);
        c.Db.ChangeTracker.Clear();

        var form = (await def.CarregarAsync(s, liberada.IdAmostrasStatus))!;
        form.Valores["PermiteResultado"] = "false";
        var desmarcar = await def.GravarAsync(s, form);
        Assert.Equal((Msg, "PermiteResultado"), (desmarcar.Mensagem, desmarcar.Campo));
        c.Db.ChangeTracker.Clear();

        Assert.Equal(Msg, (await def.ExcluirAsync(s, liberada.IdAmostrasStatus)).Mensagem);

        // com outro que permite, o primeiro pode ser inativado; um que não permite pode ser excluído
        var outro = await def.GravarAsync(s, Form(valores: [("Descricao", Unico("LIBERADA 2")), ("PermiteResultado", "true")]));
        Assert.True(outro.Sucesso, outro.Mensagem);
        Assert.True((await def.DefinirAtivoAsync(s, liberada.IdAmostrasStatus, false)).Sucesso);
        Assert.True((await def.DefinirAtivoAsync(s, liberada.IdAmostrasStatus, true)).Sucesso);
        var semResultado = await def.GravarAsync(s, Form(valores: [("Descricao", Unico("EM ESPERA"))]));
        Assert.True((await def.ExcluirAsync(s, semResultado.Valor)).Sucesso);

        // 2.5 (code-review): empresa sem nenhum que permite (migrada, copiada) ainda edita e inativa os outros status
        await c.Db.AmostrasStatus.ExecuteUpdateAsync(x => x.SetProperty(st => st.PermiteResultado, false));
        c.Db.ChangeTracker.Clear();
        var bloqueada = await c.Db.AmostrasStatus.AsNoTracking().FirstAsync(x => x.Descricao == "BLOQUEADA");
        var renomear = (await def.CarregarAsync(s, bloqueada.IdAmostrasStatus))!;
        renomear.Valores["Descricao"] = Unico("BLOQUEADA");
        renomear.Valores["Cor"] = "#123456";
        Assert.True((await def.GravarAsync(s, renomear)).Sucesso);
        Assert.True((await def.DefinirAtivoAsync(s, bloqueada.IdAmostrasStatus, false)).Sucesso);
    }

    [Fact]
    public async Task Excluir_so_oferece_inativar_quando_o_registro_esta_em_uso()
    {
        await using var c = await Cenario.CriarAsync();
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "AUXILIARES.CONSULTAR", "AUXILIARES.ALTERAR", "AUXILIARES.EXCLUIR");
        var liberada = await c.Db.AmostrasStatus.Where(x => x.PermiteResultado).Select(x => x.IdAmostrasStatus).SingleAsync();
        using var web = app.Cliente();
        await web.EntrarAsync(editor.Login);

        // regra do auxiliar (último que permite resultado) e registro inexistente: só a mensagem, sem "Inativar"
        foreach (var id in new[] { liberada, 999_999_999 })
        {
            var r = await web.PostarAsync("/Auxiliares/AMOSTRAS_STATUS", $"/Auxiliares/AMOSTRAS_STATUS/Excluir/{id}");
            var html = await (await web.GetAsync(r.Destino()!)).HtmlAsync();
            Assert.DoesNotContain("está em uso", html); // o aviso com o botão "Inativar"
        }

        // em uso (FK): oferece inativar, com o gênero certo
        var r2 = await web.PostarAsync("/Auxiliares/UNIDADES", $"/Auxiliares/UNIDADES/Excluir/{c.Unidade.IdUnidades}");
        Assert.Contains("Esta unidade está em uso e não pode ser excluída.", await (await web.GetAsync(r2.Destino()!)).HtmlAsync());
    }

    [Fact]
    public async Task Parcelas_somam_100_e_sao_regravadas_no_lugar()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new CondicaoPagamentoService(c.Db);
        var id = c.Proposta.IdCondicoesPagtos;
        ParcelasCondicaoVM Vm(params (string? Dias, string? Porc)[] linhas) =>
            new() { Id = id, Linhas = linhas.Select(l => new ParcelaLinhaVM { Dias = l.Dias, Porcentagem = l.Porc }).ToList() };

        Assert.Equal("A soma das porcentagens deve ser 100% (está em 90%).", (await s.GravarAsync(Vm(("30", "50"), ("60", "40")))).Mensagem);
        Assert.Equal("Parcela 2: informe os dias para vencimento (0 a 3650).", (await s.GravarAsync(Vm(("30", "50"), ("-1", "50")))).Mensagem);
        Assert.Equal("Parcela 1: informe a porcentagem (maior que 0 e até 100).", (await s.GravarAsync(Vm(("30", "0"), ("60", "100")))).Mensagem);
        Assert.Equal("Parcela 1: porcentagem com no máximo 4 casas decimais.", (await s.GravarAsync(Vm(("0", "33,33333"), ("30", "66,66667")))).Mensagem);

        // 3 parcelas (linha em branco ignorada), vírgula decimal
        Assert.True((await s.GravarAsync(Vm(("0", "33,3333"), (null, null), ("30", "33,3333"), ("60", "33.3334")))).Sucesso);
        var vm = (await s.CarregarAsync(id))!;
        Assert.Equal([("0", "33,3333"), ("30", "33,3333"), ("60", "33,3334")], vm.Linhas.Select(l => (l.Dias, l.Porcentagem)));

        // menos parcelas: as primeiras são alteradas no lugar, a que sobra sai
        var ids = await c.Db.CondicoesPagtosParc.AsNoTracking().Where(p => p.IdCondicoesPagtos == id).OrderBy(p => p.NrParcela).Select(p => p.IdCondicoesPagtosParc).ToListAsync();
        c.Db.ChangeTracker.Clear();
        Assert.True((await s.GravarAsync(Vm(("28", "50"), ("56", "50")))).Sucesso);
        var depois = await c.Db.CondicoesPagtosParc.AsNoTracking().Where(p => p.IdCondicoesPagtos == id).OrderBy(p => p.NrParcela).ToListAsync();
        Assert.Equal(ids.Take(2), depois.Select(p => p.IdCondicoesPagtosParc));
        Assert.Equal([(1, 28, 50m), (2, 56, 50m)], depois.Select(p => (p.NrParcela, p.DiasVencimento, p.PorcVencimento)));

        // sem parcelas é permitido; outra empresa não enxerga a condição
        c.Db.ChangeTracker.Clear();
        Assert.True((await s.GravarAsync(Vm())).Sucesso);
        Assert.False(await c.Db.CondicoesPagtosParc.AnyAsync(p => p.IdCondicoesPagtos == id));
        await using var b = await Cenario.CriarAsync();
        var deB = new CondicaoPagamentoService(b.Db);
        Assert.Null(await deB.CarregarAsync(id));
        Assert.False((await deB.GravarAsync(Vm(("0", "100")))).Sucesso);
    }

    [Fact]
    public async Task Tela_de_parcelas_e_permissoes()
    {
        await using var c = await Cenario.CriarAsync();
        var id = c.Proposta.IdCondicoesPagtos;
        var url = $"/Auxiliares/CONDICOES_PAGTOS/Parcelas/{id}";
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "AUXILIARES.CONSULTAR");
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "AUXILIARES.CONSULTAR", "AUXILIARES.ALTERAR");

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            Assert.Contains(url, await (await web.GetAsync("/Auxiliares/CONDICOES_PAGTOS")).HtmlAsync()); // botão Parcelas
            var html = await (await web.GetAsync(url)).HtmlAsync();
            Assert.Contains("Parcelas", html);
            Assert.DoesNotContain("Adicionar parcela", html);
            Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync(url, url, ("Linhas[0].Dias", "0"), ("Linhas[0].Porcentagem", "100"))).StatusCode);
        }

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(editor.Login);
            var erro = await web.PostarAsync(url, url, ("Linhas[0].Dias", "0"), ("Linhas[0].Porcentagem", "60"));
            Assert.Contains("A soma das porcentagens deve ser 100% (está em 60%).", await erro.HtmlAsync());
            var ok = await web.PostarAsync(url, url, ("Linhas[0].Dias", "0"), ("Linhas[0].Porcentagem", "40"), ("Linhas[1].Dias", "30"), ("Linhas[1].Porcentagem", "60"));
            Assert.Equal(url, ok.Destino());
            Assert.Equal(2, await c.Db.CondicoesPagtosParc.CountAsync(p => p.IdCondicoesPagtos == id));
            Assert.Equal(HttpStatusCode.NotFound, (await web.GetAsync("/Auxiliares/CONDICOES_PAGTOS/Parcelas/999999999")).StatusCode);
        }
    }
}
