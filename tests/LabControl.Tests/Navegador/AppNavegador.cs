using LabControl.Data;
using LabControl.Services.Geografia;
using LabControl.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace LabControl.Tests.Navegador;

/// <summary>
/// Testes de ponta a ponta no navegador (pedido do usuário, 20/10/2026: "teste se está realmente fazendo um CRUD"):
/// o app real no Kestrel (porta livre, banco <c>labcontrol_teste</c>) + Edge sem janela (Playwright, canal msedge —
/// usa o Edge instalado, sem baixar navegador). Exercita o que os testes HTTP não pegam: JavaScript das telas
/// (modais, abas, combos de cidade, "principal", confirmações), cliques e mensagens como o usuário vê.
/// </summary>
public sealed class AppNavegador : IAsyncLifetime
{
    private WebApplicationFactory<Program> _app = null!;
    private IPlaywright _playwright = null!;
    public IBrowser Browser { get; private set; } = null!;
    public string Url { get; private set; } = "";

    public async Task InitializeAsync()
    {
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:LabControl", BancoTeste.ConnectionString);
            // CEP sem internet: a fonte fixa responde como o ViaCEP para 01310-100 (o resto da cadeia é a real)
            b.ConfigureServices(s => s.AddSingleton<IFonteCep>(new CepDoTeste()));
        });
        _app.UseKestrel(0);
        _app.StartServer();
        var endereco = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        // "localhost" é contexto seguro: o navegador aceita o cookie Secure em http
        Url = new UriBuilder(endereco) { Host = "localhost" }.Uri.ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright.Dispose();
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Usuário com o perfil ADMINISTRADOR da empresa do cenário (todas as permissões), logado numa aba nova.
    /// Confirmações (<c>confirm()</c>) são aceitas automaticamente; erros de JavaScript ficam em <see cref="Sessao.ErrosJs"/>.
    /// <paramref name="comProfissional"/>: vinculado ao profissional do cenário (a proposta registra quem digitou).
    /// </summary>
    public async Task<Sessao> EntrarAsync(Cenario c, bool adminGlobal = false, bool comProfissional = false)
    {
        var usu = await UsuariosTeste.CriarAsync(adminGlobal: adminGlobal);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, c.Empresa.IdEmpresas, comProfissional ? c.Ativa.ProfissionalId : null, padrao: true);
        await using (var adm = await UsuariosTeste.ContextoAdminAsync())
        {
            var perfil = await adm.Perfis.IgnoreQueryFilters()
                .Where(p => p.IdEmpresas == c.Empresa.IdEmpresas && p.Nome == "ADMINISTRADOR").Select(p => p.IdPerfis).SingleAsync();
            adm.Add(new UsuarioPerfil { IdUsuarios = usu.IdUsuarios, IdEmpresas = c.Empresa.IdEmpresas, IdPerfis = perfil });
            await adm.SaveChangesAsync();
        }

        var contexto = await Browser.NewContextAsync(new() { Locale = "pt-BR", ViewportSize = new() { Width = 1400, Height = 900 } });
        var pagina = await contexto.NewPageAsync();
        var sessao = new Sessao(contexto, pagina, Url);
        pagina.Dialog += async (_, d) => { sessao.Dialogos.Add(d.Message); await d.AcceptAsync(); };
        pagina.PageError += (_, e) => sessao.ErrosJs.Add(e);
        // 422 = recusa de negócio esperada (o navegador avisa no console, mas a tela mostra a mensagem)
        pagina.Console += (_, m) => { if (m.Type == "error" && !m.Text.Contains("422")) sessao.ErrosJs.Add(m.Text); };

        await pagina.GotoAsync(Url + "/Conta/Login");
        await pagina.FillAsync("[name=Login]", usu.Login);
        await pagina.FillAsync("[name=Senha]", UsuariosTeste.Senha);
        await pagina.ClickAsync("button[type=submit]");
        if (adminGlobal && pagina.Url.Contains("/Conta/Empresa"))
        {
            await pagina.CheckAsync($"input[name=IdEmpresa][value='{c.Empresa.IdEmpresas}']");
            await pagina.ClickAsync("button[type=submit]");
        }
        await pagina.WaitForURLAsync(u => !u.Contains("/Conta/"));
        return sessao;
    }
}

/// <summary>Uma aba logada. Descartar fecha o contexto do navegador.</summary>
public sealed class Sessao(IBrowserContext contexto, IPage pagina, string url) : IAsyncDisposable
{
    public IPage Pagina { get; } = pagina;
    public string Url { get; } = url;
    public List<string> Dialogos { get; } = [];
    public List<string> ErrosJs { get; } = [];

    public Task IrAsync(string caminho) => Pagina.GotoAsync(Url + caminho);

    /// <summary>Mensagem de sucesso/erro do topo (TempData) — falha o teste se não aparecer.</summary>
    public async Task<string> AlertaAsync()
    {
        var alerta = Pagina.Locator(".alert-success, .alert-danger").First;
        await alerta.WaitForAsync(new() { Timeout = 10_000 });
        return (await alerta.InnerTextAsync()).Trim();
    }

    public ValueTask DisposeAsync() => contexto.DisposeAsync();
}

/// <summary>Fonte de CEP dos testes de navegador.</summary>
public sealed class CepDoTeste : IFonteCep
{
    public Task<CepBruto?> ConsultarAsync(string cep, CancellationToken ct) => Task.FromResult(cep == "01310100"
        ? new CepBruto("Avenida Paulista", "de 612 a 1510 - lado par", "Bela Vista", "3550308", "SP")
        : null);
}

[CollectionDefinition("Navegador")]
public sealed class ColecaoNavegador : ICollectionFixture<AppNavegador>;
