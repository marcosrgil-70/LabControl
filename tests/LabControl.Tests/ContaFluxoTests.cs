using System.Net;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.1 pelo app inteiro: cookie, antiforgery, troca de senha, escolha/troca de empresa e saída.</summary>
public class ContaFluxoTests(AppWeb app) : IClassFixture<AppWeb>
{
    [Fact]
    public async Task Anonimo_vai_para_o_login_e_post_sem_antiforgery_e_recusado()
    {
        using var c = app.Cliente();
        var r = await c.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.StartsWith("https://localhost/Conta/Login", r.Destino());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Conta/Login")).StatusCode);

        var sem = await c.Http.PostAsync("/Conta/Login", new FormUrlEncodedContent([new("Login", "X"), new("Senha", "Y")]));
        Assert.Equal(HttpStatusCode.BadRequest, sem.StatusCode);
    }

    [Fact]
    public async Task Senha_errada_mostra_a_mensagem_unica()
    {
        var usu = await UsuariosTeste.CriarAsync();
        using var c = app.Cliente();
        var r = await c.EntrarAsync(usu.Login, "errada");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Usuário ou senha inválidos.", await r.HtmlAsync());
    }

    [Fact]
    public async Task Troca_obrigatoria_antes_de_tudo_e_com_uma_empresa_entra_direto()
    {
        await using var e = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync(trocarSenha: true);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, e.Empresa.IdEmpresas, e.Ativa.ProfissionalId);
        using var c = app.Cliente();

        Assert.Equal("/Conta/TrocarSenha", (await c.EntrarAsync(usu.Login)).Destino());
        Assert.Equal("/Conta/TrocarSenha", (await c.GetAsync("/")).Destino());             // nenhuma outra tela
        Assert.Equal("/Conta/TrocarSenha", (await c.GetAsync("/Conta/Empresa")).Destino());

        var troca = await c.PostarAsync("/Conta/TrocarSenha", "/Conta/TrocarSenha",
            ("SenhaAtual", UsuariosTeste.Senha), ("NovaSenha", "NovaSenha1"), ("Confirmacao", "NovaSenha1"));
        Assert.Equal("/", troca.Destino()); // uma empresa só: entra direto

        var home = await c.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        var html = await home.HtmlAsync();
        Assert.Contains(e.Empresa.CodEmpresa, html);
        Assert.DoesNotContain("sem profissional vinculado", html);
    }

    [Fact]
    public async Task Com_duas_empresas_escolhe_troca_e_sai()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, a.Empresa.IdEmpresas, a.Ativa.ProfissionalId);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, b.Empresa.IdEmpresas); // sem profissional em B
        using var c = app.Cliente();

        Assert.Equal("/Conta/Empresa", (await c.EntrarAsync(usu.Login)).Destino());
        Assert.Equal("/Conta/Empresa", (await c.GetAsync("/")).Destino()); // sem empresa: só a escolha

        Assert.Equal("/", (await c.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", a.Empresa.IdEmpresas.ToString()))).Destino());
        var emA = await (await c.GetAsync("/")).HtmlAsync();
        Assert.Contains($"<strong>{a.Empresa.CodEmpresa}</strong>", emA);

        // trocar de empresa sem pedir a senha: o topo (e o filtro) passam a ser os de B
        Assert.Equal("/", (await c.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", b.Empresa.IdEmpresas.ToString()))).Destino());
        var emB = await (await c.GetAsync("/")).HtmlAsync();
        Assert.Contains($"<strong>{b.Empresa.CodEmpresa}</strong>", emB);
        Assert.Contains("sem profissional vinculado", emB);

        // empresa sem vínculo: recusada
        await using var outra = await Cenario.CriarAsync();
        var recusa = await c.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", outra.Empresa.IdEmpresas.ToString()));
        Assert.Contains("Você não tem acesso a esta empresa.", await recusa.HtmlAsync());

        Assert.StartsWith("/Conta/Login", (await c.PostarAsync("/", "/Conta/Sair")).Destino());
        Assert.StartsWith("https://localhost/Conta/Login", (await c.GetAsync("/")).Destino());
    }

    [Fact]
    public async Task Usuario_inativado_perde_a_sessao_na_hora()
    {
        await using var e = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, e.Empresa.IdEmpresas);
        using var c = app.Cliente();
        Assert.Equal("/", (await c.EntrarAsync(usu.Login)).Destino());
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/")).StatusCode);

        await e.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE USUARIOS SET ATIVO = 0 WHERE ID_USUARIOS = {usu.IdUsuarios}");
        Assert.StartsWith("https://localhost/Conta/Login", (await c.GetAsync("/")).Destino());
    }

    [Fact]
    public async Task Admin_global_retirado_perde_a_sessao_e_nao_se_promove_de_novo()
    {
        // revisão de segurança da 1.7: a claim não pode sobreviver à retirada do ADMIN_GLOBAL no banco
        await using var e = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync(adminGlobal: true);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, e.Empresa.IdEmpresas); // continua com acesso à empresa
        using var c = app.Cliente();
        await c.EntrarAsync(usu.Login);
        await c.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", e.Empresa.IdEmpresas.ToString()));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/Empresas")).StatusCode);

        await e.Db.Database.ExecuteSqlInterpolatedAsync($"UPDATE USUARIOS SET ADMIN_GLOBAL = 0 WHERE ID_USUARIOS = {usu.IdUsuarios}");
        Assert.StartsWith("https://localhost/Conta/Login", (await c.GetAsync("/Empresas")).Destino());
        var promover = await c.PostarAsync($"/Usuarios/Editar/{usu.IdUsuarios}", $"/Usuarios/Editar/{usu.IdUsuarios}",
            ("Login", usu.Login), ("Nome", usu.Nome), ("Ativo", "true"), ("AdminGlobal", "true"));
        Assert.NotEqual(HttpStatusCode.OK, promover.StatusCode);
        Assert.False(await e.Db.Usuarios.AsNoTracking().Where(u => u.IdUsuarios == usu.IdUsuarios).Select(u => u.AdminGlobal).SingleAsync());
    }

    [Fact]
    public async Task Usuario_sem_empresa_nao_entra()
    {
        var usu = await UsuariosTeste.CriarAsync();
        using var c = app.Cliente();
        var r = await c.EntrarAsync(usu.Login);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("não tem acesso a nenhuma empresa ativa", await r.HtmlAsync());
        Assert.StartsWith("https://localhost/Conta/Login", (await c.GetAsync("/")).Destino());
    }
}
