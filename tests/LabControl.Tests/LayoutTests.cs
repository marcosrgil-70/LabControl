using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LabControl.Tests;

/// <summary>Aceite da 0.8: o app sobe só com o código v2 e o layout não aponta para telas da v1 (Legacy/).</summary>
public partial class LayoutTests
{
    private static WebApplicationFactory<Program> App() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:LabControl", BancoTeste.ConnectionString));

    [GeneratedRegex("""(?:href|src)="(/[^"]*)""")]
    private static partial Regex Links();

    [Fact]
    public async Task Home_abre_e_todos_os_links_do_layout_existem()
    {
        await using var app = App();
        using var http = app.CreateClient();

        var home = await http.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("LabControl", html);

        var links = Links().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).Distinct().ToList();
        Assert.Contains("/", links);
        foreach (var link in links)
        {
            var r = await http.GetAsync(link);
            Assert.True(r.IsSuccessStatusCode, $"{link} → {(int)r.StatusCode}");
        }
    }

    [Fact]
    public async Task Pagina_de_erro_abre_em_portugues()
    {
        await using var app = App();
        using var http = app.CreateClient();
        var r = await http.GetAsync("/Home/Error");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Ocorreu um erro", await r.Content.ReadAsStringAsync());
    }
}
