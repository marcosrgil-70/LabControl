using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Telas T03 / T12 / T13 pelo app inteiro: abas de contatos (AJAX), permissões, geografia e auxiliares dos tipos (P25).</summary>
public class CadastrosTelasTests(AppWeb app) : IClassFixture<AppWeb>
{
    [Fact]
    public async Task Clientes_inclui_e_grava_contatos_pelas_abas()
    {
        await using var c = await Cenario.CriarAsync();
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "CLIENTES.CONSULTAR", "CLIENTES.INCLUIR", "CLIENTES.ALTERAR");
        using var web = app.Cliente();
        await web.EntrarAsync(editor.Login);

        Assert.Contains("Clientes / Fornecedores", await (await web.GetAsync("/Clientes")).HtmlAsync());
        var criar = await web.PostarAsync("/Clientes/Criar", "/Clientes/Criar",
            ("Categoria", "Juridica"), ("Nome", "cliente pela tela"), ("CpfCnpj", "11.444.777/0001-61"),
            ("TipoCliente", "true"), ("TipoCliente", "false"), ("TipoFornecedor", "false"));
        var destino = criar.Destino()!;
        Assert.Matches(@"^/Clientes/Editar/\d+#aba-enderecos$", destino);
        var id = int.Parse(destino.Split('/', '#')[3]);

        var tela = await (await web.GetAsync($"/Clientes/Editar/{id}")).HtmlAsync();
        Assert.Contains("id=\"aba-enderecos\"", tela);
        Assert.Contains("data-contato-novo=\"fone\"", tela);
        Assert.Contains("CLIENTE PELA TELA", tela);

        var tipoFone = await c.Db.FonesTipos.Where(t => t.Descricao == "CELULAR COMERCIAL").Select(t => t.IdFonesTipos).SingleAsync();
        var ok = await web.PostarAsync($"/Clientes/Editar/{id}", $"/Clientes/{id}/ContatoFone",
            ("Id", "0"), ("IdFonesTipos", tipoFone.ToString()), ("Ddd", "19"), ("Fone", "991234567"), ("Contato", "joão"), ("Principal", "true"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(JsonDocument.Parse(await ok.Content.ReadAsStringAsync()).RootElement.GetProperty("ok").GetBoolean());

        var erro = await web.PostarAsync($"/Clientes/Editar/{id}", $"/Clientes/{id}/ContatoFone", ("Id", "0"), ("IdFonesTipos", tipoFone.ToString()), ("Ddd", "19"), ("Fone", "12"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, erro.StatusCode);
        Assert.Contains("\"campo\":\"Fone\"", await erro.Content.ReadAsStringAsync());

        var abas = await (await web.GetAsync($"/Clientes/{id}/Contatos")).HtmlAsync();
        Assert.Contains("(19) 99123-4567", abas);
        Assert.Contains("principal", abas);
        Assert.Contains("JOÃO", abas);
    }

    [Fact]
    public async Task Sem_alterar_a_tela_fica_so_de_consulta()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "CLIENTES.CONSULTAR");
        using var web = app.Cliente();
        await web.EntrarAsync(leitor.Login);
        var tela = await (await web.GetAsync($"/Clientes/Editar/{c.Cliente.IdEntidades}")).HtmlAsync();
        Assert.DoesNotContain("data-contato-novo", tela);
        Assert.DoesNotContain("Gravar dados", tela);
        Assert.Contains("cliente@teste.com", tela);
        var r = await web.PostarAsync($"/Clientes/Editar/{c.Cliente.IdEntidades}", $"/Clientes/{c.Cliente.IdEntidades}/ContatoEmail", ("Email", "x@y.com"));
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Empresa_tem_abas_de_contatos_e_geografia_responde()
    {
        await using var c = await Cenario.CriarAsync();
        var admin = await UsuariosTeste.ComPermissoesAsync(c, "EMPRESAS.CONSULTAR", "EMPRESAS.ALTERAR");
        using var web = app.Cliente();
        await web.EntrarAsync(admin.Login);

        var tela = await (await web.GetAsync("/Empresa/Dados")).HtmlAsync();
        Assert.Contains("data-bs-target=\"#aba-telefones\"", tela);
        Assert.DoesNotContain("name=\"Logradouro\" class=\"form-control\"", tela.Split("modal-endereco")[0]); // campos soltos saíram
        var email = $"lab{Guid.NewGuid():N}"[..12] + "@empresa.com";
        var r = await web.PostarAsync("/Empresa/Dados", "/Empresa/ContatoEmail", ("Id", "0"), ("Email", email), ("EnviarLaudo", "true"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains(email, await (await web.GetAsync("/Empresa/Contatos")).HtmlAsync());

        var uf = await c.Db.Estados.Where(e => e.Sigla == "SP").Select(e => e.IdEstados).SingleAsync();
        var cidades = await (await web.GetAsync($"/Geografia/Cidades?idEstado={uf}")).Content.ReadAsStringAsync();
        Assert.Contains("CAMPINAS", cidades);
        Assert.True(JsonDocument.Parse(cidades).RootElement.GetArrayLength() >= 645); // 645 municípios de SP (IBGE)
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await web.GetAsync("/Geografia/Cep/123")).StatusCode);
    }

    [Fact]
    public async Task Profissionais_e_auxiliares_dos_tipos()
    {
        await using var c = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.ComPermissoesAsync(c, "PROFISSIONAIS.CONSULTAR", "PROFISSIONAIS.INCLUIR", "PROFISSIONAIS.ALTERAR", "AUXILIARES.CONSULTAR");
        using var web = app.Cliente();
        await web.EntrarAsync(usu.Login);

        Assert.Contains("Profissionais (funcionários)", await (await web.GetAsync("/Profissionais")).HtmlAsync());
        var criar = await web.PostarAsync("/Profissionais/Criar", "/Profissionais/Criar", ("Nome", "ana"), ("Sobrenome", "química"), ("ExecutaAnalise", "true"));
        Assert.Matches(@"^/Profissionais/Editar/\d+#aba-enderecos$", criar.Destino());

        var cartoes = await (await web.GetAsync("/Auxiliares")).HtmlAsync();
        foreach (var t in new[] { "Tipos de logradouro", "Tipos de endereço", "Tipos de telefone", "Tipos de e-mail", "Registros profissionais", "Cargos de funcionários" })
            Assert.Contains(t, cartoes);
        var tiposEndereco = await (await web.GetAsync("/Auxiliares/ENDERECOS_TIPOS")).HtmlAsync();
        Assert.Contains("Correspondência", tiposEndereco); // rótulo do enum com acento
    }
}
