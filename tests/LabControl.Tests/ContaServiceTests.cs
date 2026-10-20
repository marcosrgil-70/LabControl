using System.Security.Claims;
using System.Text.Json;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.1 no serviço (docs/telas/T00-login-empresa.md, P02).</summary>
public class ContaServiceTests
{
    private static JsonElement Json(string? s) => JsonDocument.Parse(s!).RootElement;

    private static async Task<(LabDbContext Db, ContaService Conta)> NovoAsync()
    {
        var ctx = new EmpresaFixa();
        var db = await BancoTeste.NovoContextoAsync(ctx);
        return (db, UsuariosTeste.Servico(db, ctx));
    }

    [Fact]
    public async Task Cinco_falhas_bloqueiam_por_15_minutos_mesmo_com_a_senha_certa()
    {
        var usu = await UsuariosTeste.CriarAsync();
        var (db, conta) = await NovoAsync();
        await using var _ = db;

        for (var i = 1; i <= 4; i++) Assert.False((await conta.EntrarAsync(usu.Login, "errada")).Sucesso);
        Assert.Null((await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == usu.IdUsuarios)).BloqueadoAte);

        Assert.Equal(ContaService.MensagemLoginInvalido, (await conta.EntrarAsync(usu.Login, "errada")).Mensagem); // 5ª
        var bloqueado = await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == usu.IdUsuarios);
        Assert.InRange(bloqueado.BloqueadoAte!.Value, DateTime.Now.AddMinutes(14), DateTime.Now.AddMinutes(16));

        // dentro do prazo, a senha certa também é recusada (mesma mensagem)
        Assert.Equal(ContaService.MensagemLoginInvalido, (await conta.EntrarAsync(usu.Login, UsuariosTeste.Senha)).Mensagem);

        // passado o prazo: entra e zera o contador
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE USUARIOS SET BLOQUEADO_ATE = NOW() - INTERVAL 1 MINUTE WHERE ID_USUARIOS = {usu.IdUsuarios}");
        Assert.True((await conta.EntrarAsync(usu.Login.ToLowerInvariant(), UsuariosTeste.Senha)).Sucesso); // login em maiúsculas
        var depois = await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == usu.IdUsuarios);
        Assert.Equal((0, (DateTime?)null), (depois.TentativasFalhas, depois.BloqueadoAte));
        Assert.NotNull(depois.UltimoAcesso);
    }

    [Fact]
    public async Task Mesma_mensagem_para_inexistente_inativo_e_senha_errada_e_auditoria_sem_senha()
    {
        var usu = await UsuariosTeste.CriarAsync();
        var inativo = await UsuariosTeste.CriarAsync();
        var (db, conta) = await NovoAsync();
        await using var _ = db;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE USUARIOS SET ATIVO = 0 WHERE ID_USUARIOS = {inativo.IdUsuarios}");

        Assert.Equal(ContaService.MensagemLoginInvalido, (await conta.EntrarAsync("NAO_EXISTE_" + Guid.NewGuid().ToString("N")[..8], "x")).Mensagem);
        Assert.Equal(ContaService.MensagemLoginInvalido, (await conta.EntrarAsync(inativo.Login, UsuariosTeste.Senha)).Mensagem);
        Assert.Equal(ContaService.MensagemLoginInvalido, (await conta.EntrarAsync(usu.Login, "SenhaErrada9")).Mensagem);
        Assert.True((await conta.EntrarAsync(usu.Login, UsuariosTeste.Senha)).Sucesso);

        var eventos = await db.Auditoria.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.Tabela == "USUARIOS" && a.Acao == AcoesAuditoria.Login && a.IdRegistro == usu.IdUsuarios.ToString())
            .OrderBy(a => a.IdAuditoria).ToListAsync();
        Assert.Equal(2, eventos.Count);
        Assert.False(Json(eventos[0].DadosDepois).GetProperty("SUCESSO").GetBoolean());
        Assert.Equal("SENHA", Json(eventos[0].DadosDepois).GetProperty("MOTIVO").GetString());
        Assert.True(Json(eventos[1].DadosDepois).GetProperty("SUCESSO").GetBoolean());
        Assert.All(eventos, e => Assert.DoesNotContain("SenhaErrada9", e.DadosDepois!));
        Assert.All(eventos, e => Assert.DoesNotContain(UsuariosTeste.Senha, e.DadosDepois!));
        Assert.True(await db.Auditoria.IgnoreQueryFilters().AnyAsync(a => a.IdRegistro == inativo.IdUsuarios.ToString() && a.DadosDepois!.Contains("INATIVO")));
    }

    [Fact]
    public async Task Troca_de_senha_valida_a_politica_e_desliga_a_troca_obrigatoria()
    {
        var usu = await UsuariosTeste.CriarAsync(trocarSenha: true);
        var (db, conta) = await NovoAsync();
        await using var _ = db;

        Assert.Equal("Senha atual incorreta.", (await conta.TrocarSenhaAsync(usu.IdUsuarios, "errada", "NovaSenha1", "NovaSenha1")).Mensagem);
        Assert.Equal("NovaSenha", (await conta.TrocarSenhaAsync(usu.IdUsuarios, UsuariosTeste.Senha, "curta1", "curta1")).Campo);
        Assert.Equal("NovaSenha", (await conta.TrocarSenhaAsync(usu.IdUsuarios, UsuariosTeste.Senha, "semnumeros", "semnumeros")).Campo);
        Assert.Equal("Confirmacao", (await conta.TrocarSenhaAsync(usu.IdUsuarios, UsuariosTeste.Senha, "NovaSenha1", "Outra1234")).Campo);
        Assert.Equal("A nova senha deve ser diferente da atual.",
            (await conta.TrocarSenhaAsync(usu.IdUsuarios, UsuariosTeste.Senha, UsuariosTeste.Senha, UsuariosTeste.Senha)).Mensagem);

        Assert.True((await conta.TrocarSenhaAsync(usu.IdUsuarios, UsuariosTeste.Senha, "NovaSenha1", "NovaSenha1")).Sucesso);
        Assert.False((await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == usu.IdUsuarios)).TrocarSenha);
        Assert.False((await conta.EntrarAsync(usu.Login, UsuariosTeste.Senha)).Sucesso);
        Assert.True((await conta.EntrarAsync(usu.Login, "NovaSenha1")).Sucesso);
        // auditada pelo SaveChanges, sem o hash
        var alt = await db.Auditoria.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.Tabela == "USUARIOS" && a.Acao == AcoesAuditoria.Alteracao && a.IdRegistro == usu.IdUsuarios.ToString()).SingleAsync();
        Assert.Equal("***", Json(alt.DadosDepois).GetProperty("SENHA_HASH").GetString());
    }

    [Fact]
    public async Task Empresas_do_usuario_e_pre_selecao()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        await using var inativa = await Cenario.CriarAsync();
        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, a.Empresa.IdEmpresas);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, b.Empresa.IdEmpresas, b.Ativa.ProfissionalId, padrao: true);
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, inativa.Empresa.IdEmpresas, ativo: false);
        var (db, conta) = await NovoAsync();
        await using var _ = db;

        var disp = await conta.EmpresasDoUsuarioAsync(usu.IdUsuarios);
        Assert.Equal(new[] { a.Empresa.IdEmpresas, b.Empresa.IdEmpresas }.Order(), disp.Empresas.Select(e => e.IdEmpresas).Order());
        Assert.Equal(b.Empresa.IdEmpresas, disp.PreSelecionada); // PADRAO
        Assert.Equal(b.Ativa.ProfissionalId, disp.Empresas.Single(e => e.IdEmpresas == b.Empresa.IdEmpresas).ProfissionalId);

        Assert.True((await conta.MontarIdentidadeAsync(usu.IdUsuarios, a.Empresa.IdEmpresas)).Sucesso);
        Assert.Equal(a.Empresa.IdEmpresas, (await conta.EmpresasDoUsuarioAsync(usu.IdUsuarios)).PreSelecionada); // última usada

        // empresa inativa some da lista
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE EMPRESAS SET ATIVA = 0 WHERE ID_EMPRESAS = {a.Empresa.IdEmpresas}");
        Assert.Equal([b.Empresa.IdEmpresas], (await conta.EmpresasDoUsuarioAsync(usu.IdUsuarios)).Empresas.Select(e => e.IdEmpresas));

        // ADMIN_GLOBAL vê todas as ativas, mesmo sem vínculo
        var admin = await UsuariosTeste.CriarAsync(adminGlobal: true);
        var todas = (await conta.EmpresasDoUsuarioAsync(admin.IdUsuarios)).Empresas.Select(e => e.IdEmpresas).ToList();
        Assert.Contains(b.Empresa.IdEmpresas, todas);
        Assert.Contains(inativa.Empresa.IdEmpresas, todas); // empresa ativa (só o vínculo daquele usuário estava inativo)
        Assert.DoesNotContain(a.Empresa.IdEmpresas, todas);
    }

    [Fact]
    public async Task Identidade_tem_empresa_profissional_e_permissoes_dos_perfis_ativos()
    {
        await using var c = await Cenario.CriarAsync();
        var codigos = await c.Db.Permissoes.OrderBy(p => p.IdPermissoes).Take(3).ToListAsync();
        var leitura = new Perfil { Nome = "LEITURA" };
        var desligado = new Perfil { Nome = "DESLIGADO", Ativo = false };
        c.Db.AddRange(leitura, desligado);
        await c.Db.SaveChangesAsync();
        c.Db.AddRange(
            new PerfilPermissao { IdPerfis = leitura.IdPerfis, IdPermissoes = codigos[0].IdPermissoes },
            new PerfilPermissao { IdPerfis = leitura.IdPerfis, IdPermissoes = codigos[1].IdPermissoes },
            new PerfilPermissao { IdPerfis = desligado.IdPerfis, IdPermissoes = codigos[2].IdPermissoes });
        await c.Db.SaveChangesAsync();

        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, c.Empresa.IdEmpresas); // sem profissional
        await using (var adm = await UsuariosTeste.ContextoAdminAsync())
        {
            adm.AddRange(new UsuarioPerfil { IdUsuarios = usu.IdUsuarios, IdEmpresas = c.Empresa.IdEmpresas, IdPerfis = leitura.IdPerfis },
                         new UsuarioPerfil { IdUsuarios = usu.IdUsuarios, IdEmpresas = c.Empresa.IdEmpresas, IdPerfis = desligado.IdPerfis });
            await adm.SaveChangesAsync();
        }

        var (db, conta) = await NovoAsync();
        await using var _ = db;
        var p = (await conta.MontarIdentidadeAsync(usu.IdUsuarios, c.Empresa.IdEmpresas)).Valor;
        Assert.Equal(c.Empresa.IdEmpresas.ToString(), p.FindFirstValue(LabClaims.EmpresaId));
        Assert.Equal(c.Empresa.CodEmpresa, p.FindFirstValue(LabClaims.EmpresaCodigo));
        Assert.Null(p.FindFirst(LabClaims.ProfissionalId));
        Assert.Null(p.FindFirst(LabClaims.AdminGlobal));
        Assert.Equal(codigos.Take(2).Select(x => x.Codigo).Order(), p.FindAll(LabClaims.Permissao).Select(x => x.Value).Order());

        // sem vínculo: recusado
        await using var outra = await Cenario.CriarAsync();
        Assert.Equal("Você não tem acesso a esta empresa.", (await conta.MontarIdentidadeAsync(usu.IdUsuarios, outra.Empresa.IdEmpresas)).Mensagem);
        // sem empresa: só o usuário (troca de senha / escolha de empresa)
        Assert.Null((await conta.MontarIdentidadeAsync(usu.IdUsuarios)).Valor.FindFirst(LabClaims.EmpresaId));
    }
}
