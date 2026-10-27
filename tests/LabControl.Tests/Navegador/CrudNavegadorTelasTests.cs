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
        // volta para a lista com o código gerado (pedido do usuário: já pode incluir o próximo)
        Assert.Equal($"Produto 000002 — {nome} gravado.", await s.AlertaAsync());
        await p.WaitForURLAsync(u => u.EndsWith("/Produtos"));
        await Assertions.Expect(p.Locator("a:has-text('Incluir')")).ToBeVisibleAsync();

        // alterar
        await Linha(p, nome).Locator("a[title=Abrir]").ClickAsync();
        await Assertions.Expect(p.Locator("#Codigo")).ToHaveValueAsync("000002");
        await p.FillAsync("#Descricao", nome + " CREMOSO");
        await p.ClickAsync("#formProduto button[type=submit]");
        await Assertions.Expect(p.Locator(".alert-success")).ToContainTextAsync($"Produto 000002 — {nome} CREMOSO gravado.");

        // pesquisar, inativar (confirmação), mostrar inativos, reativar, excluir
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
    public async Task Parametro_de_analise_crud_com_metodo_pelo_mais()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("ESTAFILOCOCOS");
        var metodo = Unico("ISO 6888");

        // incluir: sem descrição reduzida volta com o erro; método novo pelo "+"
        await s.IrAsync("/ParametrosAnalise");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Descricao", nome);
        await p.SelectOptionAsync("#IdAnalisesTipo", new SelectOptionValue { Label = "01 — MICROBIOLÓGICA" });
        await p.ClickAsync("#formParametro button[type=submit]");
        await Assertions.Expect(p.Locator("body")).ToContainTextAsync("Informe a descrição reduzida");
        await p.FillAsync("#DescReduzida", "estafilococos");
        await p.ClickAsync("button[data-rapido-botao=ANALISES_METODOS]");
        await p.FillAsync(".modal-rapido #campo_Descricao", metodo);
        await p.ClickAsync(".modal-rapido button[type=submit]");
        await Assertions.Expect(p.Locator("#IdAnalisesMetodos option:checked")).ToHaveTextAsync(metodo);
        await p.FillAsync("#VrUnitario", "95,50");
        await p.CheckAsync("#Auditado");
        await p.ClickAsync("#formParametro button[type=submit]");
        Assert.Equal($"Parâmetro {nome} gravado.", await s.AlertaAsync());
        await p.WaitForURLAsync(u => u.EndsWith("/ParametrosAnalise"));

        // filtro acreditado + alterar + inativar/reativar + excluir
        await p.SelectOptionAsync("#Acreditado", "sim");
        await p.FillAsync("#Texto", nome);
        await p.PressAsync("#Texto", "Enter");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("95,50");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync(metodo);
        await Linha(p, nome).Locator("a[title=Abrir]").ClickAsync();
        await p.UncheckAsync("#Auditado");
        await p.ClickAsync("#formParametro button[type=submit]");
        await Assertions.Expect(p.Locator(".alert-success")).ToContainTextAsync($"Parâmetro {nome} gravado.");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("Não");
        await Linha(p, nome).Locator("button[title=Inativar]").ClickAsync();
        Assert.StartsWith("Parâmetro inativado", await s.AlertaAsync());
        await p.CheckAsync("#Inativos");
        await p.ClickAsync("form.card button[type=submit]");
        await Linha(p, nome).Locator("button[title=Reativar]").ClickAsync();
        Assert.Equal("Parâmetro reativado.", await s.AlertaAsync());
        await Linha(p, nome).Locator("button[title=Excluir]").ClickAsync();
        Assert.Equal("Parâmetro de análise excluído.", await s.AlertaAsync());
        Assert.False(await c.Db.LabParametrosAnalises.AnyAsync(x => x.Descricao == nome));
        Assert.DoesNotContain(s.Dialogos, string.IsNullOrWhiteSpace);
        Assert.Empty(s.ErrosJs);
    }

    [Fact]
    public async Task Migracao_tela_valida_o_pedido_sem_executar()
    {
        // a execução completa (minutos, Firebird real) é provada em MigracaoTests; aqui o fluxo da tela
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c, adminGlobal: true);
        var p = s.Pagina;
        await s.IrAsync("/");
        await p.ClickAsync("text=Migração de Dados");
        await Assertions.Expect(p.Locator("h5")).ToContainTextAsync("Migração de dados");
        await p.FillAsync("#Form_BancoDestino", BancoTeste.NomeBanco);
        await p.FillAsync("#Form_SenhaFirebird", "masterkey");
        await p.FillAsync("#Form_SenhaProvisoria", "Migracao#2026");
        await p.FillAsync("#Form_ConfirmarSenha", "Migracao#2026");
        await p.ClickAsync("#formMigracao button[type=submit]");
        Assert.Contains(s.Dialogos, d => d.StartsWith("Executar a migração"));
        await Assertions.Expect(p.Locator("[data-erros]")).ToContainTextAsync($"O banco {BancoTeste.NomeBanco} não está vazio");
        await Assertions.Expect(p.Locator("#Form_SenhaProvisoria")).ToHaveValueAsync("");
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

    /// <summary>T20 (4.2): cliente → sugestões de endereço/contato, produto e análises nos modais, valores do roteiro, alterar, pesquisar, excluir.</summary>
    [Fact]
    public async Task Proposta_crud_com_produto_analises_e_valores()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c, comProfissional: true);
        var p = s.Pagina;
        var aa = (await c.HojeAsync()).Year % 100;

        await s.IrAsync("/Propostas");
        await p.ClickAsync("a:has-text('Incluir')");
        await Assertions.Expect(p.Locator("#nrProposta")).ToHaveTextAsync("INCLUSÃO");
        // cliente: endereço do laudo, contato do resultado e cobrança (= o próprio cliente) sugeridos
        await p.SelectOptionAsync("#IdEntidades", new SelectOptionValue { Label = "CLIENTE TESTE" });
        await Assertions.Expect(p.Locator("#IdEntEndLaudo option:checked")).ToContainTextAsync("RUA TESTE, 1");
        await Assertions.Expect(p.Locator("#IdEntContatoResultado option:checked")).ToHaveTextAsync("cliente@teste.com");
        await Assertions.Expect(p.Locator("#IdEntCobranca option:checked")).ToHaveTextAsync("CLIENTE TESTE");
        await Assertions.Expect(p.Locator("#IdEntContatoNf option:checked")).ToHaveTextAsync("cliente@teste.com");
        await p.SelectOptionAsync("#IdCondicoesPagtos", new SelectOptionValue { Label = "01 — À VISTA" });
        await p.FillAsync("#PorcDesconto", "10");

        // sem análises: mensagem e a aba Análises aberta
        await p.ClickAsync("#formProposta button[type=submit]:has-text('Gravar')");
        await Assertions.Expect(p.Locator("[data-erro-itens]")).ToHaveTextAsync("Inclua ao menos um produto com uma análise.");
        await Assertions.Expect(p.Locator("#abaAnalises")).ToBeVisibleAsync();
        await Assertions.Expect(p.Locator("#IdEntidades option:checked")).ToHaveTextAsync("CLIENTE TESTE"); // o que foi digitado volta

        // produto (qtde 2) e as 2 análises do roteiro: 100,00 e 50,00 → 300,00 − 10% = 270,00
        await p.ClickAsync("[data-acao=incluir-produto]");
        await p.SelectOptionAsync("#produtoId", new SelectOptionValue { Label = "QUEIJO MINAS (000001)" });
        await p.FillAsync("#produtoQtde", "2");
        await p.ClickAsync("#formProduto button[type=submit]");
        await p.ClickAsync("[data-acao=incluir-analise]");
        await p.SelectOptionAsync("#analiseParametro", new SelectOptionValue { Label = "CONTAGEM TOTAL" });
        await p.SelectOptionAsync("#analiseMetodo", new SelectOptionValue { Label = "ISO 4833" });
        await p.SelectOptionAsync("#analiseIdioma", new SelectOptionValue { Index = 1 });
        await p.SelectOptionAsync("#analisePrazo", new SelectOptionValue { Label = "10 DIAS" });
        await p.FillAsync("#analiseUnitario", "100");
        await Assertions.Expect(p.Locator("#analiseTotal")).ToHaveTextAsync("200,00");
        await p.ClickAsync("#formAnalise button[type=submit]");
        // a 2ª análise já vem com método/idioma/prazo da anterior
        await p.ClickAsync("[data-acao=incluir-analise]");
        await Assertions.Expect(p.Locator("#analisePrazo option:checked")).ToHaveTextAsync("10 DIAS");
        await p.SelectOptionAsync("#analiseParametro", new SelectOptionValue { Label = "SALMONELLA SP" });
        await p.FillAsync("#analiseUnitario", "50");
        await p.ClickAsync("#formAnalise button[type=submit]");
        await Assertions.Expect(p.Locator("#tabelaAnalises tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(p.Locator("#totProposta")).ToHaveTextAsync("270,00");

        // observação (texto rico) e gravar: o número vem do banco
        await p.ClickAsync("button[data-bs-target='#abaTextos']");
        await p.Locator("[data-texto=TxObservacao]").FillAsync("Amostra refrigerada");
        await p.ClickAsync("#formProposta button[type=submit]:has-text('Gravar')");
        Assert.Equal($"Nr. da Proposta: 000002/{aa:00}-R0", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#nrProposta")).ToHaveTextAsync($"000002/{aa:00}-R0");
        await Assertions.Expect(p.Locator("#totProposta")).ToHaveTextAsync("270,00");
        var id = await c.Db.LabPropostas.Where(x => x.NrProposta.StartsWith("000002/")).Select(x => x.IdLabPropostas).SingleAsync();
        Assert.Contains("Amostra refrigerada", await c.Db.LabPropostasTextos.Where(t => t.IdLabPropostas == id).Select(t => t.TxObservacao).SingleAsync());

        // alterar: qtde 3 → 450,00 − 10% = 405,00 (gravado pelo banco)
        await p.ClickAsync("button[data-bs-target='#abaAnalises']");
        await p.ClickAsync("[data-acao=alterar-produto]");
        await p.FillAsync("#produtoQtde", "3");
        await p.ClickAsync("#formProduto button[type=submit]");
        await Assertions.Expect(p.Locator("#totProposta")).ToHaveTextAsync("405,00");
        await p.ClickAsync("#formProposta button[type=submit]:has-text('Gravar')");
        Assert.Equal($"Nr. da Proposta: 000002/{aa:00}-R0", await s.AlertaAsync());
        Assert.Equal(405m, await c.Db.LabPropostas.AsNoTracking().Where(x => x.IdLabPropostas == id).Select(x => x.VrTotalProposta).SingleAsync());

        // pesquisar pelo número e excluir (com confirmação)
        await s.IrAsync("/Propostas");
        await p.FillAsync("#Numero", $"2/{aa:00}");
        await p.ClickAsync("#filtroPropostas button[type=submit]");
        await Assertions.Expect(p.Locator("tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(p.Locator("tbody tr")).ToContainTextAsync("405,00");
        await p.ClickAsync("tbody tr a:has-text('Abrir')");
        await p.ClickAsync("button[form=formExcluir]");
        Assert.Equal("Proposta excluída.", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Excluir a proposta"));
        Assert.False(await c.Db.LabPropostas.AnyAsync(x => x.IdLabPropostas == id));
        Assert.Empty(s.ErrosJs);
    }

    /// <summary>T22 (4.3): cascata cliente → proposta → item → tipo, número do banco, testes, histórico, comprovante, data retroativa confirmada, pesquisa e exclusão.</summary>
    [Fact]
    public async Task Amostra_recebimento_testes_comprovante_e_exclusao()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c, comProfissional: true);
        var p = s.Pagina;
        var aa = (await c.HojeAsync()).Year % 100;

        await s.IrAsync("/Amostras");
        await p.ClickAsync("a:has-text('Incluir')");
        await Assertions.Expect(p.Locator("#nrAmostra")).ToHaveTextAsync("INCLUSÃO");
        // cascata: cliente → propostas; proposta → itens e pedido; item → tipos de análise (um só: já escolhido)
        await p.SelectOptionAsync("#IdEntidades", new SelectOptionValue { Label = "CLIENTE TESTE" });
        await Assertions.Expect(p.Locator("#IdLabPropostas option")).ToHaveCountAsync(2);
        await p.SelectOptionAsync("#IdLabPropostas", new SelectOptionValue { Index = 1 });
        await Assertions.Expect(p.Locator("#IdLabPropostasProdutos option")).ToHaveCountAsync(2);
        await p.SelectOptionAsync("#IdLabPropostasProdutos", new SelectOptionValue { Index = 1 });
        await Assertions.Expect(p.Locator("#IdAnalisesTipo option:checked")).ToHaveTextAsync("01 — MICROBIOLÓGICA");
        await p.SelectOptionAsync("#IdAmostrasTipo", new SelectOptionValue { Label = "04 — ALIMENTOS" });
        await Assertions.Expect(p.Locator("#nrAmostra")).ToHaveTextAsync($"04-?????-01/{aa:00} (INCLUSÃO)");
        await p.FillAsync("#LocalRecebimento", "RECEPÇÃO");
        await p.SelectOptionAsync("#IdEmbalagensTipos", new SelectOptionValue { Label = "SACO PLÁSTICO" });
        await p.FillAsync("#QtdeEmbalagens", "3");
        await p.SelectOptionAsync("#ValidadeMes", "5");
        await p.SelectOptionAsync("#ValidadeAno", "2027");
        await p.ClickAsync("#botaoGravar");
        Assert.Equal($"Código da Amostra: 04-00001-01/{aa:00}", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#nrAmostra")).ToHaveTextAsync($"04-00001-01/{aa:00}");
        var id = await c.Db.LabHistAmostras.Where(a => a.NrAmostra == $"04-00001-01/{aa:00}").Select(a => a.IdLabHistAmostras).SingleAsync();

        // testes da proposta e histórico
        await p.ClickAsync("button[data-bs-target='#abaTestes']");
        await Assertions.Expect(p.Locator("#tabelaTestes tbody tr")).ToHaveCountAsync(2);
        await p.ClickAsync("button[data-bs-target='#abaHistorico']");
        await Assertions.Expect(p.Locator("#tabelaHistorico tbody tr").First).ToContainTextAsync("Inclusão");

        // data de entrega de mais de um ano atrás: a tela pede confirmação; o número não muda
        await p.ClickAsync("button[data-bs-target='#abaGeral']");
        await p.FillAsync("#DtEntrega", (await c.HojeAsync()).AddDays(-400).ToString("yyyy-MM-dd"));
        await p.FillAsync("#DtHrColeta", "");
        await Assertions.Expect(p.Locator("#avisoData")).ToContainTextAsync("será pedida confirmação");
        await p.ClickAsync("#botaoGravar");
        Assert.Equal($"Código da Amostra: 04-00001-01/{aa:00}", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.Contains("Confirme que o ANO está correto."));

        // comprovante (página de impressão)
        await s.IrAsync($"/Amostras/Comprovante/{id}");
        await Assertions.Expect(p.Locator("#nrComprovante")).ToHaveTextAsync($"04-00001-01/{aa:00}");
        await Assertions.Expect(p.Locator("table tbody tr")).ToHaveCountAsync(2);

        // pesquisa pelo número (sem o "a partir de", a entrega agora é antiga) e exclusão com confirmação
        await s.IrAsync("/Amostras");
        await p.FillAsync("#APartirDe", "");
        await p.FillAsync("#Numero", "00001");
        await p.ClickAsync("#filtroAmostras button[type=submit]");
        await Assertions.Expect(p.Locator("tbody tr")).ToHaveCountAsync(1);
        await p.ClickAsync("tbody tr a:has-text('Abrir')");
        await p.ClickAsync("button[form=formExcluir]");
        Assert.Equal("Amostra excluída.", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Excluir a amostra"));
        Assert.False(await c.Db.LabHistAmostras.AnyAsync(a => a.IdLabHistAmostras == id));
        Assert.Empty(s.ErrosJs);
    }

    /// <summary>T29 (4.4): pesquisa avançada aberta pela T22, filtro por número parcial, seleção abre a amostra.</summary>
    [Fact]
    public async Task Pesquisa_de_amostras_seleciona_e_abre()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var amostra = c.NovaAmostra(hoje);
        amostra.NrLote = "LOTE-PESQ";
        c.Db.Add(amostra);
        await c.Db.SaveChangesAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;

        await s.IrAsync("/Amostras");
        await p.ClickAsync("button[data-pesquisa-amostras]");
        await Assertions.Expect(p.Locator("#modalPesquisaAmostras [data-pesquisa-grid] tbody tr")).ToHaveCountAsync(1);
        await p.FillAsync("#pesqLote", "nada");
        await p.ClickAsync("#modalPesquisaAmostras button[type=submit]");
        await Assertions.Expect(p.Locator("#modalPesquisaAmostras tbody")).ToContainTextAsync("Nenhuma amostra encontrada");
        await p.FillAsync("#pesqLote", "");
        await p.FillAsync("#pesqNumero", "00001");
        await p.ClickAsync("#modalPesquisaAmostras button[type=submit]");
        await Assertions.Expect(p.Locator("#modalPesquisaAmostras tbody tr")).ToContainTextAsync("LOTE-PESQ");
        await p.ClickAsync("#modalPesquisaAmostras [data-selecionar]");
        await p.WaitForURLAsync(u => u.EndsWith($"/Amostras/Editar/{amostra.IdLabHistAmostras}"));
        await Assertions.Expect(p.Locator("#nrAmostra")).ToHaveTextAsync(amostra.NrAmostra);
        Assert.Empty(s.ErrosJs);
    }

    /// <summary>
    /// T26 (5.2): fila de análises, "Novo resultado" pela pesquisa, "Lançar todos", numérico com prévia no formato do
    /// boletim, descritivo levando a especificação junto, gravação, prévia relida do banco e exclusão de um parâmetro.
    /// </summary>
    [Fact]
    public async Task Resultado_lanca_todos_com_previa_e_exclui_parametro()
    {
        await using var c = await Cenario.CriarAsync();
        var amostra = c.NovaAmostra(await c.HojeAsync());
        amostra.IdAmostrasStatus = await c.Db.AmostrasStatus.Where(st => st.PermiteResultado).Select(st => st.IdAmostrasStatus).FirstAsync();
        c.Db.Add(amostra);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(amostra.IdLabHistAmostras)).Sucesso);
        await using var s = await app.EntrarAsync(c, comProfissional: true);
        var p = s.Pagina;

        await s.IrAsync("/Resultados");
        await p.ClickAsync("#botaoFila");
        await Assertions.Expect(p.Locator("#tabelaFila tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(p.Locator("#tabelaFila tbody tr").First).ToContainTextAsync(amostra.NrAmostra);

        await p.ClickAsync("#botaoNovoResultado");
        await Assertions.Expect(p.Locator("#modalPesquisaAmostras [data-pesquisa-grid] tbody tr")).ToHaveCountAsync(1);
        await p.ClickAsync("#modalPesquisaAmostras [data-selecionar]");
        await p.WaitForURLAsync(u => u.EndsWith($"/Resultados/Amostra/{amostra.IdLabHistAmostras}"));
        await Assertions.Expect(p.Locator("#nrAmostra")).ToHaveTextAsync(amostra.NrAmostra);

        await p.ClickAsync("#botaoLancarTodos");
        var num = p.Locator("[data-linha]", new() { HasText = "CONTAGEM TOTAL" });
        var desc = p.Locator("[data-linha]", new() { HasText = "SALMONELLA SP" });
        await Assertions.Expect(num.Locator("[data-corpo]")).ToBeVisibleAsync();
        await Assertions.Expect(num.Locator("[data-inicio]")).ToHaveValueAsync(amostra.DtEntrega.ToString("yyyy-MM-dd"));
        var resultado = num.Locator("[data-grupo=resultado]");
        await resultado.Locator("[data-simbolo]").SelectOptionAsync("<");
        await resultado.Locator("[data-valor]").FillAsync("1,0");
        await resultado.Locator("[data-expoente]").FillAsync("2");
        await resultado.Locator("[data-unidade]").SelectOptionAsync(new SelectOptionValue { Label = "UFC/g" });
        await Assertions.Expect(resultado.Locator("[data-previa]")).ToHaveTextAsync("< 1,0 × 10² UFC/g");

        // descritivo: a especificação acompanha o tipo do resultado
        await desc.Locator("[data-grupo=resultado] [data-numerico]").UncheckAsync();
        await Assertions.Expect(desc.Locator("[data-grupo=espec] [data-numerico]")).Not.ToBeCheckedAsync();
        await desc.Locator("[data-grupo=resultado] [data-tipo]").SelectOptionAsync(new SelectOptionValue { Label = "Ausência em 25 g" });
        await p.ClickAsync("#botaoGravar");
        Assert.Equal("Resultado gravado.", await s.AlertaAsync());
        await p.WaitForURLAsync(u => u.Contains("/Resultados/Editar/"));
        await Assertions.Expect(num.Locator("[data-previa-banco]")).ToContainTextAsync("< 1,0 × 102 UFC/g"); // o 2 em <sup>
        await Assertions.Expect(desc.Locator("[data-previa-banco]")).ToContainTextAsync("Ausência em 25 g");

        await desc.Locator("button:has-text('Excluir')").ClickAsync();
        Assert.Equal("Resultado do parâmetro excluído.", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Excluir o resultado de \"SALMONELLA SP\""));
        await Assertions.Expect(desc.Locator("[data-botao-lancar]")).ToBeVisibleAsync();
        Assert.Equal(1, await c.Db.LabResultadosParam.CountAsync(x => x.LabResultadoAnalise.IdLabHistAmostras == amostra.IdLabHistAmostras));
        Assert.Empty(s.ErrosJs);
    }

    /// <summary>
    /// T27 (5.3): novo boletim pela pesquisa, 1 de 2 parâmetros marcados, "Concluir seleção" do outro, reordenação,
    /// prévia em PDF, emissão (PDF gravado) e a tela travada depois, com "Imprimir".
    /// </summary>
    [Fact]
    public async Task Boletim_monta_reordena_visualiza_e_emite()
    {
        await using var c = await Cenario.CriarAsync();
        await c.Db.EntidadesFuncionarios.Where(f => f.IdEntidades == c.Ativa.ProfissionalId).ExecuteUpdateAsync(u => u.SetProperty(f => f.AssinaBoletim, true));
        var (amostra, _, _, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        await using var s = await app.EntrarAsync(c, comProfissional: true);
        var p = s.Pagina;

        await s.IrAsync("/Boletins");
        await p.ClickAsync("#botaoNovoBoletim");
        await Assertions.Expect(p.Locator("#modalPesquisaAmostras [data-pesquisa-grid] tbody tr")).ToHaveCountAsync(1);
        await p.ClickAsync("#modalPesquisaAmostras [data-selecionar]");
        await p.WaitForURLAsync(u => u.EndsWith($"/Boletins/Amostra/{amostra.IdLabHistAmostras}"));
        await Assertions.Expect(p.Locator("#MetodologiaAcreditada")).ToHaveValueAsync("CONT. TOTAL: ISO 4833");

        // grava com o descritivo só; depois inclui o acreditado pela "Concluir seleção" e sobe ele
        await p.Locator("#gridDisponiveis tr[data-parametro='SALMONELLA'] input").CheckAsync();
        await p.SelectOptionAsync("#IdResponsavel1", new SelectOptionValue { Index = 1 });
        await p.ClickAsync("#botaoGravar");
        Assert.Equal("Boletim gravado.", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#codBoletim")).ToHaveTextAsync(amostra.NrAmostra);
        await Assertions.Expect(p.Locator("#gridBoletim tbody tr")).ToHaveCountAsync(1);
        await p.Locator("#formSelecao tr[data-parametro='CONT. TOTAL'] input").CheckAsync();
        await p.ClickAsync("#botaoConcluirSelecao");
        Assert.Equal("Parâmetros incluídos no boletim.", await s.AlertaAsync());
        await Assertions.Expect(p.Locator("#gridBoletim tbody tr").Nth(1)).ToContainTextAsync("< 1,0 × 102");
        await p.Locator("#gridBoletim tr[data-parametro='CONT. TOTAL'] button[title=Subir]").ClickAsync();
        await Assertions.Expect(p.Locator("#gridBoletim tbody tr").First).ToHaveAttributeAsync("data-parametro", "CONT. TOTAL");

        // prévia: o botão abre o PDF numa aba nova (target=_blank); busca com os cookies da sessão
        Assert.Equal("_blank", await p.GetAttributeAsync("#botaoVisualizar", "target"));
        var resposta = await p.APIRequest.GetAsync(app.Url + await p.GetAttributeAsync("#botaoVisualizar", "href"));
        Assert.Equal("application/pdf", resposta.Headers["content-type"]);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await resposta.BodyAsync())[..4]));

        await p.ClickAsync("#botaoEmitir");
        Assert.StartsWith("Boletim emitido", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Emitir o boletim"));
        await Assertions.Expect(p.Locator("#avisoBloqueio")).ToContainTextAsync("foi emitido");
        await Assertions.Expect(p.Locator("#botaoEmitir")).ToHaveCountAsync(0);
        await Assertions.Expect(p.Locator("#botaoConcluirSelecao")).ToHaveCountAsync(0);
        await Assertions.Expect(p.Locator("#DtBoletim")).ToBeDisabledAsync();
        await Assertions.Expect(p.Locator("#botaoImprimir")).ToBeVisibleAsync();
        Assert.Single(Directory.GetFiles(app.PastaBoletins, "*.pdf", SearchOption.AllDirectories));
        Assert.Empty(s.ErrosJs);
    }

    /// <summary>T25 (4.5): do recebimento para as etiquetas com a amostra marcada; impressora escolhida; quantidade; sigilo do produto.</summary>
    [Fact]
    public async Task Etiquetas_a_partir_do_recebimento_imprimem_na_escolhida()
    {
        await using var c = await Cenario.CriarAsync();
        var amostra = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(amostra);
        await c.Db.SaveChangesAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var antes = app.Impressora.Enviados.Count;

        await s.IrAsync($"/Amostras/Editar/{amostra.IdLabHistAmostras}");
        await p.ClickAsync("#botaoEtiquetas");
        await Assertions.Expect(p.Locator("#tabelaEtiquetas tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(p.Locator("#tabelaEtiquetas tbody input[type=checkbox]")).ToBeCheckedAsync();
        await Assertions.Expect(p.Locator("#tabelaEtiquetas tbody input[type=number]")).ToHaveValueAsync("3");
        await p.FillAsync("#tabelaEtiquetas tbody input[type=number]", "2");
        await p.SelectOptionAsync("#Impressora", ImpressoraDoTeste.Argox);
        await p.CheckAsync("#SemProduto");
        await p.ClickAsync("#botaoImprimir");
        Assert.Equal($"2 etiqueta(s) enviada(s) para “{ImpressoraDoTeste.Argox}”.", await s.AlertaAsync());

        var (nome, _, dados) = app.Impressora.Enviados.Skip(antes).Single(e => System.Text.Encoding.Latin1.GetString(e.Dados).Contains(amostra.NrAmostra.Replace(" ", "")));
        var texto = System.Text.Encoding.Latin1.GetString(dados);
        Assert.Equal(ImpressoraDoTeste.Argox, nome);
        Assert.Contains("P2\r\n", texto);
        Assert.DoesNotContain("QUEIJO", texto); // sigilo do produto
        // a última impressora usada volta marcada
        await s.IrAsync("/Etiquetas");
        await Assertions.Expect(p.Locator("#Impressora")).ToHaveValueAsync(ImpressoraDoTeste.Argox);
        Assert.Empty(s.ErrosJs);
    }
}
