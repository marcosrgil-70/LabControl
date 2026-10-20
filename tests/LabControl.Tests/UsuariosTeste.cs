using System.Net;
using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabControl.Tests;

/// <summary>Usuários com senha real (PBKDF2) e vínculos com empresas, para os testes de login (T00).</summary>
public static class UsuariosTeste
{
    public const string Senha = "Senha123";

    /// <summary>Contexto de ADMIN_GLOBAL (vínculos de usuário com qualquer empresa).</summary>
    public static async Task<LabDbContext> ContextoAdminAsync() =>
        await BancoTeste.NovoContextoAsync(new EmpresaFixa { AdminGlobal = true });

    public static async Task<Usuario> CriarAsync(bool trocarSenha = false, bool adminGlobal = false, string senha = Senha)
    {
        await using var db = await ContextoAdminAsync();
        var usu = new Usuario
        {
            Login = "T" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(), Nome = "USUÁRIO DE LOGIN",
            SenhaHash = ContaService.GerarHash(senha), TrocarSenha = trocarSenha, AdminGlobal = adminGlobal,
        };
        db.Add(usu);
        await db.SaveChangesAsync();
        return usu;
    }

    public static async Task VincularAsync(int idUsuario, int idEmpresa, int? profissional = null, bool padrao = false, bool ativo = true)
    {
        await using var db = await ContextoAdminAsync();
        db.Add(new UsuarioEmpresa { IdUsuarios = idUsuario, IdEmpresas = idEmpresa, IdEntidadesFunc = profissional, Padrao = padrao, Ativo = ativo });
        await db.SaveChangesAsync();
    }

    public static ContaService Servico(LabDbContext db, IEmpresaContexto ctx) => new(db, new ServicoAuditoria(db, ctx), TimeProvider.System);

    /// <summary>Usuário vinculado à empresa do cenário com um perfil novo contendo só <paramref name="codigos"/>.</summary>
    public static async Task<Usuario> ComPermissoesAsync(Cenario c, params string[] codigos)
    {
        var usu = await CriarAsync();
        await VincularAsync(usu.IdUsuarios, c.Empresa.IdEmpresas); // sem profissional: UQ_USUEMP_FUNC (RN-02.09) permite um usuário por profissional
        if (codigos.Length == 0) return usu;
        var perfil = new Perfil { Nome = "P" + Guid.NewGuid().ToString("N")[..10] };
        c.Db.Add(perfil);
        await c.Db.SaveChangesAsync();
        var lista = codigos.ToList(); // string[].Contains viraria MemoryExtensions (ReadOnlySpan) no C# 14, que o EF 9 não traduz
        var ids = await c.Db.Permissoes.Where(p => lista.Contains(p.Codigo)).Select(p => p.IdPermissoes).ToListAsync();
        Assert.Equal(codigos.Length, ids.Count);
        c.Db.AddRange(ids.Select(id => new PerfilPermissao { IdPerfis = perfil.IdPerfis, IdPermissoes = id }));
        await c.Db.SaveChangesAsync();
        await using var adm = await ContextoAdminAsync();
        adm.Add(new UsuarioPerfil { IdUsuarios = usu.IdUsuarios, IdEmpresas = c.Empresa.IdEmpresas, IdPerfis = perfil.IdPerfis });
        await adm.SaveChangesAsync();
        return usu;
    }
}

/// <summary>App web inteiro (WebApplicationFactory) apontando para o banco de testes.</summary>
public sealed class AppWeb : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:LabControl", BancoTeste.ConnectionString);
        // controllers só de teste (ex.: TestePermissaoController) entram no app de teste
        builder.ConfigureServices(s => s.AddControllersWithViews().AddApplicationPart(typeof(AppWeb).Assembly));
    }

    /// <summary>Cliente HTTPS com cookies e sem seguir redirecionamentos (os testes conferem o destino).</summary>
    public ClienteWeb Cliente() =>
        new(CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") }));
}

public sealed partial class ClienteWeb(HttpClient http) : IDisposable
{
    public HttpClient Http => http;

    [GeneratedRegex("""name="__RequestVerificationToken" type="hidden" value="([^"]+)""")]
    private static partial Regex Token();

    public Task<HttpResponseMessage> GetAsync(string url) => http.GetAsync(url);

    /// <summary>Abre o formulário em <paramref name="pagina"/>, lê o antiforgery e posta os campos.</summary>
    public async Task<HttpResponseMessage> PostarAsync(string pagina, string acao, params (string Campo, string Valor)[] campos)
    {
        var html = await (await http.GetAsync(pagina)).Content.ReadAsStringAsync();
        var token = Token().Match(html).Groups[1].Value;
        var dados = campos.Select(c => new KeyValuePair<string, string>(c.Campo, c.Valor))
            .Append(new("__RequestVerificationToken", WebUtility.HtmlDecode(token)));
        return await http.PostAsync(acao, new FormUrlEncodedContent(dados));
    }

    public Task<HttpResponseMessage> EntrarAsync(string login, string senha = UsuariosTeste.Senha) =>
        PostarAsync("/Conta/Login", "/Conta/Login", ("Login", login), ("Senha", senha));

    public void Dispose() => http.Dispose();
}

public static class RespostaExtensions
{
    public static string? Destino(this HttpResponseMessage r) => r.Headers.Location?.OriginalString;

    /// <summary>Conteúdo com as entidades HTML decodificadas (o Razor codifica acentos: "á" → "&amp;#xE1;").</summary>
    public static async Task<string> HtmlAsync(this HttpResponseMessage r) => WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
}
