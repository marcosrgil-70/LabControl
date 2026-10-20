using System.Net;
using LabControl.Infra;
using LabControl.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace LabControl.Tests;

/// <summary>Action protegida usada só nos testes (registrada pelo <see cref="AppWeb"/>).</summary>
[Route("teste-permissao")]
public sealed class TestePermissaoController : Controller
{
    [HttpGet("leitura"), Permissao("AMOSTRAS.CONSULTAR")]
    public IActionResult Leitura() => Content("ok-leitura");

    [HttpPost("inclusao"), Permissao("AMOSTRAS.INCLUIR")]
    public IActionResult Inclusao() => Content("ok-inclusao");
}

/// <summary>Aceite da 1.2 (docs/telas/T01-layout-menu.md): permissões, menu e painel.</summary>
public class PermissoesMenuPainelTests(AppWeb app) : IClassFixture<AppWeb>
{
    [Fact]
    public async Task Action_sem_a_permissao_devolve_403_e_com_ela_executa()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "AMOSTRAS.CONSULTAR");
        var semNada = await UsuariosTeste.ComPermissoesAsync(c);

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            Assert.Equal("ok-leitura", await (await web.GetAsync("/teste-permissao/leitura")).Content.ReadAsStringAsync());
            var post = await web.PostarAsync("/", "/teste-permissao/inclusao");
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode); // POST sem permissão: 403 (P02)
            Assert.Contains("AMOSTRAS.INCLUIR", await post.HtmlAsync());
        }
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(semNada.Login);
            var r = await web.GetAsync("/teste-permissao/leitura");
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
            Assert.Contains("Acesso negado", await r.HtmlAsync());

            var ajax = new HttpRequestMessage(HttpMethod.Get, "/teste-permissao/leitura");
            ajax.Headers.Add("X-Requested-With", "XMLHttpRequest");
            var j = await web.Http.SendAsync(ajax);
            Assert.Equal(HttpStatusCode.Forbidden, j.StatusCode);
            Assert.Contains("\"erro\"", await j.Content.ReadAsStringAsync());
        }

        // ADMIN_GLOBAL passa sempre
        var admin = await UsuariosTeste.CriarAsync(adminGlobal: true);
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(admin.Login);
            await web.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", c.Empresa.IdEmpresas.ToString()));
            Assert.Equal("ok-inclusao", await (await web.PostarAsync("/", "/teste-permissao/inclusao")).Content.ReadAsStringAsync());
        }

        // anônimo: login (não 403)
        using var anon = app.Cliente();
        Assert.StartsWith("https://localhost/Conta/Login", (await anon.GetAsync("/teste-permissao/leitura")).Destino());
    }

    [Fact]
    public void Menu_mostra_so_o_permitido_e_so_telas_existentes()
    {
        var usuario = new EmpresaFixa { EmpresaId = 1 };
        usuario.Permissoes.Add("PROPOSTAS.CONSULTAR");
        usuario.Permissoes.Add("RELATORIOS.CONSULTAR");

        var tudoExiste = Menu.Visivel(usuario, (_, _) => true);
        Assert.Equal(["Principal", "Laboratório", "Relatórios"], tudoExiste.Select(s => s.Titulo));
        Assert.Equal(["Propostas"], tudoExiste.Single(s => s.Titulo == "Laboratório").Itens.Select(i => i.Texto));
        Assert.Equal(6, tudoExiste.Single(s => s.Titulo == "Relatórios").Itens.Count);

        // tela ainda não reescrita não aparece (sem link quebrado); seção vazia some
        var semPropostas = Menu.Visivel(usuario, (ctrl, _) => ctrl != "Propostas");
        Assert.DoesNotContain(semPropostas, s => s.Titulo == "Laboratório");

        // Migração: só ADMIN_GLOBAL; ADMIN_GLOBAL vê o catálogo inteiro
        Assert.DoesNotContain(tudoExiste.SelectMany(s => s.Itens), i => i.Controller == "Migracao");
        var admin = Menu.Visivel(new EmpresaFixa { EmpresaId = 1, AdminGlobal = true }, (_, _) => true);
        Assert.Equal(Menu.Catalogo.Sum(s => s.Itens.Count), admin.Sum(s => s.Itens.Count));

        // todo item exige uma permissão do catálogo do 05_seed (ou é Início/Migração)
        Assert.All(Menu.Catalogo.SelectMany(s => s.Itens).Where(i => i.Permissao is not null),
                   i => Assert.Matches(@"^[A-Z_]+\.[A-Z_]+$", i.Permissao!));
    }

    [Fact]
    public void Menu_real_sem_tela_inexistente_e_com_uma_entrada_de_empresa()
    {
        var telas = app.Services.GetRequiredService<TelasExistentes>();
        var admin = Menu.Visivel(new EmpresaFixa { EmpresaId = 1, AdminGlobal = true }, telas.Existe).SelectMany(s => s.Itens).ToList();
        // Cidades (T11-03) é da 2.3: o JSON do combo não pode se passar pela tela
        Assert.DoesNotContain(admin, i => i.Texto == "Cidades");
        // empresa usuária: um item só, com nome explícito (T03)
        Assert.Single(admin, i => i.Controller is "Empresa" or "Empresas" && i.Action is "Index" or "Dados");
        Assert.Contains(admin, i => i.Texto == "Empresas Usuárias do Sistema");
    }

    [Fact]
    public async Task Permissoes_do_menu_existem_no_catalogo()
    {
        await using var c = await Cenario.CriarAsync();
        var codigos = Menu.Catalogo.SelectMany(s => s.Itens).Select(i => i.Permissao).OfType<string>().Distinct().ToList();
        var existentes = c.Db.Permissoes.Where(p => codigos.Contains(p.Codigo)).Select(p => p.Codigo).ToList();
        Assert.Empty(codigos.Except(existentes));
    }

    [Fact]
    public void Tag_helper_esconde_sem_permissao()
    {
        var usuario = new EmpresaFixa { EmpresaId = 1 };
        usuario.Permissoes.Add("AMOSTRAS.INCLUIR");

        static (TagHelperContext, TagHelperOutput) Novo(string tag) => (
            new TagHelperContext([], new Dictionary<object, object>(), "id"),
            new TagHelperOutput(tag, [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())));

        var (ctx1, sem) = Novo("a");
        new PermissaoTagHelper(usuario) { NoElemento = "AMOSTRAS.EXCLUIR" }.Process(ctx1, sem);
        Assert.Null(sem.TagName); // SuppressOutput

        var (ctx2, com) = Novo("a");
        new PermissaoTagHelper(usuario) { NoElemento = "AMOSTRAS.INCLUIR" }.Process(ctx2, com);
        Assert.Equal("a", com.TagName);

        var (ctx3, bloco) = Novo("permissao");
        new PermissaoTagHelper(usuario) { Codigo = "AMOSTRAS.INCLUIR" }.Process(ctx3, bloco);
        Assert.Null(bloco.TagName); // só o conteúdo, sem o elemento <permissao>
        Assert.False(bloco.Content.IsModified);
    }

    private sealed class SemTelas : IActionDescriptorCollectionProvider
    {
        public ActionDescriptorCollection ActionDescriptors { get; } = new([], 0);
    }

    [Fact]
    public async Task Painel_conta_so_a_empresa_ativa_e_respeita_as_permissoes()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var amo = c.NovaAmostra(hoje);
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso); // 2 testes sem resultado
        await c.AmostraComBoletimAsync(hoje);                                                      // boletim não emitido
        await using var outra = await Cenario.CriarAsync();                                         // não pode somar
        var amoOutra = outra.NovaAmostra(hoje);
        outra.Db.Add(amoOutra);
        await outra.Db.SaveChangesAsync();

        var painel = new PainelService(c.Db, c.Ativa, new TelasExistentes(new SemTelas()));
        Assert.Empty(await painel.ContadoresAsync()); // sem permissão nenhuma

        foreach (var p in new[] { "AMOSTRAS.CONSULTAR", "RESULTADOS.CONSULTAR", "BOLETINS.CONSULTAR", "PROPOSTAS.CONSULTAR" })
            c.Ativa.Permissoes.Add(p);
        var cont = (await painel.ContadoresAsync()).ToDictionary(x => x.Titulo);
        Assert.Equal(2, cont["Amostras recebidas hoje"].Valor);
        Assert.Equal("2 no mês", cont["Amostras recebidas hoje"].Detalhe);
        Assert.Equal(2, cont["Testes pendentes"].Valor);
        Assert.Equal(1, cont["Boletins não emitidos"].Valor);
        Assert.Equal(0, cont["Propostas aguardando cliente"].Valor);
        Assert.All(cont.Values, x => Assert.Null(x.Href)); // telas ainda não existem: sem link
    }

    [Fact]
    public async Task Home_mostra_contadores_conforme_o_perfil()
    {
        await using var c = await Cenario.CriarAsync();
        var com = await UsuariosTeste.ComPermissoesAsync(c, "AMOSTRAS.CONSULTAR");
        var sem = await UsuariosTeste.ComPermissoesAsync(c);

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(com.Login);
            var html = await (await web.GetAsync("/")).HtmlAsync();
            Assert.Contains("Amostras recebidas hoje", html);
            Assert.DoesNotContain("Testes pendentes", html);
            Assert.Contains(Versao.Atual, html);
        }
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(sem.Login);
            Assert.Contains("Nenhum indicador disponível", await (await web.GetAsync("/")).HtmlAsync());
        }
    }
}
