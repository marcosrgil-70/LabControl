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

    [Fact]
    public async Task Auxiliares_codigo_travado_cor_e_status_que_permite_resultado()
    {
        await using var c = await Cenario.CriarAsync();
        c.Db.Add(c.NovaAmostra(await c.HojeAsync())); // o tipo de amostra do cenário passa a estar em uso
        await c.Db.SaveChangesAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;

        // T10-14: código em uso vem travado, a descrição continua editável
        await s.IrAsync("/Auxiliares/AMOSTRAS_TIPO");
        await Linha(p, c.TipoAmostra.Descricao).Locator("button[title=Alterar]").ClickAsync();
        await Assertions.Expect(p.Locator("#campo_Codigo")).ToBeDisabledAsync();
        await Assertions.Expect(p.Locator("#formAuxiliar")).ToContainTextAsync("Em uso: não pode ser alterado.");
        await p.FillAsync("#campo_Descricao", c.TipoAmostra.Descricao + " E BEBIDAS");
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        Assert.Equal("Tipo de amostra gravado.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, c.TipoAmostra.Descricao + " E BEBIDAS")).ToContainTextAsync(c.TipoAmostra.Codigo.ToString());

        // T10-15: cor na lista; o único status que permite resultado não pode ser inativado
        var nome = Unico("EM QUARENTENA");
        await s.IrAsync("/Auxiliares/AMOSTRAS_STATUS");
        await p.ClickAsync("button[data-form$='/Incluir']");
        await p.FillAsync("#campo_Descricao", nome);
        await p.Locator("#campo_Cor").FillAsync("#ff8800");
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        Assert.Equal("Status da amostra gravado.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("#FF8800");
        await Linha(p, "LIBERADA PARA ANÁLISE").Locator("button[title=Inativar]").ClickAsync();
        // (a página anterior ainda mostra o "gravado": espera o alerta de erro da nova)
        await Assertions.Expect(p.Locator(".alert-danger")).ToContainTextAsync("Deve existir ao menos um status ativo que permite resultado");
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Geografia_pais_estado_cidade_e_importacao_ibge()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c, adminGlobal: true);
        var p = s.Pagina;
        var pais = Unico("NÁRNIA");
        var codigo = "97" + Random.Shared.Next(10000, 99999);
        var novoIbge = "35" + Random.Shared.Next(90000, 99999);
        try
        {
            // país
            await s.IrAsync("/Geografia/Paises");
            await p.ClickAsync("button[data-editar='0']");
            await p.FillAsync("#geo_Nome", pais);
            await p.FillAsync("#geo_Sigla", "na");
            await p.ClickAsync("#formGeo button[type=submit]");
            Assert.Equal("País gravado.", await s.AlertaAsync());
            await Assertions.Expect(Linha(p, pais)).ToContainTextAsync("NA");

            // estado no país (combo), pela aba Estados
            await p.ClickAsync(".nav-tabs >> text=Estados");
            await p.ClickAsync("button[data-editar='0']");
            await p.SelectOptionAsync("#geo_IdPai", new SelectOptionValue { Label = pais });
            await p.FillAsync("#geo_Nome", "CAIR PARAVEL");
            await p.FillAsync("#geo_Sigla", "CP");
            await p.FillAsync("#geo_Codigo", "97");
            await p.ClickAsync("#formGeo button[type=submit]");
            Assert.Equal("Estado gravado.", await s.AlertaAsync());
            await Assertions.Expect(Linha(p, "CAIR PARAVEL")).ToContainTextAsync(pais);

            // cidade: código fora da UF volta com o modal aberto e o erro no campo; depois grava, altera e exclui
            await p.ClickAsync(".nav-tabs >> text=Cidades");
            await p.SelectOptionAsync("#IdPais", new SelectOptionValue { Label = pais });
            await p.ClickAsync("button[data-editar='0']");
            await p.SelectOptionAsync("#geo_IdPai", new SelectOptionValue { Label = "CP — CAIR PARAVEL" });
            await p.FillAsync("#geo_Nome", "CASTELO");
            await p.FillAsync("#geo_Codigo", "3550308");
            await p.ClickAsync("#formGeo button[type=submit]");
            await Assertions.Expect(p.Locator("#formGeo .invalid-feedback")).ToHaveTextAsync("Código IBGE: as cidades de CP começam por 97.");
            await p.FillAsync("#geo_Codigo", codigo);
            await p.ClickAsync("#formGeo button[type=submit]");
            Assert.Equal("Cidade gravada.", await s.AlertaAsync());
            await Linha(p, "CASTELO").Locator("button[title=Alterar]").ClickAsync();
            await Assertions.Expect(p.Locator("#geo_Codigo")).ToHaveValueAsync(codigo);
            await p.FillAsync("#geo_Nome", "CASTELO DE CAIR PARAVEL");
            await p.ClickAsync("#formGeo button[type=submit]");
            await Assertions.Expect(Linha(p, "CASTELO DE CAIR PARAVEL")).ToContainTextAsync("CP");
            await Linha(p, "CASTELO DE CAIR PARAVEL").Locator("button[title=Excluir]").ClickAsync();
            Assert.Equal("Cidade excluída.", await s.AlertaAsync());
            Assert.Contains(s.Dialogos, d => d.Contains("some para todas as empresas"));

            // importação IBGE: relatório com incluídos e erros
            await s.IrAsync("/Geografia/Cidades");
            await p.ClickAsync("text=Importar municípios IBGE");
            await p.SetInputFilesAsync("#arquivoIbge", new FilePayload
            {
                Name = "municipios.csv", MimeType = "text/csv",
                Buffer = System.Text.Encoding.UTF8.GetBytes($"codigo;nome;uf\n{novoIbge};Importada Pelo Navegador;SP\n3304557;RIO DE JANEIRO;SP\n"),
            });
            await p.ClickAsync("#formImportar button[type=submit]");
            await Assertions.Expect(p.Locator("#relatorioImportacao")).ToContainTextAsync("1 incluído(s)");
            await Assertions.Expect(p.Locator("#relatorioImportacao")).ToContainTextAsync("UF SP não confere com o código 3304557 (RJ).");
            Assert.True(await c.Db.Cidades.AnyAsync(x => x.CodigoIbge == novoIbge && x.Nome == "IMPORTADA PELO NAVEGADOR"));
            Assert.Empty(s.ErrosJs);
        }
        finally
        {
            await c.Db.Cidades.Where(x => x.CodigoIbge == novoIbge || x.CodigoIbge == codigo).ExecuteDeleteAsync();
            await c.Db.Estados.Where(x => x.Pais.Nome == pais).ExecuteDeleteAsync();
            await c.Db.Paises.Where(x => x.Nome == pais).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Cadastro_rapido_mais_inclui_e_seleciona_no_combo()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var cargo = Unico("ANALISTA");
        var tipo = Unico("COLETA");

        // T13: "+" do cargo → modal do auxiliar → gravado já vem escolhido no combo
        await s.IrAsync("/Profissionais/Criar");
        await p.ClickAsync("button[data-rapido-botao=CARGOS_FUNCIONARIOS]");
        await p.FillAsync(".modal-rapido #campo_Descricao", cargo);
        await Assertions.Expect(p.Locator(".modal-rapido #campo_Ativo")).ToHaveCountAsync(0); // sempre ativo
        await p.ClickAsync(".modal-rapido button[type=submit]");
        await Assertions.Expect(p.Locator(".modal-rapido")).ToBeHiddenAsync();
        await Assertions.Expect(p.Locator("#IdCargosFuncionarios option:checked")).ToHaveTextAsync(cargo);

        // duplicado: o erro aparece no campo e o modal continua aberto
        await p.ClickAsync("button[data-rapido-botao=CARGOS_FUNCIONARIOS]");
        await p.FillAsync(".modal-rapido #campo_Descricao", cargo);
        await p.ClickAsync(".modal-rapido button[type=submit]");
        await Assertions.Expect(p.Locator(".modal-rapido .invalid-feedback")).ToHaveTextAsync("Já existe um registro com esta descrição.");
        await Assertions.Expect(p.Locator(".modal-rapido[data-pronto]")).ToHaveCountAsync(1); // abertura terminada
        await p.Keyboard.PressAsync("Escape");
        await Assertions.Expect(p.Locator(".modal-rapido")).ToBeHiddenAsync();

        // de dentro do modal de endereço (contatos): abre por cima, o Esc fecha só o de cima, o tipo novo fica escolhido
        await s.IrAsync($"/Clientes/Editar/{c.Cliente.IdEntidades}");
        await p.ClickAsync("button[data-bs-target='#aba-enderecos']");
        await p.ClickAsync("[data-contato-novo=endereco]");
        await Assertions.Expect(p.Locator("#modal-endereco")).ToBeVisibleAsync();
        await p.ClickAsync("#modal-endereco button[data-rapido-botao=ENDERECOS_TIPOS]");
        await p.FillAsync("#modal-endereco .modal-rapido #campo_Descricao", tipo);
        await Assertions.Expect(p.Locator("#modal-endereco .modal-rapido[data-pronto]")).ToHaveCountAsync(1);
        await p.Keyboard.PressAsync("Escape");
        await Assertions.Expect(p.Locator("#modal-endereco .modal-rapido")).ToBeHiddenAsync();
        await Assertions.Expect(p.Locator("#modal-endereco")).ToBeVisibleAsync();
        await p.ClickAsync("#modal-endereco button[data-rapido-botao=ENDERECOS_TIPOS]");
        await p.FillAsync("#modal-endereco .modal-rapido #campo_Descricao", tipo);
        await p.ClickAsync("#modal-endereco .modal-rapido button[type=submit]");
        // Finalidade é obrigatória: o erro do servidor (422) aparece no campo, com o modal aberto
        await Assertions.Expect(p.Locator("#modal-endereco .modal-rapido .invalid-feedback")).ToHaveTextAsync("Informe finalidade.");
        await p.SelectOptionAsync("#modal-endereco .modal-rapido #campo_Finalidade", new SelectOptionValue { Label = "Entrega" });
        await p.ClickAsync("#modal-endereco .modal-rapido button[type=submit]");
        await Assertions.Expect(p.Locator("#modal-endereco .modal-rapido")).ToBeHiddenAsync();
        await Assertions.Expect(p.Locator("#end-tipo option:checked")).ToHaveTextAsync(tipo);
        await Assertions.Expect(p.Locator("#modal-endereco")).ToBeVisibleAsync();

        // e o endereço grava com o tipo novo
        await p.FillAsync("#end-logradouro", "RUA DO CADASTRO RÁPIDO, 10");
        await p.SelectOptionAsync("#end-uf", new SelectOptionValue { Label = "SP" });
        await Assertions.Expect(p.Locator("#end-cidade option", new() { HasText = "CAMPINAS" })).ToHaveCountAsync(1);
        await p.SelectOptionAsync("#end-cidade", new SelectOptionValue { Label = "CAMPINAS" });
        await p.ClickAsync("#modal-endereco button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync(tipo);
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Produto_crud_com_codigo_automatico_e_unidade_pelo_mais()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("REQUEIJÃO");
        var unidade = Unico("GRAMA");

        // incluir: código vazio (automático), unidade nova pelo "+"
        await s.IrAsync("/Produtos");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Descricao", nome.ToLowerInvariant());
        await p.ClickAsync("button[data-rapido-botao=UNIDADES]");
        await p.FillAsync(".modal-rapido #campo_Descricao", unidade);
        await p.FillAsync(".modal-rapido #campo_Sigla", "g" + unidade[^3..]);
        await p.ClickAsync(".modal-rapido button[type=submit]");
        await Assertions.Expect(p.Locator("#IdUnidades option:checked")).ToHaveTextAsync(unidade);
        await p.SelectOptionAsync("#IdEmbalagensTipos", new SelectOptionValue { Label = "SACO PLÁSTICO" });
        await p.FillAsync("#QtdeEmbalagem", "1,5");
        await p.ClickAsync("#formProduto button[type=submit]");
        Assert.Equal("Produto gravado.", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#Codigo")).ToHaveValueAsync("000002");

        // alterar
        await p.FillAsync("#Descricao", nome + " CREMOSO");
        await p.ClickAsync("#formProduto button[type=submit]");
        Assert.Equal("Produto gravado.", await s.AlertaAsync());

        // pesquisar, inativar (confirmação), mostrar inativos, reativar, excluir
        await s.IrAsync("/Produtos");
        await p.FillAsync("#Texto", "000002");
        await p.PressAsync("#Texto", "Enter");
        await Assertions.Expect(Linha(p, nome + " CREMOSO")).ToContainTextAsync("1,5");
        await Linha(p, nome).Locator("button[title=Inativar]").ClickAsync();
        Assert.StartsWith("Produto inativado", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Inativar"));
        await Assertions.Expect(Linha(p, nome)).ToHaveCountAsync(0);
        await p.CheckAsync("#Inativos");
        await p.ClickAsync("form.card button[type=submit]");
        await Linha(p, nome).Locator("button[title=Reativar]").ClickAsync();
        Assert.Equal("Produto reativado.", await s.AlertaAsync());
        await Linha(p, nome).Locator("button[title=Excluir]").ClickAsync();
        Assert.Equal("Produto excluído.", await s.AlertaAsync());
        Assert.False(await c.Db.Produtos.AnyAsync(x => x.Descricao.StartsWith(nome)));
        Assert.DoesNotContain(s.Dialogos, string.IsNullOrWhiteSpace);
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Condicao_de_pagamento_com_parcelas()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var id = c.Proposta.IdCondicoesPagtos;

        await s.IrAsync("/Auxiliares/CONDICOES_PAGTOS");
        await Linha(p, "À VISTA").Locator("a[title=Parcelas]").ClickAsync();
        await Assertions.Expect(p.Locator("h5")).ToContainTextAsync("Parcelas — 01 · À VISTA");

        // 3 parcelas divididas igualmente; o total mostra 100%
        for (var i = 0; i < 3; i++) await p.ClickAsync("#adicionar");
        var dias = p.Locator("#linhas .dias");
        await dias.Nth(0).FillAsync("0");
        await dias.Nth(1).FillAsync("30");
        await dias.Nth(2).FillAsync("60");
        await p.ClickAsync("#dividir");
        await Assertions.Expect(p.Locator("#total")).ToHaveTextAsync("100%");

        // remover uma: o total avisa e o servidor recusa
        await p.Locator("#linhas .remover").Nth(2).ClickAsync();
        await Assertions.Expect(p.Locator("#total")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));
        await p.ClickAsync("#formParcelas button[type=submit]");
        await Assertions.Expect(p.Locator(".alert-danger")).ToContainTextAsync("A soma das porcentagens deve ser 100%");

        await p.ClickAsync("#dividir");
        await p.ClickAsync("#formParcelas button[type=submit]");
        await Assertions.Expect(p.Locator(".alert-success")).ToContainTextAsync("Parcelas gravadas.");
        var gravadas = await c.Db.CondicoesPagtosParc.AsNoTracking().Where(x => x.IdCondicoesPagtos == id).OrderBy(x => x.NrParcela).ToListAsync();
        Assert.Equal([(1, 0, 50m), (2, 30, 50m)], gravadas.Select(x => (x.NrParcela, x.DiasVencimento, x.PorcVencimento)));
        Assert.Empty(s.ErrosJs);
    }
}
