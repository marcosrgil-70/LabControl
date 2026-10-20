using System.Net;
using System.Text.RegularExpressions;

namespace LabControl.Tests;

/// <summary>Aceite da 0.8: o app sobe só com o código v2 e o layout não aponta para telas da v1 (Legacy/).</summary>
public partial class LayoutTests(AppWeb app) : IClassFixture<AppWeb>
{
    [GeneratedRegex("""(?:href|src)="(/[^"]*)""")]
    private static partial Regex Links();

    [Fact]
    public async Task Home_abre_e_todos_os_links_do_layout_existem()
    {
        await using var e = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, e.Empresa.IdEmpresas, e.Ativa.ProfissionalId);
        using var c = app.Cliente();
        Assert.Equal("/", (await c.EntrarAsync(usu.Login)).Destino());

        var home = await c.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("LabControl", html);

        var links = Links().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).Distinct().ToList();
        Assert.Contains("/", links);
        Assert.Contains("/Conta/Empresa", links);
        foreach (var link in links)
        {
            var r = await c.GetAsync(link);
            Assert.True(r.IsSuccessStatusCode, $"{link} → {(int)r.StatusCode} {r.Destino()}");
        }
    }

    [Fact]
    public async Task Pagina_de_erro_abre_em_portugues_sem_login()
    {
        using var c = app.Cliente();
        var r = await c.GetAsync("/Home/Error");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Ocorreu um erro", await r.Content.ReadAsStringAsync());
    }
}
