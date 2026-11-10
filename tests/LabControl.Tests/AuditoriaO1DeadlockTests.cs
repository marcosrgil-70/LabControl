using System.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LabControl.Tests;

/// <summary>
/// Auditoria SABOTADOR da O-1, rodada 2 (09/11/2026): deadlocks entre revisões simultâneas de famílias diferentes.
/// A PR_REVISAO_RESULTADO antiga (UPDATE ... JOIN com "ID_AMOSTRA_RAIZ = v_raiz OR ID = v_raiz") lia as amostras com
/// trava fora da família e esperava a amostra nova de outra revisão em andamento (em carga: 75 deadlocks em 80
/// revisões). Agora a família vem primeiro (derivada pelo FK_AMO_RAIZ) e os resultados pelo UQ_RES_REV.
/// Esperar a trava = falhar (espera curta de 3 s). Proteção contra regressão (passa hoje).
/// </summary>
public class AuditoriaO1DeadlockTests
{
    [Fact]
    public async Task Revisao_de_amostra_nao_espera_revisao_em_andamento_de_outra_empresa()
    {
        await using var x = await Cenario.CriarAsync();
        await using var y = await Cenario.CriarAsync();
        var (amoX, _, _) = await x.AmostraComBoletimAsync(await x.HojeAsync());
        var (amoY, _, _) = await y.AmostraComBoletimAsync(await y.HojeAsync());

        // nos dois sentidos: a ordem dos ids decide qual das varreduras antigas atravessava a família da outra
        foreach (var (segura, revisa) in new[] { (y, x), (x, y) })
        {
            var idSegura = segura == y ? amoY.IdLabHistAmostras : amoX.IdLabHistAmostras;
            var idRevisa = revisa == x ? amoX.IdLabHistAmostras : amoY.IdLabHistAmostras;
            await using (await TransacaoAbertaAsync(
                "SET @LC_REVISAO = 1; CALL PR_REVISAO_AMOSTRA(@id, 'TESTE DE TRAVA', @func, @usu, @n, @r, @rr, @b); SET @LC_REVISAO = NULL",
                ("@id", idSegura), ("@func", segura.Ativa.ProfissionalId), ("@usu", segura.Ativa.UsuarioId)))
            {
                await EsperaCurtaAsync(revisa);
                var rev = await revisa.Procedures.GerarRevisaoAmostraAsync(idRevisa, "LOTE DIGITADO ERRADO");
                Assert.True(rev.Sucesso, rev.Mensagem);
                if (revisa == x) amoX = await x.Db.LabHistAmostras.AsNoTracking().SingleAsync(a => a.IdLabHistAmostras == rev.Valor.IdNova);
                else amoY = await y.Db.LabHistAmostras.AsNoTracking().SingleAsync(a => a.IdLabHistAmostras == rev.Valor.IdNova);
            }
        }
    }

    /// <summary>
    /// 1205 (espera de trava esgotada) vira a mensagem de "outro usuário gravando" e nada fica gravado. Com
    /// innodb_rollback_on_timeout = OFF o MySQL desfaz só o comando: o SaveChanges (transação própria do EF) desfaz o resto.
    /// </summary>
    [Fact]
    public async Task Espera_de_trava_esgotada_vira_mensagem_e_nao_grava()
    {
        await using var c = await Cenario.CriarAsync();
        var id = c.Produto.IdProdutos;
        var antes = await c.Db.Produtos.AsNoTracking().Where(p => p.IdProdutos == id).Select(p => p.Descricao).SingleAsync();

        await using (await TransacaoAbertaAsync("SELECT 1 FROM PRODUTOS WHERE ID_PRODUTOS = @id FOR UPDATE", ("@id", id)))
        {
            await c.Db.Database.OpenConnectionAsync();
            await c.Db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1");
            c.Db.ChangeTracker.Clear();
            var p = await c.Db.Produtos.SingleAsync(x => x.IdProdutos == id);
            p.Descricao = "ALTERADO PELA AUDITORIA";
            var r = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
            Assert.False(r.Sucesso);
            Assert.Equal("Outro usuário estava gravando os mesmos dados ao mesmo tempo e nada foi gravado. Tente de novo.", r.Mensagem);
        }

        await using var limpo = await BancoTeste.NovoContextoAsync(c.Ativa);
        Assert.Equal(antes, await limpo.Produtos.AsNoTracking().Where(p => p.IdProdutos == id).Select(p => p.Descricao).SingleAsync());
    }

    private static async Task EsperaCurtaAsync(Cenario c)
    {
        await c.Db.Database.OpenConnectionAsync();
        await c.Db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 3");
    }

    /// <summary>SQL numa conexão própria, em READ COMMITTED como a aplicação; a transação fica aberta (rollback ao descartar).</summary>
    private static async Task<IAsyncDisposable> TransacaoAbertaAsync(string sql, params (string Nome, object? Valor)[] parametros)
    {
        var con = new MySqlConnection(BancoTeste.ConnectionString + ";Allow User Variables=true");
        await con.OpenAsync();
        var tx = await con.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var cmd = new MySqlCommand(sql, con, tx);
        foreach (var (nome, valor) in parametros) cmd.Parameters.AddWithValue(nome, valor);
        await cmd.ExecuteNonQueryAsync();
        return new TransacaoAberta(con, tx);
    }

    private sealed record TransacaoAberta(MySqlConnection Con, MySqlTransaction Tx) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Tx.RollbackAsync();
            await Con.DisposeAsync();
        }
    }
}
