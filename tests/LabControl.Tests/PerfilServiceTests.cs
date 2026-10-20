using System.Net;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.4 (docs/telas/T04-perfis-permissoes.md).</summary>
public class PerfilServiceTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static Task<int> IdAsync(LabDbContext db, string codigo) =>
        db.Permissoes.Where(p => p.Codigo == codigo).Select(p => p.IdPermissoes).SingleAsync();

    private static Task<List<string>> CodigosAsync(LabDbContext db, int perfil) =>
        db.PerfisPermissoes.IgnoreQueryFilters().Where(pp => pp.IdPerfis == perfil).Select(pp => pp.Permissao.Codigo).OrderBy(c => c).ToListAsync();

    [Fact]
    public async Task Empresa_nova_ja_tem_os_perfis_sugeridos()
    {
        await using var c = await Cenario.CriarAsync();
        var nomes = await c.Db.Perfis.Select(p => p.Nome).OrderBy(n => n).ToListAsync();
        Assert.Equal(["ADMINISTRADOR", "ANALISTA", "COMERCIAL", "RECEPÇÃO", "RESPONSÁVEL TÉCNICO"], nomes);

        var comercial = await c.Db.Perfis.Where(p => p.Nome == "COMERCIAL").Select(p => p.IdPerfis).SingleAsync();
        var codigos = await CodigosAsync(c.Db, comercial);
        Assert.Contains("PEDIDOS_VENDA.GERAR", codigos);
        Assert.Contains("PROPOSTAS.REVISAR", codigos);
        Assert.DoesNotContain("BOLETINS.EMITIR", codigos);
        var rt = await c.Db.Perfis.Where(p => p.Nome == "RESPONSÁVEL TÉCNICO").Select(p => p.IdPerfis).SingleAsync();
        Assert.Contains("BOLETINS.EMITIR", await CodigosAsync(c.Db, rt));
    }

    [Fact]
    public async Task Inclusao_marca_o_consultar_e_recusa_nome_repetido()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new PerfilService(c.Db, c.Ativa);
        var r = await s.IncluirAsync(new PerfilVM { Nome = " estoque ", Permissoes = [await IdAsync(c.Db, "AMOSTRAS.INCLUIR"), 999999] });
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal("ESTOQUE", (await c.Db.Perfis.SingleAsync(p => p.IdPerfis == r.Valor)).Nome);
        Assert.Equal(["AMOSTRAS.CONSULTAR", "AMOSTRAS.INCLUIR"], await CodigosAsync(c.Db, r.Valor)); // id fora do catálogo ignorado

        var dup = await s.IncluirAsync(new PerfilVM { Nome = "Estoque" });
        Assert.Equal(("Já existe um perfil com este nome.", "Nome"), (dup.Mensagem, dup.Campo));
        Assert.Equal("O nome ADMINISTRADOR é reservado.", (await s.IncluirAsync(new PerfilVM { Nome = "administrador" })).Mensagem);

        // alteração: só a diferença, auditada
        c.Db.ChangeTracker.Clear();
        var vm = (await s.CarregarAsync(r.Valor))!;
        vm.Permissoes = [await IdAsync(c.Db, "RESULTADOS.INCLUIR")];
        Assert.True((await s.AlterarAsync(vm)).Sucesso);
        Assert.Equal(["RESULTADOS.CONSULTAR", "RESULTADOS.INCLUIR"], await CodigosAsync(c.Db, r.Valor));
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "PERFIS_PERMISSOES" && a.Acao == AcoesAuditoria.Exclusao
                                                    && a.IdRegistro.StartsWith(r.Valor + "-")));
    }

    [Fact]
    public async Task Administrador_nao_e_alterado_nem_excluido_e_perfil_com_usuarios_nao_e_excluido()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new PerfilService(c.Db, c.Ativa);
        var admin = await c.Db.Perfis.SingleAsync(p => p.Nome == PerfilService.Administrador);
        var vm = (await s.CarregarAsync(admin.IdPerfis))!;
        Assert.True(vm.Administrador);
        vm.Permissoes = [];
        Assert.Equal(PerfilService.MensagemAdministrador, (await s.AlterarAsync(vm)).Mensagem);
        Assert.Equal(PerfilService.MensagemAdministrador, (await s.ExcluirAsync(admin.IdPerfis)).Mensagem);
        c.Db.ChangeTracker.Clear();
        Assert.Equal(await c.Db.Permissoes.CountAsync(), await c.Db.PerfisPermissoes.CountAsync(pp => pp.IdPerfis == admin.IdPerfis));

        // perfil atribuído a usuário: não exclui; sem usuários: exclui
        var usuario = await UsuariosTeste.ComPermissoesAsync(c, "AMOSTRAS.CONSULTAR");
        var emUso = await c.Db.UsuariosPerfis.Where(up => up.IdUsuarios == usuario.IdUsuarios).Select(up => up.IdPerfis).SingleAsync();
        Assert.StartsWith("Este perfil está atribuído a usuários", (await s.ExcluirAsync(emUso)).Mensagem);
        var livre = await s.IncluirAsync(new PerfilVM { Nome = "TEMPORARIO", Permissoes = [await IdAsync(c.Db, "PRODUTOS.CONSULTAR")] });
        Assert.True((await s.ExcluirAsync(livre.Valor)).Sucesso);
        Assert.False(await c.Db.Perfis.AnyAsync(p => p.IdPerfis == livre.Valor));
    }

    [Fact]
    public async Task Copiar_perfil_na_empresa_e_entre_empresas_so_admin_global()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var analistaB = await b.Db.Perfis.Where(p => p.Nome == "ANALISTA").Select(p => p.IdPerfis).SingleAsync();
        var analistaA = await a.Db.Perfis.Where(p => p.Nome == "ANALISTA").Select(p => p.IdPerfis).SingleAsync();

        // administrador da empresa A: copia da própria empresa; a de B nem aparece
        var local = new PerfilService(a.Db, a.Ativa);
        Assert.DoesNotContain(await local.OrigensAsync(), o => o.Id == analistaB);
        Assert.Equal("Perfil de origem não encontrado.", (await local.CopiarAsync(analistaB, "ROUBADO")).Mensagem);
        var copia = await local.CopiarAsync(analistaA, "ANALISTA 2");
        Assert.True(copia.Sucesso, copia.Mensagem);
        Assert.Equal(await CodigosAsync(a.Db, analistaA), await CodigosAsync(a.Db, copia.Valor));

        // ADMIN_GLOBAL com a empresa A ativa copia o perfil de B para A
        var global = new EmpresaFixa { EmpresaId = a.Empresa.IdEmpresas, UsuarioId = a.Ativa.UsuarioId, AdminGlobal = true };
        await using var dbG = await BancoTeste.NovoContextoAsync(global);
        var sg = new PerfilService(dbG, global);
        Assert.Contains(await sg.OrigensAsync(), o => o.Id == analistaB && !o.DaEmpresaAtiva);
        var entre = await sg.CopiarAsync(analistaB, "ANALISTA DE B");
        Assert.True(entre.Sucesso, entre.Mensagem);
        Assert.Equal(a.Empresa.IdEmpresas, (await dbG.Perfis.SingleAsync(p => p.IdPerfis == entre.Valor)).IdEmpresas);
        Assert.Equal(await CodigosAsync(dbG, analistaB), await CodigosAsync(dbG, entre.Valor));
    }

    [Fact]
    public async Task Sem_PERFIS_INCLUIR_nao_ve_os_botoes_e_recebe_403()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "PERFIS.CONSULTAR");
        using var web = app.Cliente();
        await web.EntrarAsync(leitor.Login);

        var lista = await web.GetAsync("/Perfis");
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var html = await lista.HtmlAsync();
        Assert.Contains("RESPONSÁVEL TÉCNICO", html);
        Assert.DoesNotContain("/Perfis/Criar", html);
        Assert.DoesNotContain("/Perfis/Copiar", html);
        Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync("/Perfis", "/Perfis/Criar", ("Nome", "X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Perfis/Editar/1")).StatusCode);
    }

    [Fact]
    public async Task Tela_da_matriz_grava_pelo_formulario()
    {
        await using var c = await Cenario.CriarAsync();
        var gestor = await UsuariosTeste.ComPermissoesAsync(c, "PERFIS.CONSULTAR", "PERFIS.INCLUIR", "PERFIS.ALTERAR");
        using var web = app.Cliente();
        await web.EntrarAsync(gestor.Login);
        var form = await web.GetAsync("/Perfis/Criar");
        Assert.Equal(HttpStatusCode.OK, form.StatusCode);
        Assert.Contains("BOLETINS.EMITIR", await form.HtmlAsync());

        var emitir = await IdAsync(c.Db, "BOLETINS.EMITIR");
        var r = await web.PostarAsync("/Perfis/Criar", "/Perfis/Criar", ("Nome", "EMISSOR"), ("Ativo", "true"), ("Permissoes", emitir.ToString()));
        Assert.Equal("/Perfis", r.Destino());
        var id = await c.Db.Perfis.Where(p => p.Nome == "EMISSOR").Select(p => p.IdPerfis).SingleAsync();
        Assert.Equal(["BOLETINS.CONSULTAR", "BOLETINS.EMITIR"], await CodigosAsync(c.Db, id));
    }
}
