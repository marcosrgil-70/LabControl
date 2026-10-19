using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Roteiro do doc 12 §7 (docs/banco/12-PROCEDURES-REVISOES.md) pela aplicação: gravações pelo EF, procedures por
/// <see cref="Procedures"/>, erros por <see cref="ErrosMySql"/>. O mesmo roteiro em SQL puro está em
/// Scripts/v2/teste/roteiro_12_7.sql (rodado por recriar.ps1 -Teste). Um teste por passo (ou grupo de passos
/// dependentes), cada um na própria empresa.
/// </summary>
public class RoteiroTests
{
    [Fact]
    public async Task Passo_01_inicializar_empresa()
    {
        await using var c = await Cenario.CriarAsync(); // CriarEmpresaAsync chama Procedures.InicializarEmpresaAsync
        var db = c.Db;

        Assert.Equal((4, 3, 3, 3), (await db.AmostrasStatus.CountAsync(), await db.AnalisesStatus.CountAsync(),
            await db.BoletinsStatus.CountAsync(), await db.LabPropostasStatus.CountAsync()));
        Assert.Equal((3, 2), (await db.Idiomas.CountAsync(), await db.Moedas.CountAsync()));

        var admin = await db.Perfis.SingleAsync(p => p.Nome == "ADMINISTRADOR");
        Assert.Equal(await db.Permissoes.CountAsync(), await db.PerfisPermissoes.CountAsync(pp => pp.IdPerfis == admin.IdPerfis));

        var par = await db.EmpresasParametros.SingleAsync();
        Assert.All(new[] { par.IdAmostrasStatusInicial, par.IdLabPropostasStatusInicial, par.IdLabPropostasStatusEnvcli,
                           par.IdBoletinsStatusInicial, par.IdBoletinsStatusEmitido }, id => Assert.NotNull(id));
    }

    [Fact]
    public async Task Passos_02_03_proposta_numerada_com_totais_e_produto_com_codigo_automatico()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        Assert.Equal("000001", c.Produto.Codigo);

        var prop = await c.Db.LabPropostas.AsNoTracking().SingleAsync(p => p.IdLabPropostas == c.Proposta.IdLabPropostas);
        Assert.Equal($"000001/{hoje.Year % 100:00}-R0", prop.NrProposta);
        Assert.Equal((300m, 30m, 270m), (prop.VrSomaVrTotAnalis, prop.VrDesconto, prop.VrTotalProposta));
    }

    [Fact]
    public async Task Passos_04_05_pedido_e_amostra_numerada_pelo_servidor()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        var ped = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "RESPONSÁVEL DO CLIENTE", null);
        Assert.True(ped.Sucesso, ped.Mensagem);
        Assert.Equal(1, ped.Valor.NrPedido);

        // ano/número enviados pela tela são ignorados: o banco usa o ano do servidor e a próxima sequência
        var amo = c.NovaAmostra(hoje);
        amo.AnoAmostra = 1999;
        amo.CodAmostra = 999;
        amo.ValidadeMes = 5;
        amo.ValidadeAno = (short)(hoje.Year + 1);
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso);

        Assert.Equal($"04-00001-01/{hoje.Year % 100:00}", amo.NrAmostra);
        Assert.Equal((hoje.Year, 1), (amo.AnoAmostra, amo.CodAmostra));
        Assert.Equal(ped.Valor.IdPedido, amo.IdPedidosVenda); // passo 4: herda o pedido
        Assert.Equal($"05/{hoje.Year + 1}", await Funcao<string>(c, $"SELECT FN_DATA_PARCIAL(NULL, 5, {hoje.Year + 1}) AS Value"));
        Assert.Equal(3m, (await c.Db.LabHistAmostrasSaldo.SingleAsync(s => s.IdLabHistAmostras == amo.IdLabHistAmostras)).SaldoAtual);
        Assert.Equal(2, await Funcao<int>(c, $"SELECT FN_ACREDITADO({amo.IdLabHistAmostras}) AS Value"));

        var prazos = await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == amo.IdLabHistAmostras).Select(t => t.DtPrevista).ToListAsync();
        Assert.Equal([hoje.AddDays(10), hoje.AddDays(10)], prazos);
    }

    [Fact]
    public async Task Passos_06_07_resultado_formatado_e_boletim_emitido()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        var (amo, res, bol) = await c.AmostraComBoletimAsync(hoje);
        var formatados = await c.Db.LabResultadosParam.Where(r => r.IdLabResultadosAnalises == res.IdLabResultadosAnalises)
            .OrderBy(r => r.IdLabResultadosParam).Select(r => r.ResultadoFormatado).ToListAsync();
        Assert.Equal(["< 1,0e2 UFC/g", "Ausência em 25 g"], formatados);

        Assert.Equal(amo.NrAmostra, bol.CodBoletim);
        Assert.Equal("CONT. TOTAL: ISO 4833", await Funcao<string>(c, $"SELECT FN_OBS_METODOS({amo.IdLabHistAmostras}, 1) AS Value"));

        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "boletins/teste.pdf", new string('a', 64))).Sucesso);
        await c.Db.Entry(bol).ReloadAsync();
        Assert.NotNull(bol.DhEmissao);
    }

    [Fact]
    public async Task Passos_08_09_revisao_da_amostra_e_do_resultado()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var (amo, res, bol) = await c.AmostraComBoletimAsync(hoje);
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "boletins/teste.pdf", new string('a', 64))).Sucesso);

        // 8. amostra R01 + resultado rev 1 + boletim rev 1 (não emitido, 1 parâmetro apontando para o resultado novo)
        var ra = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "TESTE DE REVISÃO DA AMOSTRA");
        Assert.True(ra.Sucesso, ra.Mensagem);
        var db = c.Db;
        db.ChangeTracker.Clear();
        var amo2 = await db.LabHistAmostras.SingleAsync(a => a.IdLabHistAmostras == ra.Valor.IdNova);
        Assert.Equal((amo.NrAmostra + " R01", amo.IdLabHistAmostras, 1), (amo2.NrAmostra, amo2.IdAmostraRaiz, amo2.Revisao));

        var res2 = await db.LabResultadosAnalises.SingleAsync(r => r.IdLabResultadosAnalises == ra.Valor.IdResultadoNovo);
        Assert.Equal((1, amo2.IdLabHistAmostras), (res2.Revisao, res2.IdLabHistAmostras));

        var bol2 = await db.LabBoletimAnalises.SingleAsync(b => b.IdLabBoletimAnalises == ra.Valor.IdBoletimNovo);
        Assert.Equal((1, (DateTime?)null, amo.NrAmostra), (bol2.Revisao, bol2.DhEmissao, bol2.CodBoletim));
        var paramsBol2 = await db.LabBoletimParametros.Where(p => p.IdLabBoletimAnalises == bol2.IdLabBoletimAnalises)
            .Select(p => p.LabResultadoParam.IdLabResultadosAnalises).ToListAsync();
        Assert.Equal([res2.IdLabResultadosAnalises], paramsBol2);

        Assert.False((await db.LabHistAmostras.SingleAsync(a => a.IdLabHistAmostras == amo.IdLabHistAmostras)).UltimaRevisao);
        Assert.False((await db.LabResultadosAnalises.SingleAsync(r => r.IdLabResultadosAnalises == res.IdLabResultadosAnalises)).UltimaRevisao);
        Assert.False((await db.LabBoletimAnalises.SingleAsync(b => b.IdLabBoletimAnalises == bol.IdLabBoletimAnalises)).UltimaRevisao);

        // a revisão não duplica o estoque: movimentos só na raiz, saldo 3
        Assert.False(await db.LabMovAmostras.AnyAsync(m => m.IdLabHistAmostras == amo2.IdLabHistAmostras));
        Assert.Equal(3m, (await db.LabHistAmostrasSaldo.SingleAsync(s => s.IdLabHistAmostras == amo.IdLabHistAmostras)).SaldoAtual);

        // 9. resultado rev 2 + boletim rev 2 apontando para ele
        var rr = await c.Procedures.GerarRevisaoResultadoAsync(res2.IdLabResultadosAnalises, "TESTE DE REVISÃO DO RESULTADO");
        Assert.True(rr.Sucesso, rr.Mensagem);
        Assert.Equal(2, rr.Valor.Revisao);
        var bol3 = await db.LabBoletimAnalises.SingleAsync(b => b.IdLabBoletimAnalises == rr.Valor.IdBoletimNovo);
        Assert.Equal((2, rr.Valor.IdNovo), (bol3.Revisao, bol3.IdLabResultadosAnalises));
    }

    [Fact]
    public async Task Passo_10_revisao_da_proposta()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();

        var rp = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "TESTE DE REVISÃO DA PROPOSTA");
        Assert.True(rp.Sucesso, rp.Mensagem);
        var prop2 = await c.Db.LabPropostas.AsNoTracking().SingleAsync(p => p.IdLabPropostas == rp.Valor.IdNovo);
        Assert.Equal($"000001/{hoje.Year % 100:00}-R1", prop2.NrProposta);
        Assert.Equal(270m, prop2.VrTotalProposta);
        Assert.Equal(2, await c.Db.LabPropostasProdanalises.CountAsync(a => a.IdLabPropostas == prop2.IdLabPropostas && a.StRevAnterior));
    }

    [Fact]
    public async Task Passos_11_12_travas_e_entrega_retroativa_liberada()
    {
        await using var c = await Cenario.CriarAsync();
        var db = c.Db;
        var hoje = await c.HojeAsync();
        var (amo, _, bol) = await c.AmostraComBoletimAsync(hoje);
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "boletins/teste.pdf", new string('a', 64))).Sucesso);
        var ra = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "TESTE DE REVISÃO DA AMOSTRA");
        var rp = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "TESTE DE REVISÃO DA PROPOSTA");
        Assert.True(ra.Sucesso && rp.Sucesso, ra.Mensagem ?? rp.Mensagem);
        Assert.True((await c.Procedures.GerarPedidoVendaAsync(rp.Valor.IdNovo, hoje, null, null)).Sucesso);
        db.ChangeTracker.Clear();

        // 11. travas: todas recusadas pelo banco com a mensagem de negócio
        await Recusada(c, "Este boletim foi emitido. Não é permitido alterar ou excluir.", async () =>
            (await db.LabBoletimAnalises.SingleAsync(b => b.IdLabBoletimAnalises == bol.IdLabBoletimAnalises)).TxtObservacao = "X");
        await Recusada(c, "Revisão antiga da amostra é somente leitura.", async () =>
            (await db.LabHistAmostras.SingleAsync(a => a.IdLabHistAmostras == amo.IdLabHistAmostras)).NrLote = "X");
        await Recusada(c, "Revisão antiga da proposta é somente leitura.", async () =>
            (await db.LabPropostasProdanalises.FirstAsync(a => a.IdLabPropostas == c.Proposta.IdLabPropostas)).VrUnitario = 1);
        await Recusada(c, "Saldo insuficiente da amostra para esta saída.", () =>
        {
            db.Add(new LabMovAmostra { IdLabHistAmostras = ra.Valor.IdNova, Qtde = 10, ES = EntradaSaida.Saida, IdEntidadesFunc = c.Ativa.ProfissionalId!.Value });
            return Task.CompletedTask;
        });
        Assert.Equal("Já existe Pedido de Venda para esta proposta.",
            (await c.Procedures.GerarPedidoVendaAsync(rp.Valor.IdNovo, hoje, null, null)).Mensagem);

        var item2 = await db.LabPropostasProdutos.SingleAsync(i => i.IdLabPropostas == rp.Valor.IdNovo);
        LabHistAmostra AmostraDaRevisao(DateOnly entrega)
        {
            var a = c.NovaAmostra(entrega);
            (a.IdLabPropostas, a.IdLabPropostasProdutos, a.QtdeEmbalagensEntregue) = (rp.Valor.IdNovo, item2.IdLabPropostasProdutos, 1);
            return a;
        }
        await Recusada(c, "Data de validade inválida.", () =>
        {
            var a = AmostraDaRevisao(hoje);
            (a.ValidadeDia, a.ValidadeMes, a.ValidadeAno) = (31, 2, (short)(hoje.Year + 1));
            db.Add(a);
            return Task.CompletedTask;
        });
        await Recusada(c, "Data de entrega muito antiga (verifique o ano digitado). Requer confirmação e permissão.", () =>
        {
            db.Add(AmostraDaRevisao(hoje.AddYears(-1)));
            return Task.CompletedTask;
        });

        // 12. com liberação: ano do servidor e sequência contínua (as recusas acima não consomem número)
        var ultimo = await db.LabHistAmostras.Where(a => a.AnoAmostra == hoje.Year).MaxAsync(a => a.CodAmostra);
        var retro = AmostraDaRevisao(hoje.AddYears(-1));
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await c.Procedures.DefinirPermiteRetroativoAsync(true);
            db.Add(retro);
            await db.SaveChangesAsync();
            await c.Procedures.DefinirPermiteRetroativoAsync(false);
            await tx.CommitAsync();
        }
        Assert.Equal((hoje.Year, ultimo + 1), (retro.AnoAmostra, retro.CodAmostra));
    }

    /// <summary>Executa a alteração, grava e espera a recusa do banco com a mensagem exata; limpa o rastreador depois.</summary>
    private static async Task Recusada(Cenario c, string mensagem, Func<Task> alterar)
    {
        await alterar();
        var r = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        c.Db.ChangeTracker.Clear();
        Assert.Equal(mensagem, r.Mensagem);
    }

    private static Task<T> Funcao<T>(Cenario c, string sql) => c.Db.Database.SqlQueryRaw<T>(sql).SingleAsync();
}
