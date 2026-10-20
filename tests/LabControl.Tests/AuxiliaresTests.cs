using System.Net;
using System.Text.Json;
using LabControl.Data;
using LabControl.Models;
using LabControl.Services.Auxiliares;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 2.1: padrão T10-00 (docs/telas/T10-00-crud-auxiliares.md) com Idiomas (T10-07).</summary>
public class AuxiliaresTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static readonly AuxiliarDefinicao Idiomas = CatalogoAuxiliares.Encontrar("idiomas")!;

    private static AuxiliarFormVM Form(string descricao, int id = 0, bool ativo = true) =>
        new() { Id = id, Ativo = ativo, Valores = { ["Descricao"] = descricao } };

    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6]}";

    [Fact]
    public async Task Inclui_altera_inativa_e_exclui_sem_uso()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);

        var nome = Unico("ALEMÃO");
        var r = await Idiomas.GravarAsync(s, Form("  " + nome + "  "));
        Assert.True(r.Sucesso, r.Mensagem);
        var id = r.Valor;
        Assert.Equal(nome, (await c.Db.Idiomas.AsNoTracking().SingleAsync(i => i.IdIdiomas == id)).Descricao); // sem espaços

        var form = (await Idiomas.CarregarAsync(s, id))!;
        Assert.Equal(nome, form.Valores["Descricao"]);
        form.Valores["Descricao"] = nome + " (DE)";
        form.Ativo = false;
        Assert.True((await Idiomas.GravarAsync(s, form)).Sucesso);
        var gravado = await c.Db.Idiomas.AsNoTracking().SingleAsync(i => i.IdIdiomas == id);
        Assert.Equal((nome + " (DE)", false), (gravado.Descricao, gravado.Ativo));

        Assert.True((await Idiomas.DefinirAtivoAsync(s, id, true)).Sucesso);
        Assert.True((await c.Db.Idiomas.AsNoTracking().SingleAsync(i => i.IdIdiomas == id)).Ativo);

        Assert.True((await Idiomas.ExcluirAsync(s, id)).Sucesso);
        Assert.False(await c.Db.Idiomas.AnyAsync(i => i.IdIdiomas == id));
        Assert.Equal("O idioma não foi encontrado (pode ter sido excluído).", (await Idiomas.ExcluirAsync(s, id)).Mensagem);
    }

    [Fact]
    public async Task Obrigatorio_tamanho_e_duplicado_na_mesma_empresa()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AuxiliarService(c.Db);

        var vazio = await Idiomas.GravarAsync(s, Form("   "));
        Assert.Equal(("Informe descrição.", "Descricao"), (vazio.Mensagem, vazio.Campo));
        Assert.Equal("Descrição: no máximo 40 caracteres.", (await Idiomas.GravarAsync(s, Form(new string('X', 41)))).Mensagem);

        // a collation não diferencia maiúsculas nem acentos: "português" = "Português"
        var existente = await c.Db.Idiomas.AsNoTracking().Select(i => i.Descricao).FirstAsync();
        var dup = await Idiomas.GravarAsync(s, Form(existente.ToLowerInvariant()));
        Assert.Equal(("Já existe um registro com esta descrição.", "Descricao"), (dup.Mensagem, dup.Campo));

        // outra empresa pode ter a mesma descrição
        await using var b = await Cenario.CriarAsync();
        var nome = Unico("JAPONÊS");
        Assert.True((await Idiomas.GravarAsync(s, Form(nome))).Sucesso);
        Assert.True((await Idiomas.GravarAsync(new AuxiliarService(b.Db), Form(nome))).Sucesso);
    }

    [Fact]
    public async Task Em_uso_nao_exclui_e_inativo_so_aparece_no_combo_do_registro_antigo()
    {
        await using var c = await Cenario.CriarAsync(); // a proposta do cenário usa o 1º idioma
        var s = new AuxiliarService(c.Db);
        var usado = await c.Db.LabPropostasProdanalises.Select(a => a.IdIdiomas).FirstAsync();

        var r = await Idiomas.ExcluirAsync(s, usado);
        Assert.Equal("Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).", r.Mensagem);
        Assert.True((await Idiomas.DefinirAtivoAsync(s, usado, false)).Sucesso);

        Assert.DoesNotContain(await Idiomas.OpcoesAsync(s, null), o => o.Id == usado);   // inclusão
        Assert.Contains(await Idiomas.OpcoesAsync(s, usado), o => o.Id == usado);         // registro antigo

        var lista = await Idiomas.ListarAsync(s, new AuxiliarFiltroVM());
        Assert.DoesNotContain(lista.Itens, i => i.Id == usado);
        Assert.Contains((await Idiomas.ListarAsync(s, new AuxiliarFiltroVM { Inativos = true })).Itens, i => i.Id == usado && !i.Ativo);
    }

    [Fact]
    public async Task Busca_paginacao_e_cada_empresa_ve_so_os_seus()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var s = new AuxiliarService(a.Db);
        var prefixo = "ZZ" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        for (var i = 1; i <= 25; i++)
            Assert.True((await Idiomas.GravarAsync(s, Form($"{prefixo} {i:00}"))).Sucesso);

        var p1 = await Idiomas.ListarAsync(s, new AuxiliarFiltroVM { Busca = prefixo.ToLowerInvariant() });
        Assert.Equal((25, 2, 20), (p1.Total, p1.Paginas, p1.Itens.Count));
        Assert.Equal($"{prefixo} 01", p1.Itens[0].Valores[0]);
        var p2 = await Idiomas.ListarAsync(s, new AuxiliarFiltroVM { Busca = prefixo, Pagina = 2 });
        Assert.Equal(5, p2.Itens.Count);
        Assert.Equal(2, (await Idiomas.ListarAsync(s, new AuxiliarFiltroVM { Busca = prefixo, Pagina = 99 })).Filtro.Pagina); // além do fim → última

        var deB = new AuxiliarService(b.Db);
        Assert.Equal(0, (await Idiomas.ListarAsync(deB, new AuxiliarFiltroVM { Busca = prefixo })).Total);
        var idA = p1.Itens[0].Id;
        Assert.Null(await Idiomas.CarregarAsync(deB, idA));
        Assert.False((await Idiomas.GravarAsync(deB, Form("INVASÃO", idA))).Sucesso);
        Assert.False((await Idiomas.ExcluirAsync(deB, idA)).Sucesso);
        Assert.Equal($"{prefixo} 01", (await a.Db.Idiomas.AsNoTracking().SingleAsync(i => i.IdIdiomas == idA)).Descricao);
    }

    [Fact]
    public async Task Tela_permissoes_e_cadastro_rapido()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "AUXILIARES.CONSULTAR");
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "AUXILIARES.CONSULTAR", "AUXILIARES.INCLUIR", "AUXILIARES.ALTERAR", "AUXILIARES.EXCLUIR");

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            Assert.Contains("Idiomas", await (await web.GetAsync("/Auxiliares")).HtmlAsync());
            var html = await (await web.GetAsync("/Auxiliares/IDIOMAS")).HtmlAsync();
            Assert.Contains("Mostrar inativos", html);
            Assert.DoesNotContain("data-form=\"/Auxiliares/IDIOMAS/Incluir\"", html); // botão escondido sem permissão
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Auxiliares/IDIOMAS/Incluir")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync("/Auxiliares/IDIOMAS", "/Auxiliares/IDIOMAS/Rapido", ("Valores[Descricao]", "X"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await web.GetAsync("/Auxiliares/NAO_EXISTE")).StatusCode);
        }

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(editor.Login);
            var nome = Unico("ITALIANO");
            var incluir = await web.PostarAsync("/Auxiliares/IDIOMAS", "/Auxiliares/IDIOMAS/Incluir",
                ("Valores[Descricao]", nome), ("Ativo", "true"), ("Ativo", "false"), ("Busca", "ITA"), ("Inativos", "false"), ("Pagina", "1"));
            Assert.Equal("/Auxiliares/IDIOMAS?Busca=ITA", incluir.Destino()); // volta ao mesmo filtro (página 1 fica implícita)
            var idioma = await c.Db.Idiomas.AsNoTracking().SingleAsync(i => i.Descricao == nome);
            Assert.True(idioma.Ativo);

            // desmarcar "Ativo" grava inativo (checkbox + hidden)
            await web.PostarAsync("/Auxiliares/IDIOMAS", $"/Auxiliares/IDIOMAS/Editar/{idioma.IdIdiomas}", ("Valores[Descricao]", nome), ("Ativo", "false"));
            Assert.False((await c.Db.Idiomas.AsNoTracking().SingleAsync(i => i.IdIdiomas == idioma.IdIdiomas)).Ativo);

            // duplicado: a lista volta com o modal aberto e o erro no campo
            var dup = await web.PostarAsync("/Auxiliares/IDIOMAS", "/Auxiliares/IDIOMAS/Incluir", ("Valores[Descricao]", nome), ("Ativo", "true"));
            Assert.Equal(HttpStatusCode.OK, dup.StatusCode);
            var htmlDup = await dup.HtmlAsync();
            Assert.Contains("Já existe um registro com esta descrição.", htmlDup);
            Assert.Contains("id=\"formAuxiliar\"", htmlDup);

            // excluir em uso: mensagem + oferta de inativar
            var usado = await c.Db.LabPropostasProdanalises.Select(a => a.IdIdiomas).FirstAsync();
            var excluir = await web.PostarAsync("/Auxiliares/IDIOMAS", $"/Auxiliares/IDIOMAS/Excluir/{usado}");
            var depois = await (await web.GetAsync(excluir.Destino()!)).HtmlAsync();
            Assert.Contains("não pode ser excluído", depois);
            Assert.Contains($"/Auxiliares/IDIOMAS/Ativo/{usado}", depois);

            // cadastro rápido "+": JSON { id, texto }; erro → 422 { erro, campo }
            var rapido = await web.PostarAsync("/Auxiliares/IDIOMAS", "/Auxiliares/IDIOMAS/Rapido", ("Valores[Descricao]", nome + " R"));
            using var json = JsonDocument.Parse(await rapido.Content.ReadAsStringAsync());
            Assert.Equal(nome + " R", json.RootElement.GetProperty("texto").GetString());
            Assert.True(await c.Db.Idiomas.AnyAsync(i => i.IdIdiomas == json.RootElement.GetProperty("id").GetInt32()));
            var erro = await web.PostarAsync("/Auxiliares/IDIOMAS", "/Auxiliares/IDIOMAS/Rapido", ("Valores[Descricao]", nome + " R"));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, erro.StatusCode);
            Assert.Contains("Descricao", await erro.Content.ReadAsStringAsync());

            // opções do combo: ativos (o inativo só quando já escolhido)
            var opcoes = await (await web.GetAsync("/Auxiliares/IDIOMAS/Opcoes")).Content.ReadAsStringAsync();
            Assert.DoesNotContain(nome + "\"", opcoes);
            Assert.Contains(nome + "\"", await (await web.GetAsync($"/Auxiliares/IDIOMAS/Opcoes?atual={idioma.IdIdiomas}")).Content.ReadAsStringAsync());
        }
    }
}
