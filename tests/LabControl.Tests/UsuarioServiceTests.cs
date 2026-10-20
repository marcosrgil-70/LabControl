using System.Net;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.3 (docs/telas/T02-usuarios.md).</summary>
public class UsuarioServiceTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static UsuarioService Servico(LabDbContext db, IEmpresaContexto ctx) => new(db, ctx, new VinculoUsuarioProfissionalService(db, ctx));

    private static Task<int> PerfilAdministradorAsync(Cenario c) =>
        c.Db.Perfis.Where(p => p.Nome == "ADMINISTRADOR").Select(p => p.IdPerfis).SingleAsync();

    private static async Task<UsuarioVM> NovoComPerfilAsync(UsuarioService s, Cenario c, int? profissional = null)
    {
        var vm = await s.NovoAsync();
        vm.Login = "n" + Guid.NewGuid().ToString("N")[..12];
        vm.Nome = "NOVO USUÁRIO";
        var linha = vm.Empresas.Single(e => e.IdEmpresa == c.Empresa.IdEmpresas);
        linha.Acesso = true;
        linha.IdProfissional = profissional;
        linha.Perfis = [await PerfilAdministradorAsync(c)];
        return vm;
    }

    [Fact]
    public async Task Inclusao_gera_senha_provisoria_e_obriga_a_troca_no_primeiro_acesso()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c.Db, c.Ativa); // administrador da empresa (não global)
        var vm = await NovoComPerfilAsync(s, c, c.Ativa.ProfissionalId);
        var r = await s.IncluirAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Null(ContaService.ValidarNovaSenha(r.Valor.Senha));
        Assert.Equal(vm.Login.ToUpperInvariant(), r.Valor.Login);

        var usu = await c.Db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == r.Valor.IdUsuario);
        Assert.True(usu.TrocarSenha);
        Assert.False(usu.AdminGlobal);
        var vinc = await c.Db.UsuariosEmpresas.AsNoTracking().SingleAsync(v => v.IdUsuarios == usu.IdUsuarios);
        Assert.Equal((c.Empresa.IdEmpresas, c.Ativa.ProfissionalId, true), (vinc.IdEmpresas, vinc.IdEntidadesFunc, vinc.Padrao));

        // a senha provisória entra e a identidade sai com a troca pendente
        var conta = UsuariosTeste.Servico(c.Db, c.Ativa);
        Assert.True((await conta.EntrarAsync(usu.Login, r.Valor.Senha)).Sucesso);
        Assert.True((await conta.MontarIdentidadeAsync(usu.IdUsuarios)).Valor.HasClaim(LabClaims.TrocarSenha, "1"));

        // auditoria das três tabelas (antes/depois)
        foreach (var t in new[] { "USUARIOS", "USUARIOS_EMPRESAS", "USUARIOS_PERFIS" })
            Assert.True(await c.Db.Auditoria.IgnoreQueryFilters().AnyAsync(a => a.Tabela == t && a.IdRegistro.StartsWith(usu.IdUsuarios + "")), t);
    }

    [Fact]
    public async Task Mesmo_profissional_em_dois_usuarios_da_empresa_e_recusado_com_mensagem_clara()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c.Db, c.Ativa);
        Assert.True((await s.IncluirAsync(await NovoComPerfilAsync(s, c, c.Ativa.ProfissionalId))).Sucesso);

        var r = await s.IncluirAsync(await NovoComPerfilAsync(s, c, c.Ativa.ProfissionalId));
        Assert.Equal(VinculoUsuarioProfissionalService.MensagemProfissionalEmUso, r.Mensagem);
        Assert.Equal("Empresas[0].IdProfissional", r.Campo);
    }

    [Fact]
    public async Task Administrador_local_so_altera_o_vinculo_da_sua_empresa()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var compartilhado = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(compartilhado.IdUsuarios, a.Empresa.IdEmpresas);
        await UsuariosTeste.VincularAsync(compartilhado.IdUsuarios, b.Empresa.IdEmpresas);
        var soDeB = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(soDeB.IdUsuarios, b.Empresa.IdEmpresas);

        var s = Servico(a.Db, a.Ativa); // administrador da empresa A
        var lista = await s.ListarAsync(new UsuarioFiltroVM { Ativos = null });
        Assert.Contains(lista, u => u.Id == compartilhado.IdUsuarios);
        Assert.DoesNotContain(lista, u => u.Id == soDeB.IdUsuarios);
        Assert.Null(await s.CarregarAsync(soDeB.IdUsuarios));
        Assert.All(lista, u => Assert.Equal("", u.Empresas)); // não vê as outras empresas do usuário

        var vm = (await s.CarregarAsync(compartilhado.IdUsuarios))!;
        Assert.False(vm.PodeEditarDados);
        Assert.Equal([a.Empresa.IdEmpresas], vm.Empresas.Select(e => e.IdEmpresa)); // só a linha da empresa ativa
        vm.Nome = "TENTATIVA DE RENOMEAR";
        vm.Ativo = false;
        vm.Empresas[0].Perfis = [await PerfilAdministradorAsync(a)];
        vm.Empresas.Add(new UsuarioEmpresaVM { IdEmpresa = b.Empresa.IdEmpresas, Acesso = false }); // linha forjada de B
        Assert.True((await s.AlterarAsync(vm)).Sucesso);

        var depois = await a.Db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == compartilhado.IdUsuarios);
        Assert.Equal((compartilhado.Nome, true), (depois.Nome, depois.Ativo));               // dados globais intactos
        Assert.True(await a.Db.UsuariosEmpresas.AnyAsync(v => v.IdUsuarios == compartilhado.IdUsuarios && v.IdEmpresas == b.Empresa.IdEmpresas));
        Assert.True(await a.Db.UsuariosPerfis.AnyAsync(p => p.IdUsuarios == compartilhado.IdUsuarios && p.IdEmpresas == a.Empresa.IdEmpresas));

        Assert.Equal(UsuarioService.MensagemSoAdminGlobal, (await s.ResetarSenhaAsync(compartilhado.IdUsuarios)).Mensagem);
        Assert.Equal(UsuarioService.MensagemSoAdminGlobal, (await s.ExcluirAsync(compartilhado.IdUsuarios)).Mensagem);
        Assert.True((await s.DesbloquearAsync(compartilhado.IdUsuarios)).Sucesso);

        // ADMIN_GLOBAL altera os dados e vê todas as empresas
        var global = new EmpresaFixa { EmpresaId = a.Empresa.IdEmpresas, UsuarioId = a.Ativa.UsuarioId, AdminGlobal = true };
        await using var dbG = await BancoTeste.NovoContextoAsync(global);
        var sg = Servico(dbG, global);
        var vmG = (await sg.CarregarAsync(compartilhado.IdUsuarios))!;
        Assert.True(vmG.PodeEditarDados);
        Assert.Contains(vmG.Empresas, e => e.IdEmpresa == b.Empresa.IdEmpresas && e.Acesso);
        Assert.True((await sg.ResetarSenhaAsync(compartilhado.IdUsuarios)).Sucesso);
    }

    [Fact]
    public async Task Usuario_nao_se_inativa_nem_remove_o_proprio_acesso()
    {
        await using var c = await Cenario.CriarAsync();
        var eu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(eu.IdUsuarios, c.Empresa.IdEmpresas);
        var ctx = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, UsuarioId = eu.IdUsuarios };
        await using var db = await BancoTeste.NovoContextoAsync(ctx);
        var s = Servico(db, ctx);

        var vm = (await s.CarregarAsync(eu.IdUsuarios))!;
        vm.Ativo = false;
        Assert.Equal("Você não pode inativar o seu próprio usuário.", (await s.AlterarAsync(vm)).Mensagem);

        db.ChangeTracker.Clear();
        vm = (await s.CarregarAsync(eu.IdUsuarios))!;
        vm.Empresas[0].Acesso = false;
        Assert.Equal("Você não pode remover o seu próprio acesso à empresa ativa.", (await s.AlterarAsync(vm)).Mensagem);
        Assert.Equal("Você não pode excluir o seu próprio usuário.", (await s.ExcluirAsync(eu.IdUsuarios)).Mensagem);
    }

    [Fact]
    public async Task Exclusao_so_de_quem_nunca_gravou_e_autoria_preenchida_pelo_contexto()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c.Db, c.Ativa);
        var novo = await s.IncluirAsync(await NovoComPerfilAsync(s, c));
        Assert.True((await s.ExcluirAsync(novo.Valor.IdUsuario)).Sucesso);
        Assert.False(await c.Db.Usuarios.AnyAsync(u => u.IdUsuarios == novo.Valor.IdUsuario));

        // usuário que gravou um produto (CRIADO_POR preenchido pelo SaveChanges) não pode ser excluído
        var autor = await s.IncluirAsync(await NovoComPerfilAsync(s, c));
        var ctxAutor = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, UsuarioId = autor.Valor.IdUsuario };
        await using (var dbAutor = await BancoTeste.NovoContextoAsync(ctxAutor))
        {
            var prod = new Produto { Descricao = "GRAVADO PELO AUTOR", IdUnidades = c.Unidade.IdUnidades };
            dbAutor.Add(prod);
            await dbAutor.SaveChangesAsync();
            Assert.Equal(autor.Valor.IdUsuario, prod.CriadoPor);
            prod.Descricao = "ALTERADO PELO AUTOR";
            await dbAutor.SaveChangesAsync();
            Assert.Equal(autor.Valor.IdUsuario, prod.AlteradoPor);
        }
        Assert.Equal("Este usuário já gravou registros no sistema: inative-o em vez de excluir.", (await s.ExcluirAsync(autor.Valor.IdUsuario)).Mensagem);
    }

    [Fact]
    public async Task Reset_gera_nova_senha_e_desbloqueia()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c.Db, c.Ativa);
        var novo = await s.IncluirAsync(await NovoComPerfilAsync(s, c));
        await c.Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE USUARIOS SET BLOQUEADO_ATE = NOW() + INTERVAL 10 MINUTE, TENTATIVAS_FALHAS = 3, TROCAR_SENHA = 0 WHERE ID_USUARIOS = {novo.Valor.IdUsuario}");
        c.Db.ChangeTracker.Clear();

        var reset = await s.ResetarSenhaAsync(novo.Valor.IdUsuario);
        Assert.NotEqual(novo.Valor.Senha, reset.Valor.Senha);
        var u = await c.Db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuarios == novo.Valor.IdUsuario);
        Assert.Equal((true, 0, (DateTime?)null), (u.TrocarSenha, u.TentativasFalhas, u.BloqueadoAte));
        Assert.True((await UsuariosTeste.Servico(c.Db, c.Ativa).EntrarAsync(u.Login, reset.Valor.Senha)).Sucesso);
    }

    [Fact]
    public async Task Sem_USUARIOS_INCLUIR_nao_ve_o_botao_e_recebe_403_ao_postar()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "USUARIOS.CONSULTAR");
        using var web = app.Cliente();
        await web.EntrarAsync(leitor.Login);

        var lista = await web.GetAsync("/Usuarios");
        Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
        var html = await lista.HtmlAsync();
        Assert.Contains(leitor.Login, html);
        Assert.DoesNotContain("/Usuarios/Criar", html);
        Assert.Contains("href=\"/Usuarios\"", html); // item do menu aparece agora que a tela existe

        Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Usuarios/Criar")).StatusCode);
        var post = await web.PostarAsync("/Usuarios", "/Usuarios/Criar", ("Login", "XPTO"), ("Nome", "X"));
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    [Fact]
    public async Task Tela_de_inclusao_mostra_a_senha_uma_vez()
    {
        await using var c = await Cenario.CriarAsync();
        var gestor = await UsuariosTeste.ComPermissoesAsync(c, "USUARIOS.CONSULTAR", "USUARIOS.INCLUIR", "USUARIOS.ALTERAR");
        var perfil = await PerfilAdministradorAsync(c);
        using var web = app.Cliente();
        await web.EntrarAsync(gestor.Login);
        Assert.Equal(HttpStatusCode.OK, (await web.GetAsync("/Usuarios/Criar")).StatusCode);

        var login = "w" + Guid.NewGuid().ToString("N")[..12];
        var r = await web.PostarAsync("/Usuarios/Criar", "/Usuarios/Criar", ("Login", login), ("Nome", "PELA TELA"), ("Ativo", "true"),
            ("Empresas[0].IdEmpresa", c.Empresa.IdEmpresas.ToString()), ("Empresas[0].Acesso", "true"), ("Empresas[0].Ativo", "true"),
            ("Empresas[0].Perfis", perfil.ToString()), ("EmpresaPadrao", c.Empresa.IdEmpresas.ToString()));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("no-store", r.Headers.CacheControl?.ToString());
        var html = await r.HtmlAsync();
        Assert.Contains("somente agora", html);
        Assert.Contains(login.ToUpperInvariant(), html);
    }

    [Fact]
    public async Task Novo_usuario_ja_vem_com_a_empresa_ativa_e_aparece_no_combo_do_profissional()
    {
        // cadastrado pelo ADMIN_GLOBAL sem mexer na grade de empresas: tem que entrar no combo "Usuário do sistema" (T13)
        await using var c = await Cenario.CriarAsync();
        var global = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, AdminGlobal = true };
        var vm = await Servico(c.Db, global).NovoAsync();
        Assert.Equal(c.Empresa.IdEmpresas, vm.EmpresaPadrao);
        Assert.Equal([c.Empresa.IdEmpresas], vm.Empresas.Where(e => e.Acesso).Select(e => e.IdEmpresa));

        vm.Login = "n" + Guid.NewGuid().ToString("N")[..12];
        vm.Nome = "USUÁRIO SEM PROFISSIONAL";
        vm.Empresas.Single(e => e.Acesso).Perfis = [await PerfilAdministradorAsync(c)];
        var r = await Servico(c.Db, global).IncluirAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);

        var profissionais = new LabControl.Services.Entidades.ProfissionalService(c.Db, c.Ativa, new VinculoUsuarioProfissionalService(c.Db, c.Ativa));
        var prof = await profissionais.NovoAsync();
        Assert.Contains(prof.Usuarios, u => u.Id == r.Valor.IdUsuario);
    }
}
