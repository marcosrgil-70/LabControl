using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Achados da revisão da Fase 0 (tarefa 0.9) — um teste por correção.</summary>
public class RevisaoFase0Tests
{
    [Fact]
    public async Task Procedure_grava_a_auditoria_na_propria_transacao_com_usuario_e_ip()
    {
        await using var c = await Cenario.CriarAsync();
        var (_, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "b.pdf", new string('a', 64))).Sucesso);
        var rev = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "MOTIVO");
        Assert.True(rev.Sucesso, rev.Mensagem);

        var a = await c.Db.Auditoria.AsNoTracking().SingleAsync(x => x.Tabela == "LAB_PROPOSTAS" && x.IdRegistro == rev.Valor.IdNovo.ToString());
        Assert.Equal((AcoesAuditoria.Revisao, c.Ativa.EmpresaId, c.Ativa.UsuarioId, "127.0.0.1"), (a.Acao, a.IdEmpresas, a.IdUsuarios, a.Ip));

        // recusa (45000) dentro da transação da procedure = ROLLBACK: nenhum evento
        var antes = await c.Db.Auditoria.CountAsync();
        Assert.Equal("Boletim já emitido.", (await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "c.pdf", new string('b', 64))).Mensagem);
        Assert.Equal(antes, await c.Db.Auditoria.CountAsync());
    }

    [Fact]
    public async Task Evento_de_auditoria_nao_grava_alteracoes_pendentes_do_servico()
    {
        await using var c = await Cenario.CriarAsync();
        var prop = await c.Db.LabPropostas.SingleAsync(p => p.IdLabPropostas == c.Proposta.IdLabPropostas);
        prop.PorcDesconto = 50; // pendente, ainda não salva

        await new ServicoAuditoria(c.Db, c.Ativa).RegistrarAsync(AcoesAuditoria.Login, "USUARIOS", c.Ativa.UsuarioId.ToString()!);

        Assert.Equal(EntityState.Modified, c.Db.Entry(prop).State);
        Assert.Equal(10m, await c.Db.LabPropostas.Where(p => p.IdLabPropostas == prop.IdLabPropostas).Select(p => p.PorcDesconto).SingleAsync());
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Acao == AcoesAuditoria.Login && a.IdRegistro == c.Ativa.UsuarioId.ToString()));
    }

    [Fact]
    public async Task Rastreador_so_e_aceito_depois_do_commit()
    {
        await using var c = await Cenario.CriarAsync();
        var prop = await c.Db.LabPropostas.SingleAsync(p => p.IdLabPropostas == c.Proposta.IdLabPropostas);

        // acceptAllChangesOnSuccess: false agora é suportado em tabela auditada: grava e mantém a entidade Modified
        prop.DtValidade = prop.DtSolicitacao.AddDays(15);
        await c.Db.SaveChangesAsync(acceptAllChangesOnSuccess: false);
        Assert.Equal(EntityState.Modified, c.Db.Entry(prop).State);
        c.Db.ChangeTracker.AcceptAllChanges();

        // padrão: aceito depois de gravar alteração + auditoria
        prop.DtValidade = prop.DtSolicitacao.AddDays(20);
        await c.Db.SaveChangesAsync();
        Assert.Equal(EntityState.Unchanged, c.Db.Entry(prop).State);
        Assert.Equal(3, await c.Db.Auditoria.CountAsync(a => a.Tabela == "LAB_PROPOSTAS" && a.IdRegistro == prop.IdLabPropostas.ToString()));
    }

    [Fact]
    public async Task Filho_em_pai_de_outra_empresa_e_recusado_pelo_banco()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();

        // o SaveChanges grava a empresa ativa (B); a trigger confere com a empresa do pai (A)
        b.Db.Add(new LabPropostaProduto { IdLabPropostas = a.Proposta.IdLabPropostas, IdProdutos = b.Produto.IdProdutos, QtdeAmostras = 1 });
        var r = await ErrosMySql.CapturarAsync(() => b.Db.SaveChangesAsync());
        Assert.Equal("Registro pertence a outra empresa.", r.Mensagem);
        Assert.Equal(1, await a.Db.LabPropostasProdutos.CountAsync());
    }

    [Fact]
    public async Task Vinculo_de_usuario_com_outra_empresa_so_por_admin_global()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var perfilB = await b.Db.Perfis.FirstAsync();

        a.Db.Add(new UsuarioEmpresa { IdUsuarios = a.Ativa.UsuarioId!.Value, IdEmpresas = b.Empresa.IdEmpresas });
        Assert.Contains("ADMIN_GLOBAL", (await Assert.ThrowsAsync<InvalidOperationException>(() => a.Db.SaveChangesAsync())).Message);
        a.Db.ChangeTracker.Clear();

        a.Db.Add(new UsuarioPerfil { IdUsuarios = a.Ativa.UsuarioId!.Value, IdEmpresas = b.Empresa.IdEmpresas, IdPerfis = perfilB.IdPerfis });
        await Assert.ThrowsAsync<InvalidOperationException>(() => a.Db.SaveChangesAsync());
        a.Db.ChangeTracker.Clear();

        // na empresa ativa, ou como ADMIN_GLOBAL, grava
        a.Db.Add(new UsuarioEmpresa { IdUsuarios = a.Ativa.UsuarioId!.Value, IdEmpresas = a.Empresa.IdEmpresas });
        await a.Db.SaveChangesAsync();
        a.Ativa.AdminGlobal = true;
        a.Db.Add(new UsuarioEmpresa { IdUsuarios = a.Ativa.UsuarioId!.Value, IdEmpresas = b.Empresa.IdEmpresas });
        await a.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Sem_empresa_ativa_linhas_sem_empresa_nao_aparecem()
    {
        var semEmpresa = new EmpresaFixa();
        await using var db = await BancoTeste.NovoContextoAsync(semEmpresa);
        var id = Guid.NewGuid().ToString("N");
        await new ServicoAuditoria(db, semEmpresa).RegistrarAsync(AcoesAuditoria.Login, "USUARIOS", id); // ID_EMPRESAS NULL

        Assert.False(await db.Auditoria.AnyAsync(a => a.IdRegistro == id));
        Assert.True(await db.Auditoria.IgnoreQueryFilters().AnyAsync(a => a.IdRegistro == id));
    }

    [Fact]
    public async Task Inicializar_empresa_exige_admin_global()
    {
        await using var c = await Cenario.CriarAsync();
        Assert.Equal("Somente o administrador global pode inicializar uma empresa.",
            (await c.Procedures.InicializarEmpresaAsync(c.Empresa.IdEmpresas)).Mensagem);
        c.Ativa.AdminGlobal = true;
        Assert.True((await c.Procedures.InicializarEmpresaAsync(c.Empresa.IdEmpresas)).Sucesso); // idempotente
    }
}
