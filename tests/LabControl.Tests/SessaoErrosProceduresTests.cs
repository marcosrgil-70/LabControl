using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 0.5 (docs/processos/P22-erros-transacoes.md).</summary>
public class SessaoErrosProceduresTests
{
    private sealed record Sessao(long? Usuario, long? Migracao, long? Retroativo, long Conexao, string Isolamento);

    private static Task<Sessao> LerSessaoAsync(Data.LabDbContext db) =>
        db.Database.SqlQueryRaw<Sessao>(
            "SELECT @LC_ID_USUARIOS AS Usuario, @LC_MIGRACAO AS Migracao, @LC_PERMITE_RETROATIVO AS Retroativo, CONNECTION_ID() AS Conexao, @@transaction_isolation AS Isolamento")
          .SingleAsync();

    [Fact]
    public async Task Conexao_do_pool_nao_herda_variaveis_de_outro_request()
    {
        var conexoes = new HashSet<long>();
        await using (var db1 = await BancoTeste.NovoContextoAsync(new EmpresaFixa { UsuarioId = 7 }))
        {
            await db1.Database.OpenConnectionAsync();
            Assert.Equal(7, (await LerSessaoAsync(db1)).Usuario);
            await db1.Database.ExecuteSqlRawAsync("SET @LC_PERMITE_RETROATIVO = 1, @LC_MIGRACAO = 1");
            var s = await LerSessaoAsync(db1);
            Assert.Equal((1L, 1L), (s.Retroativo, s.Migracao));
            conexoes.Add(s.Conexao);
            await db1.Database.CloseConnectionAsync(); // volta para o pool
        }

        // "outro request": usuário 9, conexão (provavelmente a mesma) devolvida pelo pool
        await using var db2 = await BancoTeste.NovoContextoAsync(new EmpresaFixa { UsuarioId = 9 });
        var s2 = await LerSessaoAsync(db2);
        conexoes.Add(s2.Conexao);
        Assert.Equal(9, s2.Usuario);
        Assert.Equal(0, s2.Retroativo);
        Assert.Equal(0, s2.Migracao);
        Assert.Equal("READ-COMMITTED", s2.Isolamento);

        // sem usuário: NULL (nunca o usuário do request anterior)
        await using var db3 = await BancoTeste.NovoContextoAsync(new EmpresaFixa());
        Assert.Null((await LerSessaoAsync(db3)).Usuario);
    }

    [Fact]
    public async Task Trigger_que_recusa_devolve_a_mensagem_exata()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        c.Db.Add(c.NovaAmostra(hoje.AddDays(1)));
        var r = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        Assert.False(r.Sucesso);
        Assert.Equal("Data de entrega maior que a data de hoje.", r.Mensagem);
        c.Db.ChangeTracker.Clear();

        // 1062 traduzido: código de produto repetido na mesma empresa
        c.Db.Add(new Produto { Codigo = c.Produto.Codigo, Descricao = "OUTRO", IdUnidades = c.Unidade.IdUnidades });
        var dup = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        Assert.Equal("Já existe um produto com este código.", dup.Mensagem);
        c.Db.ChangeTracker.Clear();

        // descrição repetida num cadastro auxiliar
        c.Db.Add(new EmbalagemTipo { Descricao = c.Embalagem.Descricao });
        Assert.Equal("Já existe um registro com esta descrição.", (await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync())).Mensagem);
        c.Db.ChangeTracker.Clear();

        // 1451 traduzido: excluir unidade usada por produto
        c.Db.Remove(await c.Db.Unidades.SingleAsync(u => u.IdUnidades == c.Unidade.IdUnidades));
        var emUso = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        Assert.StartsWith("Registro em uso", emUso.Mensagem);
    }

    [Fact]
    public async Task Data_retroativa_so_com_liberacao_na_mesma_transacao()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        // fora de transação o helper recusa (outra conexão perderia a variável)
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Procedures.DefinirPermiteRetroativoAsync(true));

        c.Db.Add(c.NovaAmostra(hoje.AddYears(-1)));
        var sem = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        Assert.Equal("Data de entrega muito antiga (verifique o ano digitado). Requer confirmação e permissão.", sem.Mensagem);
        c.Db.ChangeTracker.Clear();

        var amo = c.NovaAmostra(hoje.AddYears(-1));
        await using (var tx = await c.Db.Database.BeginTransactionAsync())
        {
            await c.Procedures.DefinirPermiteRetroativoAsync(true);
            c.Db.Add(amo);
            await c.Db.SaveChangesAsync();
            Assert.True((await c.Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso);
            await c.Procedures.DefinirPermiteRetroativoAsync(false);
            await tx.CommitAsync();
        }
        Assert.Equal(hoje.Year, amo.AnoAmostra);               // ano do servidor, não o da data digitada
        Assert.Equal($"04-00001-01/{hoje.Year % 100:00}", amo.NrAmostra);
        Assert.Equal(c.Ativa.EmpresaId, amo.IdEmpresas);
    }

    [Fact]
    public async Task Helpers_de_procedure_leem_os_OUT_params_e_devolvem_mensagens_45000()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        var pedido = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "RESPONSÁVEL", null);
        Assert.True(pedido.Sucesso, pedido.Mensagem);
        Assert.Equal(1, pedido.Valor.NrPedido);
        var outro = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, null, null);
        Assert.Equal("Já existe Pedido de Venda para esta proposta.", outro.Mensagem);

        var (amo, res, bol) = await c.AmostraComBoletimAsync(hoje);
        Assert.Equal(pedido.Valor.IdPedido, amo.IdPedidosVenda); // herdado pela trigger, relido pelo EF

        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "boletins/teste.pdf", new string('a', 64))).Sucesso);
        Assert.Equal("Boletim já emitido.",
            (await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "x.pdf", new string('b', 64))).Mensagem);

        var ra = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "TESTE DE REVISÃO DA AMOSTRA");
        Assert.True(ra.Sucesso, ra.Mensagem);
        Assert.Equal(1, ra.Valor.Revisao);
        Assert.NotNull(ra.Valor.IdResultadoNovo);
        Assert.NotNull(ra.Valor.IdBoletimNovo);
        var amo2 = await c.Db.LabHistAmostras.AsNoTracking().SingleAsync(a => a.IdLabHistAmostras == ra.Valor.IdNova);
        Assert.Equal(amo.NrAmostra + " R01", amo2.NrAmostra);
        Assert.Equal(amo.IdLabHistAmostras, amo2.IdAmostraRaiz);

        var rr = await c.Procedures.GerarRevisaoResultadoAsync(ra.Valor.IdResultadoNovo!.Value, "TESTE DE REVISÃO DO RESULTADO");
        Assert.True(rr.Sucesso, rr.Mensagem);
        Assert.Equal(2, rr.Valor.Revisao);
        Assert.NotNull(rr.Valor.IdBoletimNovo);

        // revisar uma revisão antiga: mensagem da procedure
        var antiga = await c.Procedures.GerarRevisaoResultadoAsync(res.IdLabResultadosAnalises, "X");
        Assert.Equal("Só é possível revisar a última revisão do resultado.", antiga.Mensagem);
        Assert.Equal("Informe o motivo da revisão.", (await c.Procedures.GerarRevisaoBoletimAsync(rr.Valor.IdBoletimNovo!.Value, " ")).Mensagem);

        var rb = await c.Procedures.GerarRevisaoBoletimAsync(rr.Valor.IdBoletimNovo!.Value, "TESTE DE REVISÃO DO BOLETIM");
        Assert.Equal(3, rb.Valor.Revisao);
        Assert.True((await c.Procedures.ExcluirRevisaoBoletimAsync(rb.Valor.IdNovo)).Sucesso);

        var rp = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "TESTE DE REVISÃO DA PROPOSTA");
        Assert.True(rp.Sucesso, rp.Mensagem);
        Assert.Equal(1, rp.Valor.Revisao);
        var prop2 = await c.Db.LabPropostas.AsNoTracking().SingleAsync(p => p.IdLabPropostas == rp.Valor.IdNovo);
        Assert.Equal($"000001/{hoje.Year % 100:00}-R1", prop2.NrProposta);
        Assert.Equal(270m, prop2.VrTotalProposta);
        Assert.True((await c.Procedures.AtualizarPedidoRevisaoAsync(pedido.Valor.IdPedido)).Sucesso);

        // já na revisão atual: nada muda e não gera auditoria (revisão da 1.7)
        int Eventos() => c.Db.Auditoria.Count(a => a.Tabela == "PEDIDOS_VENDA" && a.IdRegistro == pedido.Valor.IdPedido.ToString() && a.Acao == "A");
        var antes = Eventos();
        Assert.True((await c.Procedures.AtualizarPedidoRevisaoAsync(pedido.Valor.IdPedido)).Sucesso);
        Assert.Equal(antes, Eventos());
        Assert.NotEqual(0, await c.Db.PedidosVendaItens.CountAsync(i => i.IdPedidosVenda == pedido.Valor.IdPedido));
    }

    [Fact]
    public async Task Procedure_com_transacao_propria_e_recusada_dentro_de_BeginTransaction()
    {
        await using var c = await Cenario.CriarAsync();
        await using var tx = await c.Db.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "DENTRO DE TRANSAÇÃO"));
        // as sem transação própria podem
        Assert.True((await c.Procedures.RecalcularPropostaAsync(c.Proposta.IdLabPropostas)).Sucesso);
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Helper_nao_chama_procedure_para_registro_de_outra_empresa()
    {
        await using var a = await Cenario.CriarAsync();
        var idPropostaA = a.Proposta.IdLabPropostas;

        await using var b = await Cenario.CriarAsync(); // outra empresa
        var r = await b.Procedures.GerarRevisaoPropostaAsync(idPropostaA, "INVASÃO");
        Assert.Equal("Proposta não encontrada.", r.Mensagem);
        Assert.Equal(1, await a.Db.LabPropostas.CountAsync()); // nada foi criado na empresa A

        // usuário sem profissional vinculado: mensagem clara em vez de erro técnico (p_id_func obrigatório)
        var hoje = await b.HojeAsync();
        var amo = b.NovaAmostra(hoje);
        b.Db.Add(amo);
        await b.Db.SaveChangesAsync();
        b.Ativa.ProfissionalId = null;
        Assert.StartsWith("Seu usuário não está vinculado a um profissional",
            (await b.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "SEM PROFISSIONAL")).Mensagem);
    }
}
