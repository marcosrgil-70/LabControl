using System.Net;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 3.4 (T15): sem descrição reduzida não grava; tipo, método padrão, valor e acreditado; filtros; em uso.</summary>
public class ParametrosAnaliseTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    [Fact]
    public async Task Grava_com_descricao_reduzida_metodo_valor_e_acreditado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ParametroAnaliseService(c.Db);
        var tipo = c.TipoAnalise.IdAnalisesTipo;
        var metodo = await c.Db.AnalisesMetodos.Where(m => m.Descricao == "ISO 4833").Select(m => m.IdAnalisesMetodos).SingleAsync();

        Assert.Equal(("Informe a descrição reduzida (é o texto impresso no boletim).", "DescReduzida"),
            Erro(await s.GravarAsync(new ParametroAnaliseVM { Descricao = "BOLORES", DescReduzida = " ", IdAnalisesTipo = tipo })));
        Assert.Equal(("Escolha o tipo de análise.", "IdAnalisesTipo"), Erro(await s.GravarAsync(new ParametroAnaliseVM { Descricao = "BOLORES", DescReduzida = "BOLORES" })));
        Assert.Equal("Valor unitário: em reais, maior ou igual a zero, com até 2 casas (ex.: 1.234,56).",
            (await s.GravarAsync(new ParametroAnaliseVM { Descricao = "BOLORES", DescReduzida = "BOLORES", IdAnalisesTipo = tipo, VrUnitario = "-1" })).Mensagem);

        var nome = Unico("bolores e leveduras");
        var r = await s.GravarAsync(new ParametroAnaliseVM
        {
            Descricao = nome, DescReduzida = "bolores", IdAnalisesTipo = tipo, IdAnalisesMetodos = metodo,
            DescrMetodo = "bolores e leveduras", VrUnitario = "1.234,56", Auditado = true,
        });
        Assert.True(r.Sucesso, r.Mensagem);
        var p = await c.Db.LabParametrosAnalises.AsNoTracking().SingleAsync(x => x.IdLabParametrosAnalises == r.Valor);
        Assert.Equal((nome.ToUpperInvariant(), "BOLORES", (int?)metodo, "BOLORES E LEVEDURAS", 1234.56m, true),
            (p.Descricao, p.DescReduzida, p.IdAnalisesMetodos, p.DescrMetodo, p.VrUnitario, p.Auditado));
        Assert.Equal("1.234,56", (await s.CarregarAsync(r.Valor))!.VrUnitario);

        // ponto decimal também vale; vazio = 0
        Assert.True((await s.GravarAsync(new ParametroAnaliseVM { Descricao = Unico("PH"), DescReduzida = "PH", IdAnalisesTipo = tipo, VrUnitario = "45.5" })).Sucesso);
        var semValor = await s.GravarAsync(new ParametroAnaliseVM { Descricao = Unico("UMIDADE"), DescReduzida = "UMIDADE", IdAnalisesTipo = tipo });
        Assert.Equal(0m, await c.Db.LabParametrosAnalises.Where(x => x.IdLabParametrosAnalises == semValor.Valor).Select(x => x.VrUnitario).SingleAsync());

        // descrição única por empresa, no campo
        var dup = await s.GravarAsync(new ParametroAnaliseVM { Descricao = "contagem total", DescReduzida = "X", IdAnalisesTipo = tipo });
        Assert.Equal(("Já existe um parâmetro de análise com esta descrição.", "Descricao"), Erro(dup));

        // filtros: tipo + acreditado + texto
        var acreditados = await s.ListarAsync(new ParametroAnaliseFiltroVM { IdAnalisesTipo = tipo, Acreditado = "sim" });
        Assert.Contains(acreditados.Itens, i => i.Id == r.Valor && i.VrUnitario == "1.234,56" && i.Metodo == "ISO 4833" && i.Tipo == "01 — MICROBIOLÓGICA");
        Assert.DoesNotContain(acreditados.Itens, i => i.Descricao == "SALMONELLA SP");
        Assert.Contains((await s.ListarAsync(new ParametroAnaliseFiltroVM { Acreditado = "nao" })).Itens, i => i.Descricao == "SALMONELLA SP");
        Assert.Equal([r.Valor], (await s.ListarAsync(new ParametroAnaliseFiltroVM { Texto = nome[^6..] })).Itens.Select(i => i.Id));
    }

    [Fact]
    public async Task Em_uso_nao_exclui_inativo_some_e_outra_empresa_nao_ve()
    {
        await using var c = await Cenario.CriarAsync(); // os dois parâmetros do cenário estão na proposta
        var s = new ParametroAnaliseService(c.Db);
        var usado = c.ParamDescritivo.IdLabParametrosAnalises;

        Assert.Equal("Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).", (await s.ExcluirAsync(usado)).Mensagem);
        Assert.True((await s.DefinirInativoAsync(usado, true)).Sucesso);
        var lista = await s.ListarAsync(new ParametroAnaliseFiltroVM { Texto = "SALMONELLA" });
        Assert.Equal((0, 1), (lista.Itens.Count, lista.InativosOcultos));

        // método inativado sai do combo da inclusão
        var met = await c.Db.AnalisesMetodos.FirstAsync();
        await c.Db.AnalisesMetodos.Where(m => m.IdAnalisesMetodos == met.IdAnalisesMetodos).ExecuteUpdateAsync(x => x.SetProperty(m => m.Ativo, false));
        Assert.DoesNotContain((await s.NovoAsync()).Metodos, m => m.Id == met.IdAnalisesMetodos);

        var livre = (await s.GravarAsync(new ParametroAnaliseVM { Descricao = Unico("LIVRE"), DescReduzida = "LIVRE", IdAnalisesTipo = c.TipoAnalise.IdAnalisesTipo })).Valor;
        await using var b = await Cenario.CriarAsync();
        var deB = new ParametroAnaliseService(b.Db);
        Assert.Null(await deB.CarregarAsync(livre));
        Assert.Equal(ParametroAnaliseService.MensagemNaoEncontrado, (await deB.ExcluirAsync(livre)).Mensagem);
        Assert.True((await s.ExcluirAsync(livre)).Sucesso);
    }

    [Fact]
    public async Task Tela_permissoes_e_volta_para_a_lista()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "PARAMETROS_ANALISE.CONSULTAR");
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "PARAMETROS_ANALISE.CONSULTAR", "PARAMETROS_ANALISE.INCLUIR", "PARAMETROS_ANALISE.ALTERAR");

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            var html = await (await web.GetAsync("/ParametrosAnalise")).HtmlAsync();
            Assert.Contains("CONTAGEM TOTAL", html);
            Assert.DoesNotContain("href=\"/ParametrosAnalise/Criar\"", html);
            Assert.Contains("<fieldset disabled", await (await web.GetAsync($"/ParametrosAnalise/Editar/{c.ParamAcreditado.IdLabParametrosAnalises}")).HtmlAsync());
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/ParametrosAnalise/Criar")).StatusCode);
        }

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(editor.Login);
            var nome = Unico("COLIFORMES");
            var criar = await web.PostarAsync("/ParametrosAnalise/Criar", "/ParametrosAnalise/Criar", ("Descricao", nome), ("DescReduzida", "COLIFORMES"),
                ("IdAnalisesTipo", c.TipoAnalise.IdAnalisesTipo.ToString()), ("VrUnitario", "80,00"), ("Auditado", "true"), ("Auditado", "false"), ("Inativo", "false"));
            Assert.Equal("/ParametrosAnalise", criar.Destino());
            Assert.Contains($"Parâmetro {nome} gravado.", await (await web.GetAsync("/ParametrosAnalise")).HtmlAsync());
            Assert.True(await c.Db.LabParametrosAnalises.AnyAsync(p => p.Descricao == nome && p.Auditado && p.VrUnitario == 80m));

            var semReduzida = await (await web.PostarAsync("/ParametrosAnalise/Criar", "/ParametrosAnalise/Criar", ("Descricao", Unico("X")),
                ("IdAnalisesTipo", c.TipoAnalise.IdAnalisesTipo.ToString()))).HtmlAsync();
            Assert.Contains("Informe a descrição reduzida (é o texto impresso no boletim).", semReduzida);
        }
    }

    private static (string?, string?) Erro(LabControl.Infra.IResultado r) => (r.Mensagem, r.Campo);
}
