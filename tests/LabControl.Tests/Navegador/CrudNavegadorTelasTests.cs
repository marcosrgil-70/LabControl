using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace LabControl.Tests.Navegador;

/// <summary>CRUD pelo navegador das demais telas já feitas: T13, T03 (+ contatos), T05, T02, T04, T03-lista (ADMIN_GLOBAL) e um auxiliar com opções.</summary>
[Collection("Navegador")]
public class CrudNavegadorTelasTests(AppNavegador app)
{
    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static ILocator Linha(IPage p, string texto) => p.Locator("tbody tr", new() { HasText = texto });

    [Fact]
    public async Task Profissional_crud_com_registro_e_contatos()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("ANA");

        await s.IrAsync("/Profissionais");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Nome", nome);
        await p.FillAsync("#Sobrenome", "química");
        await p.CheckAsync("#AssinaBoletim");
        await p.ClickAsync("button:has-text('Gravar dados')");
        await Assertions.Expect(p.Locator("body")).ToContainTextAsync("Quem assina boletim precisa de tipo, nº e UF do registro profissional.");
        await p.SelectOptionAsync("#IdTiposRegProfissional", new SelectOptionValue { Label = "CRQ" });
        await p.FillAsync("#NrRegistroProfissional", "04123456");
        await p.SelectOptionAsync("#UfRegistro", "SP");
        await p.ClickAsync("button:has-text('Gravar dados')");
        Assert.Contains("Profissional gravado", await s.AlertaAsync());
        await p.WaitForURLAsync(u => u.Contains("/Profissionais/Editar/"));
        var id = int.Parse(p.Url.Split('/').Last().Split('#')[0]);

        // contato
        await p.ClickAsync("button[data-bs-target='#aba-emails']");
        await p.ClickAsync("[data-contato-novo=email]");
        await p.FillAsync("#email-email", "ana@lab.com");
        await p.CheckAsync("#email-principal");
        await p.ClickAsync("#modal-email button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-emails tbody")).ToContainTextAsync("ana@lab.com");

        // alterar
        await p.ClickAsync("button[data-bs-target='#aba-dados']");
        await p.FillAsync("#Sobrenome", "química sênior");
        await p.ClickAsync("button:has-text('Gravar dados')");
        Assert.Equal("Profissional gravado.", await s.AlertaAsync());

        // pesquisar e excluir
        await s.IrAsync("/Profissionais");
        await p.FillAsync("#Texto", nome);
        await p.PressAsync("#Texto", "Enter");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("CRQ 04123456-SP");
        await Linha(p, nome).Locator("button[title=Excluir]").ClickAsync();
        Assert.Equal("Profissional excluído.", await s.AlertaAsync());
        Assert.False(await c.Db.Entidades.AnyAsync(e => e.IdEntidades == id));
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Empresa_dados_contatos_e_parametros()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var fantasia = Unico("LAB");

        await s.IrAsync("/Empresa/Dados");
        await p.FillAsync("#NomeFantasia", fantasia);
        await p.ClickAsync("#aba-dados button[type=submit]");
        Assert.Equal("Dados da empresa usuária gravados.", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("body")).ToContainTextAsync(fantasia); // topo atualizado

        await p.ClickAsync("button[data-bs-target='#aba-telefones']");
        await p.ClickAsync("[data-contato-novo=fone]");
        await p.SelectOptionAsync("#fone-tipo", new SelectOptionValue { Label = "COMERCIAL" });
        await p.FillAsync("#fone-ddd", "19");
        await p.FillAsync("#fone-numero", "34221000");
        await p.ClickAsync("#modal-fone button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-telefones tbody")).ToContainTextAsync("(19) 3422-1000");
        await p.ReloadAsync();
        await p.ClickAsync("button[data-bs-target='#aba-telefones']");
        await Assertions.Expect(p.Locator("#aba-telefones tbody")).ToContainTextAsync("(19) 3422-1000"); // gravado de verdade

        // T05 parâmetros
        await s.IrAsync("/Empresa/Parametros");
        await p.FillAsync("#DiasRetroativosEntrega", "15");
        await p.ClickAsync("#parametros button[type=submit]");
        Assert.Equal("Parâmetros da empresa gravados.", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#DiasRetroativosEntrega")).ToHaveValueAsync("15");
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Usuario_crud_com_senha_provisoria()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var login = "U" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        await s.IrAsync("/Usuarios");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Login", login);
        await p.FillAsync("#Nome", "Usuário do teste");
        await p.Locator("input[name='Empresas[0].Perfis']").First.CheckAsync();
        await p.ClickAsync("form button[type=submit]:has-text('Gravar')");
        await Assertions.Expect(p.Locator("#senha-provisoria")).ToBeVisibleAsync();

        await s.IrAsync("/Usuarios");
        await p.FillAsync("#Texto", login);
        await p.PressAsync("#Texto", "Enter");
        await Linha(p, login).Locator("a[title=Alterar]").ClickAsync();
        await p.FillAsync("#Nome", "Usuário alterado");
        await p.ClickAsync("form button[type=submit]:has-text('Gravar')");
        Assert.Equal("Usuário gravado.", await s.AlertaAsync());

        await Linha(p, login).Locator("a[title=Alterar]").ClickAsync();
        await p.ClickAsync("button:has-text('Resetar senha')");
        await Assertions.Expect(p.Locator("#senha-provisoria")).ToBeVisibleAsync();

        await s.IrAsync("/Usuarios");
        await p.FillAsync("#Texto", login);
        await p.PressAsync("#Texto", "Enter");
        await Linha(p, login).Locator("a[title=Alterar]").ClickAsync();
        await p.ClickAsync("button:has-text('Excluir')");
        Assert.Equal("Usuário excluído.", await s.AlertaAsync());
        Assert.False(await c.Db.Usuarios.AnyAsync(u => u.Login == login));
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Perfil_crud_matriz_e_copia()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("PERFIL");

        await s.IrAsync("/Perfis");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Nome", nome);
        await p.CheckAsync("input[title='CLIENTES.INCLUIR']");
        await Assertions.Expect(p.Locator("input[title='CLIENTES.CONSULTAR']")).ToBeCheckedAsync(); // CONSULTAR automático
        await p.ClickAsync("form button[type=submit]:has-text('Gravar')");
        Assert.Equal("Perfil incluído.", await s.AlertaAsync());

        await Linha(p, nome).Locator("a[title=Alterar]").ClickAsync();
        await p.CheckAsync("input[title='PRODUTOS.ALTERAR']");
        await p.ClickAsync("form button[type=submit]:has-text('Gravar')");
        Assert.StartsWith("Perfil gravado.", await s.AlertaAsync());
        var permissoes = await c.Db.PerfisPermissoes.Where(x => x.Perfil.Nome == nome).Select(x => x.Permissao.Codigo).ToListAsync();
        Assert.Contains("PRODUTOS.ALTERAR", permissoes);
        Assert.Contains("PRODUTOS.CONSULTAR", permissoes);

        await Linha(p, nome).Locator("a[title=Copiar]").ClickAsync();
        await p.FillAsync("#Nome", nome + " COPIA");
        await p.ClickAsync("form[action*=Copiar] button[type=submit]");
        Assert.Equal("Perfil copiado.", await s.AlertaAsync());

        foreach (var n in new[] { nome + " COPIA", nome })
        {
            await Linha(p, n).Locator("button[title=Excluir]").ClickAsync();
            Assert.Equal("Perfil excluído.", await s.AlertaAsync());
        }
        Assert.False(await c.Db.Perfis.AnyAsync(x => x.Nome.StartsWith(nome)));
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Empresas_admin_global_cria_e_altera()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c, adminGlobal: true);
        var p = s.Pagina;
        string codigo;
        do codigo = "Z" + Guid.NewGuid().ToString("N")[..2].ToUpperInvariant();
        while (await c.Db.Empresas.AnyAsync(e => e.CodEmpresa == codigo));

        await s.IrAsync("/Empresas");
        await p.ClickAsync("a:has-text('Nova empresa usuária')");
        await p.FillAsync("#CodEmpresa", codigo);
        await p.FillAsync("#RazaoSocial", "EMPRESA PELO NAVEGADOR");
        await p.ClickAsync("form[action*=Empresas] button[type=submit]");
        Assert.StartsWith($"Empresa usuária {codigo} criada.", await s.AlertaAsync());
        await Linha(p, codigo).Locator("a[title=Alterar]").ClickAsync();
        await p.FillAsync("#NomeFantasia", "NAVEGADOR");
        await p.ClickAsync("form[action*=Empresas] button[type=submit]");
        Assert.Equal("Empresa usuária gravada.", await s.AlertaAsync());
        var e = await c.Db.Empresas.AsNoTracking().SingleAsync(x => x.CodEmpresa == codigo);
        Assert.Equal("NAVEGADOR", e.NomeFantasia);
        Assert.NotNull(e.IdEntidades); // já nasce com a entidade (contatos)
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Auxiliar_com_opcoes_tipos_de_endereco()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("COLETA");

        await s.IrAsync("/Auxiliares/ENDERECOS_TIPOS");
        await p.ClickAsync("button[data-form$='/Incluir']");
        await p.FillAsync("#campo_Descricao", nome);
        await p.SelectOptionAsync("#campo_Finalidade", new SelectOptionValue { Label = "Cobrança" });
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        Assert.Equal("Tipo de endereço gravado.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("Cobrança");

        await Linha(p, nome).Locator("button[title=Alterar]").ClickAsync();
        await Assertions.Expect(p.Locator("#campo_Finalidade option:checked")).ToHaveTextAsync("Cobrança");
        await p.SelectOptionAsync("#campo_Finalidade", new SelectOptionValue { Label = "Entrega" });
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("Entrega");
        Assert.Empty(s.ErrosJs);
    }
}
