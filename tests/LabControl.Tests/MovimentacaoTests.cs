using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 7.1 (T23): saldo protegido, movimento na amostra física, entrada automática fixa e parâmetros atendidos.</summary>
public class MovimentacaoTests
{
    private static MovimentacaoService Servico(Cenario c) => new(c.Db, c.Ativa);

    private static MovimentoNovoVM Saida(int idAmostra, string qtde, params int[] testes) =>
        new() { IdAmostra = idAmostra, Tipo = EntradaSaida.Saida, Qtde = qtde, Justificativa = "CONSUMO PARA ANÁLISE", Testes = [.. testes] };

    [Fact]
    public async Task Saida_maior_que_o_saldo_e_recusada_e_a_valida_baixa_o_saldo()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amo, _, _, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());   // recebida com 3

        var r = await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "5"));
        Assert.Equal("Saldo insuficiente da amostra para esta saída (saldo atual: 3).", r.Mensagem);
        Assert.Equal("Informe a justificativa da saída.", (await s.IncluirAsync(new MovimentoNovoVM { IdAmostra = amo.IdLabHistAmostras, Qtde = "1" })).Mensagem);
        Assert.Contains("maior que zero", (await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "0"))).Mensagem);
        var futura = Saida(amo.IdLabHistAmostras, "1");
        futura.DataMov = (await c.Db.Database.SqlQueryRaw<DateTime>("SELECT NOW() AS Value").SingleAsync()).AddDays(1);
        Assert.Equal("A data da movimentação não pode ser futura.", (await s.IncluirAsync(futura)).Mensagem);

        var ok = await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "1,5"));
        Assert.True(ok.Sucesso, ok.Mensagem);
        var tela = await s.CarregarAsync(amo.IdLabHistAmostras);
        Assert.Equal(1.5m, tela!.Saldo);
        Assert.Equal(2, tela.Movimentos.Count);
        Assert.Equal(c.Ativa.ProfissionalId, (await c.Db.LabMovAmostras.SingleAsync(m => m.IdLabMovAmostras == ok.Valor)).IdEntidadesFunc);
    }

    [Fact]
    public async Task Movimento_na_revisao_vai_para_a_amostra_fisica_com_saldo_unico()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amo, _, _) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var rev = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "LOTE ERRADO");
        Assert.True(rev.Sucesso, rev.Mensagem);

        var r = await s.IncluirAsync(Saida(rev.Valor.IdNova, "2"));
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal(amo.IdLabHistAmostras, (await c.Db.LabMovAmostras.SingleAsync(m => m.IdLabMovAmostras == r.Valor)).IdLabHistAmostras);
        var raiz = await s.CarregarAsync(amo.IdLabHistAmostras);
        var revisao = await s.CarregarAsync(rev.Valor.IdNova);
        Assert.Equal(1m, raiz!.Saldo);
        Assert.Equal(1m, revisao!.Saldo);
        Assert.Equal(raiz.Movimentos.Select(m => m.Id), revisao.Movimentos.Select(m => m.Id));
        Assert.False(raiz.UltimaRevisao);

        // a busca mostra só a última revisão, com o saldo da física
        var lista = await s.ListarAsync(new MovimentacaoFiltroVM { Numero = revisao.NrAmostra });
        Assert.Equal(rev.Valor.IdNova, Assert.Single(lista.Itens).Id);
        Assert.Equal(1m, lista.Itens[0].Saldo);
        Assert.Empty((await s.ListarAsync(new MovimentacaoFiltroVM())).Itens);   // sem filtro não lista (RN-06.07)
    }

    [Fact]
    public async Task Entrada_automatica_nao_sai_e_entrada_que_cobre_saida_tambem_nao()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amo, _, _, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        var automatica = await c.Db.LabMovAmostras.SingleAsync(m => m.IdLabHistAmostras == amo.IdLabHistAmostras);
        Assert.True(automatica.Automatica);
        Assert.Equal("A entrada automática do recebimento não pode ser excluída.", (await s.ExcluirAsync(automatica.IdLabMovAmostras)).Mensagem);

        var entrada = await s.IncluirAsync(new MovimentoNovoVM { IdAmostra = amo.IdLabHistAmostras, Tipo = EntradaSaida.Entrada, Qtde = "2" });
        Assert.True(entrada.Sucesso, entrada.Mensagem);   // entrada complementar sem justificativa
        var saida = await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "4"));
        Assert.True(saida.Sucesso, saida.Mensagem);       // saldo 1

        Assert.Equal("Esta entrada não pode ser excluída: o saldo da amostra ficaria negativo (saldo atual: 1).",
            (await s.ExcluirAsync(entrada.Valor)).Mensagem);
        Assert.True((await s.ExcluirAsync(saida.Valor)).Sucesso);
        Assert.True((await s.ExcluirAsync(entrada.Valor)).Sucesso);
        Assert.Equal(3m, (await s.CarregarAsync(amo.IdLabHistAmostras))!.Saldo);
        Assert.Equal(2, await c.Db.Auditoria.CountAsync(a => a.Tabela == "LAB_MOV_AMOSTRAS" && a.Acao == LabControl.Data.AcoesAuditoria.Exclusao));
    }

    [Fact]
    public async Task Parametros_atendidos_gravados_e_marcados_como_ja_atendidos()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var (amo, _, _, _) = await c.AmostraComResultadoAsync(hoje);
        var (outra, _, _, _) = await c.AmostraComResultadoAsync(hoje);
        var tela = await s.CarregarAsync(amo.IdLabHistAmostras);
        Assert.Equal(2, tela!.Parametros.Count);
        Assert.All(tela.Parametros, p => Assert.False(p.JaAtendido));
        var teste = tela.Parametros.Single(p => p.Parametro == c.ParamAcreditado.Descricao).IdTeste;

        var testeDaOutra = await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == outra.IdLabHistAmostras).Select(t => t.IdLabHistAmostrasTestes).FirstAsync();
        Assert.Equal("Parâmetro que não é desta amostra.", (await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "1", testeDaOutra))).Mensagem);
        Assert.Equal("Os parâmetros atendidos são indicados só na saída.",
            (await s.IncluirAsync(new MovimentoNovoVM { IdAmostra = amo.IdLabHistAmostras, Tipo = EntradaSaida.Entrada, Qtde = "1", Testes = [teste] })).Mensagem);

        var r = await s.IncluirAsync(Saida(amo.IdLabHistAmostras, "1", teste, teste));   // repetido conta uma vez
        Assert.True(r.Sucesso, r.Mensagem);
        tela = await s.CarregarAsync(amo.IdLabHistAmostras);
        Assert.Equal([c.ParamAcreditado.Descricao], tela!.Movimentos.Single(m => m.Id == r.Valor).Parametros);
        Assert.True(tela.Parametros.Single(p => p.IdTeste == teste).JaAtendido);
        Assert.False(tela.Parametros.Single(p => p.IdTeste != teste).JaAtendido);

        // excluir a saída leva os parâmetros junto
        Assert.True((await s.ExcluirAsync(r.Valor)).Sucesso);
        Assert.False(await c.Db.LabMovAmostrasParam.AnyAsync(p => p.IdLabMovAmostras == r.Valor));
    }

    [Fact]
    public async Task Duas_saidas_ao_mesmo_tempo_nao_deixam_o_saldo_negativo()
    {
        await using var c = await Cenario.CriarAsync();
        var (amo, _, _, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());   // saldo 3

        var resultados = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            var ativa = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, ProfissionalId = c.Ativa.ProfissionalId };
            await using var db = await BancoTeste.NovoContextoAsync(ativa);
            return await new MovimentacaoService(db, ativa).IncluirAsync(Saida(amo.IdLabHistAmostras, "2"));
        }));

        Assert.Single(resultados, r => r.Sucesso);
        Assert.All(resultados.Where(r => !r.Sucesso), r => Assert.StartsWith("Saldo insuficiente", r.Mensagem));
        Assert.Equal(1m, await c.Db.LabHistAmostrasSaldo.Where(x => x.IdLabHistAmostras == amo.IdLabHistAmostras).Select(x => x.SaldoAtual).SingleAsync());
    }
}
