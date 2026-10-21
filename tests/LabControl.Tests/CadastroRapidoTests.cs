namespace LabControl.Tests;

/// <summary>
/// Aceite da 2.4 (servidor): o "+" (<c>lc-rapido</c>) só aparece para quem tem AUXILIARES.INCLUIR. O fluxo no
/// navegador (inclui e já seleciona, inclusive de dentro do modal de contatos) está em <c>CrudNavegadorTelasTests</c>.
/// </summary>
public class CadastroRapidoTests(AppWeb app) : IClassFixture<AppWeb>
{
    [Fact]
    public async Task Botao_mais_so_com_permissao_de_incluir_auxiliar()
    {
        await using var c = await Cenario.CriarAsync();
        var sem = await UsuariosTeste.ComPermissoesAsync(c, "PROFISSIONAIS.CONSULTAR", "PROFISSIONAIS.INCLUIR");
        var com = await UsuariosTeste.ComPermissoesAsync(c, "PROFISSIONAIS.CONSULTAR", "PROFISSIONAIS.INCLUIR", "AUXILIARES.INCLUIR");

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(sem.Login);
            var html = await (await web.GetAsync("/Profissionais/Criar")).HtmlAsync();
            Assert.Contains("id=\"IdCargosFuncionarios\"", html);
            Assert.DoesNotContain("data-rapido", html);
        }
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(com.Login);
            var html = await (await web.GetAsync("/Profissionais/Criar")).HtmlAsync();
            Assert.Contains("data-rapido=\"CARGOS_FUNCIONARIOS\"", html);
            Assert.Contains("data-rapido=\"TIPOS_REG_PROFISSIONAL\"", html);
            Assert.Contains("data-rapido-nome=\"cargo\"", html);
        }
    }
}
