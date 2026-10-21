using System.Net;
using System.Text;
using LabControl.Infra;
using LabControl.Services.Geografia;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 2.3: T11-01…03 (países, estados, cidades — tabelas globais) e a importação IBGE. Cada teste cria o
/// seu país (nome sorteado) para as siglas de UF não colidirem; o que entra nas tabelas globais é apagado no fim.
/// </summary>
public class GeografiaTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static GeoFormVM Form(int id = 0, int? pai = null, string? nome = null, string? sigla = null, string? codigo = null) =>
        new() { Id = id, IdPai = pai, Nome = nome, Sigla = sigla, Codigo = codigo };

    private static GeografiaService Servico(Data.LabDbContext db) => new(db, new ServicoAuditoria(db, new EmpresaFixa { AdminGlobal = true }));

    [Fact]
    public async Task Pais_estado_e_cidade_com_validacoes_duplicidade_e_exclusao()
    {
        await using var db = await UsuariosTeste.ContextoAdminAsync();
        var s = Servico(db);
        const string P = GeografiaService.Paises, E = GeografiaService.Estados, C = GeografiaService.Cidades;

        // país
        Assert.Equal(("Informe o nome.", "Nome"), Erro(await s.GravarAsync(P, Form(nome: "  "))));
        Assert.Equal(("Código BACEN: até 10 dígitos.", "Codigo"), Erro(await s.GravarAsync(P, Form(nome: Unico("PAÍS"), codigo: "12A"))));
        var nomePais = Unico("terra média");
        var pais = await s.GravarAsync(P, Form(nome: nomePais, sigla: "tm", codigo: "9999"));
        Assert.True(pais.Sucesso, pais.Mensagem);
        Assert.Equal((nomePais.ToUpperInvariant(), "TM"), await db.Paises.AsNoTracking().Where(x => x.IdPaises == pais.Valor).Select(x => ValueTuple.Create(x.Nome, x.Sigla)).SingleAsync());
        Assert.Equal(("Já existe um país com este nome.", "Nome"), Erro(await s.GravarAsync(P, Form(nome: nomePais))));

        // estado
        Assert.Equal(("Escolha o país.", "IdPai"), Erro(await s.GravarAsync(E, Form(nome: "GONDOR", sigla: "GD"))));
        Assert.Equal(("Sigla: 2 letras (ex.: SP).", "Sigla"), Erro(await s.GravarAsync(E, Form(pai: pais.Valor, nome: "GONDOR", sigla: "G1"))));
        Assert.Equal(("Código IBGE: 2 dígitos (ex.: 35).", "Codigo"), Erro(await s.GravarAsync(E, Form(pai: pais.Valor, nome: "GONDOR", sigla: "GD", codigo: "123"))));
        var estado = await s.GravarAsync(E, Form(pai: pais.Valor, nome: "gondor", sigla: "gd", codigo: "98"));
        Assert.True(estado.Sucesso, estado.Mensagem);
        Assert.Equal(("Já existe um estado com esta sigla neste país.", "Sigla"), Erro(await s.GravarAsync(E, Form(pai: pais.Valor, nome: "OUTRO", sigla: "GD"))));

        // cidade: 7 dígitos começando pelo código da UF; código único
        Assert.Equal(("Escolha o estado.", "IdPai"), Erro(await s.GravarAsync(C, Form(nome: "MINAS TIRITH"))));
        Assert.Equal(("Código IBGE: 7 dígitos (ex.: 3550308).", "Codigo"), Erro(await s.GravarAsync(C, Form(pai: estado.Valor, nome: "MINAS TIRITH", codigo: "98123"))));
        Assert.Equal(("Código IBGE: as cidades de GD começam por 98.", "Codigo"), Erro(await s.GravarAsync(C, Form(pai: estado.Valor, nome: "MINAS TIRITH", codigo: "3550308"))));
        var codigo = "98" + Random.Shared.Next(10000, 99999);
        var cidade = await s.GravarAsync(C, Form(pai: estado.Valor, nome: "minas tirith", codigo: codigo));
        Assert.True(cidade.Sucesso, cidade.Mensagem);
        Assert.Equal(("Já existe uma cidade com este código IBGE.", "Codigo"), Erro(await s.GravarAsync(C, Form(pai: estado.Valor, nome: "OSGILIATH", codigo: codigo))));

        // alterar
        db.ChangeTracker.Clear();
        Assert.True((await s.GravarAsync(C, Form(cidade.Valor, estado.Valor, "MINAS TIRITH (CAPITAL)", codigo: codigo))).Sucesso);
        Assert.Equal("MINAS TIRITH (CAPITAL)", (await db.Cidades.AsNoTracking().SingleAsync(x => x.IdCidades == cidade.Valor)).Nome);

        // listas: busca, filtro e contagem de filhos
        var paises = await s.ListarAsync(P, new GeoFiltroVM { Busca = nomePais[^6..] });
        Assert.Equal(1, paises.Itens.Single().Filhos);
        var cidades = await s.ListarAsync(C, new GeoFiltroVM { IdEstado = estado.Valor });
        Assert.Equal([("MINAS TIRITH (CAPITAL)", "GD", codigo)], cidades.Itens.Select(x => (x.Nome, x.Pai, x.Codigo)));
        Assert.Single((await s.ListarAsync(C, new GeoFiltroVM { Busca = codigo })).Itens);

        // excluir: só sem filhos (de baixo para cima)
        Assert.Equal("O país tem estados cadastrados: não pode ser excluído.", (await s.ExcluirAsync(P, pais.Valor)).Mensagem);
        Assert.Equal("O estado tem cidades cadastradas: não pode ser excluído.", (await s.ExcluirAsync(E, estado.Valor)).Mensagem);
        Assert.True((await s.ExcluirAsync(C, cidade.Valor)).Sucesso);
        Assert.True((await s.ExcluirAsync(E, estado.Valor)).Sucesso);
        Assert.True((await s.ExcluirAsync(P, pais.Valor)).Sucesso);
        Assert.Equal("Registro não encontrado (pode ter sido excluído).", (await s.ExcluirAsync(P, pais.Valor)).Mensagem);
    }

    [Fact]
    public async Task Cidade_usada_em_endereco_de_qualquer_empresa_nao_e_excluida()
    {
        await using var c = await Cenario.CriarAsync(); // o endereço do cliente usa a "CIDADE TESTE"
        var idCidade = await c.Db.EntidadesEnderecos.Select(e => e.IdCidades).FirstAsync();
        // o administrador global com outra empresa ativa (ou nenhuma) não enxerga o endereço pelo filtro, mas o uso conta
        await using var db = await UsuariosTeste.ContextoAdminAsync();
        var r = await Servico(db).ExcluirAsync(GeografiaService.Cidades, idCidade);
        Assert.Equal("A cidade é usada em endereços de cadastros: não pode ser excluída.", r.Mensagem);
    }

    [Fact]
    public async Task Importacao_ibge_inclui_atualiza_e_relata_erros()
    {
        await using var db = await UsuariosTeste.ContextoAdminAsync();
        var s = Servico(db);
        var novo = "35" + Random.Shared.Next(90000, 99999); // código de SP que não existe no IBGE
        try
        {
            var csv = $"""
                CODIGO;NOME;UF
                3550308;São Paulo;SP
                {novo};"Cidade Importada";SP

                3304557;RIO DE JANEIRO;SP
                9912345;LUGAR NENHUM;XX
                12;CURTO;SP
                {novo};REPETIDA;SP
                """;
            var r = await s.ImportarIbgeAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
            Assert.True(r.Sucesso, r.Mensagem);
            Assert.Equal((5, 1, 0, 1), (r.Valor.Lidas, r.Valor.Incluidas, r.Valor.Atualizadas, r.Valor.Iguais));
            Assert.Equal([
                "Linha 5: UF SP não confere com o código 3304557 (RJ).",
                "Linha 6: nenhum estado com o código IBGE 99.",
                "Linha 7: código IBGE \"12\" inválido (7 dígitos).",
                "Linha 8: código " + novo + " repetido no arquivo."], r.Valor.Erros);
            var cidade = await db.Cidades.AsNoTracking().Include(x => x.Estado).SingleAsync(x => x.CodigoIbge == novo);
            Assert.Equal(("CIDADE IMPORTADA", "SP"), (cidade.Nome, cidade.Estado.Sigla));

            // de novo, em Latin-1 (CSV do Excel) e com vírgula: o nome muda pelo código, nada duplica
            db.ChangeTracker.Clear();
            var r2 = await s.ImportarIbgeAsync(new MemoryStream(Encoding.Latin1.GetBytes($"{novo},CIDADE IMPORTADA DO ESTADO DE SÃO PAULO,SP\r\n")));
            Assert.Equal((1, 0, 1, 0), (r2.Valor.Lidas, r2.Valor.Incluidas, r2.Valor.Atualizadas, r2.Valor.Iguais));
            Assert.Equal("CIDADE IMPORTADA DO ESTADO DE SÃO PAULO", (await db.Cidades.AsNoTracking().SingleAsync(x => x.CodigoIbge == novo)).Nome);
            Assert.Equal(1, await db.Cidades.CountAsync(x => x.CodigoIbge == novo));

            Assert.Equal("Nenhum município encontrado no arquivo (esperado: código IBGE; nome; UF por linha).",
                (await s.ImportarIbgeAsync(new MemoryStream(Encoding.UTF8.GetBytes("CODIGO;NOME;UF\n")))).Mensagem);
        }
        finally
        {
            await db.Cidades.Where(x => x.CodigoIbge == novo).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Telas_consulta_para_todos_e_gravacao_so_admin_global()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "GEOGRAFIA.CONSULTAR");
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            var html = await (await web.GetAsync("/Geografia/Cidades?Busca=3550308")).HtmlAsync();
            Assert.Contains("SÃO PAULO", html);
            Assert.DoesNotContain("id=\"modalGeo\"", html);      // sem Incluir/Alterar
            Assert.DoesNotContain("title=\"Alterar\"", html);
            Assert.DoesNotContain("Importar municípios IBGE", html);
            Assert.Equal(HttpStatusCode.OK, (await web.GetAsync("/Geografia/Paises")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await web.GetAsync("/Geografia/Estados")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await web.PostarAsync("/Geografia/Paises", "/Geografia/Paises/Gravar", ("Nome", Unico("PAÍS")))).StatusCode);
        }

        var global = await UsuariosTeste.CriarAsync(adminGlobal: true);
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(global.Login);
            await web.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", c.Empresa.IdEmpresas.ToString()));
            Assert.Contains("Importar municípios IBGE", await (await web.GetAsync("/Geografia/Cidades")).HtmlAsync());

            var nome = Unico("PAÍS");
            var ok = await web.PostarAsync("/Geografia/Paises", "/Geografia/Paises/Gravar", ("Id", "0"), ("Nome", nome), ("Busca", "X"));
            Assert.Equal("/Geografia/Paises?Busca=X", ok.Destino());
            var id = await c.Db.Paises.Where(p => p.Nome == nome).Select(p => p.IdPaises).SingleAsync();

            // duplicado: a lista volta com o modal aberto e o erro no campo
            var dup = await (await web.PostarAsync("/Geografia/Paises", "/Geografia/Paises/Gravar", ("Id", "0"), ("Nome", nome))).HtmlAsync();
            Assert.Contains("Já existe um país com este nome.", dup);

            var excluir = await web.PostarAsync("/Geografia/Paises", $"/Geografia/Paises/Excluir/{id}");
            Assert.Equal("/Geografia/Paises", excluir.Destino());
            Assert.False(await c.Db.Paises.AnyAsync(p => p.IdPaises == id));
            Assert.Equal(HttpStatusCode.NotFound, (await web.PostarAsync("/Geografia/Paises", "/Geografia/Bairros/Gravar", ("Nome", "X"))).StatusCode);
        }
    }

    private static (string?, string?) Erro(IResultado r) => (r.Mensagem, r.Campo);
}
