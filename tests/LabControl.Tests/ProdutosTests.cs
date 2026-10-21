using System.Net;
using System.Text.Json;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 3.3 (T14 Produtos): código automático de 6 dígitos, duplicidade recusada, combos, inativar/excluir.</summary>
public class ProdutosTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    [Fact]
    public async Task Codigo_automatico_digitado_e_duplicado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ProdutoService(c.Db);
        var unidade = c.Unidade.IdUnidades;

        // vazio → o banco gera 6 dígitos (o cenário já tem um produto: 000001)
        var r = await s.GravarAsync(new ProdutoVM { Descricao = "  queijo prato ", IdUnidades = unidade });
        Assert.True(r.Sucesso, r.Mensagem);
        var p = await c.Db.Produtos.AsNoTracking().SingleAsync(x => x.IdProdutos == r.Valor);
        Assert.Equal(("000002", "QUEIJO PRATO"), (p.Codigo, p.Descricao));

        // digitado vale como está (maiúsculas); repetido é recusado no campo Código
        var d = await s.GravarAsync(new ProdutoVM { Codigo = "ab-10", Descricao = "IOGURTE", IdUnidades = unidade, IdEmbalagensTipos = c.Embalagem.IdEmbalagensTipos, QtdeEmbalagem = "2,5" });
        Assert.True(d.Sucesso, d.Mensagem);
        var iogurte = await c.Db.Produtos.AsNoTracking().SingleAsync(x => x.IdProdutos == d.Valor);
        Assert.Equal(("AB-10", 2.5m), (iogurte.Codigo, iogurte.QtdeEmbalagem));
        var dup = await s.GravarAsync(new ProdutoVM { Codigo = "000002", Descricao = "OUTRO", IdUnidades = unidade });
        Assert.Equal(("Já existe um produto com este código.", "Codigo"), (dup.Mensagem, dup.Campo));

        // validações
        Assert.Equal(("Informe a descrição.", "Descricao"), Erro(await s.GravarAsync(new ProdutoVM { Descricao = " ", IdUnidades = unidade })));
        Assert.Equal(("Escolha a unidade de estoque.", "IdUnidades"), Erro(await s.GravarAsync(new ProdutoVM { Descricao = "X" })));
        Assert.Equal("Qtde. por embalagem: número maior que zero, com até 4 casas decimais.",
            (await s.GravarAsync(new ProdutoVM { Descricao = "X", IdUnidades = unidade, QtdeEmbalagem = "0" })).Mensagem);
        Assert.Equal(("Informe o código (só na inclusão ele é gerado sozinho).", "Codigo"),
            Erro(await s.GravarAsync(new ProdutoVM { Id = r.Valor, Descricao = "QUEIJO PRATO", IdUnidades = unidade })));

        // alterar mantém o código e relê a lista/busca por código ou descrição
        var vm = (await s.CarregarAsync(r.Valor))!;
        Assert.Equal("000002", vm.Codigo);
        vm.Descricao = "QUEIJO PRATO FATIADO";
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        Assert.Equal(["QUEIJO PRATO FATIADO"], (await s.ListarAsync(new ProdutoFiltroVM { Texto = "000002" })).Itens.Select(i => i.Descricao));
        var lista = await s.ListarAsync(new ProdutoFiltroVM { Texto = "iogurte" });
        Assert.Equal([("AB-10", "UFC/g", "SACO PLÁSTICO", "2,5")], lista.Itens.Select(i => (i.Codigo, i.Unidade, i.Embalagem, i.QtdeEmbalagem)));
    }

    [Fact]
    public async Task Em_uso_nao_exclui_inativo_some_e_combo_mostra_o_escolhido()
    {
        await using var c = await Cenario.CriarAsync(); // o produto do cenário está na proposta
        var s = new ProdutoService(c.Db);
        var usado = c.Produto.IdProdutos;

        Assert.Equal("Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).",
            (await s.ExcluirAsync(usado)).Mensagem);
        Assert.True((await s.DefinirInativoAsync(usado, true)).Sucesso);
        var lista = await s.ListarAsync(new ProdutoFiltroVM { Texto = "QUEIJO MINAS" });
        Assert.Equal((0, 1), (lista.Itens.Count, lista.InativosOcultos));
        Assert.Single((await s.ListarAsync(new ProdutoFiltroVM { Texto = "QUEIJO MINAS", Inativos = true })).Itens);

        // unidade inativada continua no combo do produto que a usa, mas não serve para um produto novo
        await c.Db.Unidades.Where(u => u.IdUnidades == c.Unidade.IdUnidades).ExecuteUpdateAsync(x => x.SetProperty(u => u.Ativo, false));
        var form = (await s.CarregarAsync(usado))!;
        Assert.Contains(form.Unidades, u => u.Id == c.Unidade.IdUnidades);
        Assert.DoesNotContain((await s.NovoAsync()).Unidades, u => u.Id == c.Unidade.IdUnidades);
        Assert.True((await s.GravarAsync(form)).Sucesso);
        Assert.Equal("Escolha a unidade de estoque.", (await s.GravarAsync(new ProdutoVM { Descricao = "NOVO", IdUnidades = c.Unidade.IdUnidades })).Mensagem);
        // sem uso: exclui; outra empresa não enxerga
        var ativa = new LabControl.Models.Unidade { Descricao = Unico("MILIGRAMA"), Sigla = Unico("mg") };
        c.Db.Add(ativa);
        await c.Db.SaveChangesAsync();
        var livre = (await s.GravarAsync(new ProdutoVM { Descricao = Unico("LIVRE"), IdUnidades = ativa.IdUnidades })).Valor;
        await using var b = await Cenario.CriarAsync();
        var deB = new ProdutoService(b.Db);
        Assert.Null(await deB.CarregarAsync(livre));
        Assert.Equal(ProdutoService.MensagemNaoEncontrado, (await deB.ExcluirAsync(livre)).Mensagem);
        Assert.True((await s.ExcluirAsync(livre)).Sucesso);
    }

    [Fact]
    public async Task Tela_permissoes_e_cadastro_rapido()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "PRODUTOS.CONSULTAR");
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "PRODUTOS.CONSULTAR", "PRODUTOS.INCLUIR", "PRODUTOS.ALTERAR");

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            var html = await (await web.GetAsync("/Produtos")).HtmlAsync();
            Assert.Contains("QUEIJO MINAS", html);
            Assert.DoesNotContain("href=\"/Produtos/Criar\"", html);
            Assert.Contains("<fieldset disabled", await (await web.GetAsync($"/Produtos/Editar/{c.Produto.IdProdutos}")).HtmlAsync());
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Produtos/Criar")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync("/Produtos", "/Produtos/Rapido", ("Descricao", "X"))).StatusCode);
        }

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(editor.Login);
            var nome = Unico("MANTEIGA");
            var criar = await web.PostarAsync("/Produtos/Criar", "/Produtos/Criar", ("Descricao", nome), ("IdUnidades", c.Unidade.IdUnidades.ToString()), ("Inativo", "false"));
            var id = await c.Db.Produtos.Where(p => p.Descricao == nome).Select(p => p.IdProdutos).SingleAsync();
            Assert.Equal($"/Produtos/Editar/{id}", criar.Destino());

            // erro volta para o formulário com a mensagem
            var dup = await (await web.PostarAsync("/Produtos/Criar", "/Produtos/Criar", ("Codigo", "000001"), ("Descricao", "X"), ("IdUnidades", c.Unidade.IdUnidades.ToString()))).HtmlAsync();
            Assert.Contains("Já existe um produto com este código.", dup);

            // cadastro rápido (proposta, 4.2): { id, texto }; erro → 422 { erro, campo }
            var rapido = await web.PostarAsync("/Produtos", "/Produtos/Rapido", ("Descricao", nome + " R"), ("IdUnidades", c.Unidade.IdUnidades.ToString()));
            using var json = JsonDocument.Parse(await rapido.Content.ReadAsStringAsync());
            Assert.Equal(nome + " R", json.RootElement.GetProperty("texto").GetString());
            var erro = await web.PostarAsync("/Produtos", "/Produtos/Rapido", ("Descricao", "SEM UNIDADE"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, erro.StatusCode);
            Assert.Contains("IdUnidades", await erro.Content.ReadAsStringAsync());
        }
    }

    private static (string?, string?) Erro(LabControl.Infra.IResultado r) => (r.Mensagem, r.Campo);
}
