using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LabControl.Tests;

/// <summary>Numeração pelo banco sob gravações simultâneas (vários usuários ao mesmo tempo).</summary>
public class ConcorrenciaTests
{
    private const int Paralelos = 8, PorTarefa = 10;

    [Fact]
    public async Task Numeracao_simultanea_na_mesma_empresa_nao_trava_nem_repete()
    {
        await using var c = await Cenario.CriarAsync();
        var erros = new List<string>();

        await Task.WhenAll(Enumerable.Range(0, Paralelos).Select(async t =>
        {
            var ativa = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, ProfissionalId = c.Ativa.ProfissionalId };
            await using var db = await BancoTeste.NovoContextoAsync(ativa);
            for (var i = 0; i < PorTarefa; i++)
            {
                db.Add(new Produto { Descricao = $"P{t}-{i}", IdUnidades = c.Unidade.IdUnidades });   // SP_PROXIMO_NUMERO('PRODUTO')
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { lock (erros) erros.Add(ErrosMySql.Encontrar(ex)?.Message ?? ex.Message); db.ChangeTracker.Clear(); }
            }
        }));

        var codigos = await c.Db.Produtos.Select(p => p.Codigo).ToListAsync();
        Assert.True(erros.Count == 0, $"{erros.Count} falhas:\n" + string.Join("\n", erros.Distinct()));
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
        Assert.Equal(1 + Paralelos * PorTarefa, codigos.Count);
    }

    [Fact]
    public async Task Primeiro_item_de_propostas_diferentes_ao_mesmo_tempo_nao_trava()
    {
        await using var c = await Cenario.CriarAsync();
        var erros = new List<string>();
        var modelo = await c.Db.LabPropostas.AsNoTracking().SingleAsync();

        await Task.WhenAll(Enumerable.Range(0, Paralelos).Select(async t =>
        {
            var ativa = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, ProfissionalId = c.Ativa.ProfissionalId };
            await using var db = await BancoTeste.NovoContextoAsync(ativa);
            for (var i = 0; i < PorTarefa; i++)
            {
                try
                {
                    var p = new LabProposta
                    {
                        IdEntidades = modelo.IdEntidades, IdEntidadesFunc = modelo.IdEntidadesFunc, IdEntEndLaudo = modelo.IdEntEndLaudo,
                        IdEntContatoResultado = modelo.IdEntContatoResultado, IdEntCobranca = modelo.IdEntCobranca,
                        IdLabPropostasStatus = modelo.IdLabPropostasStatus, IdCondicoesPagtos = modelo.IdCondicoesPagtos,
                    };
                    db.Add(p);
                    await db.SaveChangesAsync();                                   // SP_PROXIMO_NUMERO('PROPOSTA')
                    db.Add(new LabPropostaProduto { IdLabPropostas = p.IdLabPropostas, IdProdutos = c.Produto.IdProdutos, QtdeAmostras = 1 });
                    await db.SaveChangesAsync();                                   // ITEM = MAX + 1 (TG_PROPPROD_BI)
                }
                catch (Exception ex) { lock (erros) erros.Add(ErrosMySql.Encontrar(ex)?.Message ?? ex.Message); db.ChangeTracker.Clear(); }
            }
        }));

        Assert.True(erros.Count == 0, $"{erros.Count} falhas:\n" + string.Join("\n", erros.Distinct()));
        var numeros = await c.Db.LabPropostas.Select(p => p.CodProposta).ToListAsync();
        Assert.Equal(numeros.Count, numeros.Distinct().Count());
    }

    /// <summary>
    /// 6.3 (D-23): emissão de boletim e revisão de resultado de pedidos/empresas diferentes não esperam uma pela outra.
    /// Antes, o filtro por COALESCE(ID_AMOSTRA_RAIZ, ...) sem índice e o SET v = EXISTS(...) liam a tabela inteira de
    /// amostras com trava, e as duas rotinas davam deadlock com testes rodando em paralelo.
    /// </summary>
    [Fact]
    public async Task Emissao_e_revisao_de_resultado_de_pedidos_diferentes_nao_se_travam()
    {
        await using var x = await Cenario.CriarAsync();
        await using var y = await Cenario.CriarAsync();
        var (_, _, bolX) = await x.AmostraComBoletimAsync(await x.HojeAsync());
        var idPedidoX = await GerarPedidoAsync(x);
        var (_, resY, _) = await y.AmostraComBoletimAsync(await y.HojeAsync());

        // revisão de resultado da empresa Y aberta (sem commit) × emissão do boletim da empresa X
        await using (await TransacaoAbertaAsync(
            "SET @LC_REVISAO = 1; CALL PR_REVISAO_RESULTADO(@id, NULL, 'TESTE DE TRAVA', @func, @usu, 0, @n, @r, @b)",
            ("@id", resY.IdLabResultadosAnalises), ("@func", y.Ativa.ProfissionalId), ("@usu", y.Ativa.UsuarioId)))
        {
            await EsperaCurtaAsync(x);
            var emissao = await x.Procedures.EmitirBoletimAsync(bolX.IdLabBoletimAnalises, "boletins/x.pdf", new string('a', 64));
            Assert.True(emissao.Sucesso, emissao.Mensagem);
        }

        // situação do pedido X calculada (sem commit) × revisão de resultado da empresa Y
        await using (await TransacaoAbertaAsync("CALL PR_ATUALIZAR_SITUACAO_PEDIDO(@id)", ("@id", idPedidoX)))
        {
            await EsperaCurtaAsync(y);
            var revisao = await y.Procedures.GerarRevisaoResultadoAsync(resY.IdLabResultadosAnalises, "CORRIGIR UNIDADE", gerarBoletim: false);
            Assert.True(revisao.Sucesso, revisao.Mensagem);
        }
    }

    /// <summary>
    /// 6.3 (D-23): desfazer a revisão do boletim trava só o boletim. Antes o JOIN no FOR UPDATE travava também a
    /// amostra, na ordem inversa da revisão de resultado (boletim → amostra × amostra → boletins): risco de deadlock.
    /// </summary>
    [Fact]
    public async Task Desfazer_revisao_do_boletim_nao_trava_a_amostra()
    {
        await using var c = await Cenario.CriarAsync();
        var (amo, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var rev = await c.Procedures.GerarRevisaoBoletimAsync(bol.IdLabBoletimAnalises, "CORRIGIR UNIDADE");
        Assert.True(rev.Sucesso, rev.Mensagem);

        // outra transação segura a amostra (como a revisão de resultado, que trava a amostra raiz primeiro)
        await using (await TransacaoAbertaAsync("SELECT 1 FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = @id FOR UPDATE",
                         ("@id", amo.IdLabHistAmostras)))
        {
            await EsperaCurtaAsync(c);
            var desfazer = await c.Procedures.ExcluirRevisaoBoletimAsync(rev.Valor.IdNovo);
            Assert.True(desfazer.Sucesso, desfazer.Mensagem);
        }
    }

    private static async Task<int> GerarPedidoAsync(Cenario c)
    {
        var s = new PedidoVendaService(c.Db, c.Procedures, new PropostaService(c.Db, c.Ativa, c.Procedures), new ServicoAuditoria(c.Db, c.Ativa));
        var r = await s.GerarAsync(new PedidoVendaGerarVM { IdProposta = c.Proposta.IdLabPropostas, DtAutorizacao = await c.HojeAsync() });
        Assert.True(r.Sucesso, r.Mensagem);
        return r.Valor.IdPedido;
    }

    /// <summary>Mantém a conexão do contexto aberta com espera de trava de 3 s (em vez de 50 s): esperar = falhar.</summary>
    private static async Task EsperaCurtaAsync(Cenario c)
    {
        await c.Db.Database.OpenConnectionAsync();
        await c.Db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 3");
    }

    /// <summary>Executa o SQL numa conexão própria e devolve a transação aberta (rollback ao descartar).</summary>
    private static async Task<IAsyncDisposable> TransacaoAbertaAsync(string sql, params (string Nome, object? Valor)[] parametros)
    {
        var con = new MySqlConnection(BancoTeste.ConnectionString + ";Allow User Variables=true");
        await con.OpenAsync();
        var tx = await con.BeginTransactionAsync();
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
