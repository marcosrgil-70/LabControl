using System.Text;
using LabControl.Models;
using LabControl.Services.Relatorios;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 8.1 (T30-01…06, P20): período obrigatório, revisões que não duplicam (pedido, amostra, resultado),
/// "somente com pedido" (dúvida 9), dias inclusivos, totais, PDF e CSV do mesmo conjunto de dados.
/// </summary>
public class RelatoriosTests
{
    private static RelatoriosService Servico(Cenario c) => new(c.Db, c.Ativa);

    private static async Task<RelatorioDados> GerarAsync(Cenario c, string nome, RelatorioFiltroVM f)
    {
        var r = await Servico(c).GerarAsync(RelatoriosService.Definicao(nome)!, f);
        Assert.True(r.Sucesso, r.Mensagem);
        return r.Valor;
    }

    private static async Task<RelatorioFiltroVM> HojeAsync(Cenario c)
    {
        var hoje = await c.HojeAsync();
        return new RelatorioFiltroVM { De = hoje, Ate = hoje, Visualizar = true };
    }

    private static IEnumerable<LinhaRelatorio> Linhas(RelatorioDados r, TipoLinha tipo, int tabela = 0) => r.Tabelas[tabela].Linhas.Where(l => l.Tipo == tipo);

    /// <summary>Amostra com resultado + revisão da amostra (cascata: amostra, resultado e boletim novos) + pedido de venda.</summary>
    private static async Task<(LabHistAmostra Amostra, int IdPedido)> AmostraRevisadaComPedidoAsync(Cenario c)
    {
        var hoje = await c.HojeAsync();
        var (amo, _, _) = await c.AmostraComBoletimAsync(hoje);
        var pedido = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "MARIA", null);
        Assert.True(pedido.Sucesso, pedido.Mensagem);
        Assert.True((await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "LOTE ERRADO")).Sucesso);
        return (amo, pedido.Valor.IdPedido);
    }

    [Fact]
    public async Task Periodo_obrigatorio_e_fim_nao_antes_do_inicio()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        foreach (var d in RelatoriosService.Catalogo)
        {
            Assert.Equal(RelatoriosService.PeriodoInvalido, (await Servico(c).GerarAsync(d, new RelatorioFiltroVM { De = hoje })).Mensagem);
            Assert.Equal(RelatoriosService.PeriodoInvalido, (await Servico(c).GerarAsync(d, new RelatorioFiltroVM { De = hoje, Ate = hoje.AddDays(-1) })).Mensagem);
        }
        // padrão: o mês corrente
        var f = new RelatorioFiltroVM();
        await Servico(c).PreencherPadraoAsync(f);
        Assert.Equal((new DateOnly(hoje.Year, hoje.Month, 1), hoje.Month), (f.De, f.Ate!.Value.Month));
        Assert.Equal(1, f.Ate.Value.AddDays(1).Day);
    }

    [Fact]
    public async Task Faturamento_um_pedido_por_linha_com_a_revisao_vigente_e_sem_cancelados()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var pedido = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "MARIA", null);
        Assert.True(pedido.Sucesso, pedido.Mensagem);
        // revisão da proposta depois do pedido: o legado listava o pedido duas vezes
        Assert.True((await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "AJUSTE")).Sucesso);

        var r = await GerarAsync(c, "Faturamento", await HojeAsync(c));
        var dado = Assert.Single(Linhas(r, TipoLinha.Dado));
        Assert.Equal("CLIENTE TESTE", dado.Celulas[4]);
        Assert.Equal(("10,00", "30,00", "270,00"), (dado.Celulas[7], dado.Celulas[8], dado.Celulas[9]));   // 300 − 10%
        var total = Assert.Single(Linhas(r, TipoLinha.Total));
        Assert.Equal(("Total geral (1 pedido)", "270,00"), (total.Celulas[4], total.Celulas[9]));

        // filtro de cliente e situação; cancelado sai do relatório
        Assert.Empty(Linhas(await GerarAsync(c, "Faturamento", new RelatorioFiltroVM { De = hoje, Ate = hoje, Cliente = "OUTRO" }), TipoLinha.Dado));
        Assert.Empty(Linhas(await GerarAsync(c, "Faturamento", new RelatorioFiltroVM { De = hoje, Ate = hoje, Situacao = SituacaoPedido.Concluido }), TipoLinha.Dado));
        var p = await c.Db.PedidosVenda.SingleAsync(x => x.IdPedidosVenda == pedido.Valor.IdPedido);
        p.Situacao = SituacaoPedido.Cancelado;
        p.DtCancelamento = DateTime.Now;
        p.MotivoCancelamento = "TESTE";
        await c.Db.SaveChangesAsync();
        Assert.Empty(Linhas(await GerarAsync(c, "Faturamento", await HojeAsync(c)), TipoLinha.Dado));
    }

    [Fact]
    public async Task Amostra_revisada_conta_uma_vez_e_somente_com_pedido_e_o_padrao()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        await AmostraRevisadaComPedidoAsync(c);
        // amostra sem pedido e sem resultado (o pedido já existe, mas ela não foi ligada a ele)
        var semPedido = c.NovaAmostra(hoje);
        c.Db.Add(semPedido);
        await c.Db.SaveChangesAsync();
        await c.Db.LabHistAmostras.Where(a => a.IdLabHistAmostras == semPedido.IdLabHistAmostras)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IdPedidosVenda, (int?)null));

        var f = await HojeAsync(c);
        Assert.True(f.SomenteComPedido);   // dúvida 9: padrão marcado
        var r = await GerarAsync(c, "AmostrasPeriodo", f);
        var linha = Assert.Single(Linhas(r, TipoLinha.Dado));
        Assert.Equal(("CLIENTE TESTE", "1", "1", "100,0%"), (linha.Celulas[0], linha.Celulas[2], linha.Celulas[3], linha.Celulas[4]));
        Assert.Contains("Somente amostras com pedido de venda", r.Subtitulo);

        f.SomenteComPedido = false;
        linha = Assert.Single(Linhas(await GerarAsync(c, "AmostrasPeriodo", f), TipoLinha.Dado));
        Assert.Equal(("2", "1", "50,0%"), (linha.Celulas[2], linha.Celulas[3], linha.Celulas[4]));

        // por cliente e produto: mesma contagem, com subtotal do cliente
        r = await GerarAsync(c, "AmostrasCliente", f);
        linha = Assert.Single(Linhas(r, TipoLinha.Dado));
        Assert.Equal(("QUEIJO MINAS", "2", "1"), (linha.Celulas[3], linha.Celulas[4], linha.Celulas[5]));
        Assert.Equal("2", Assert.Single(Linhas(r, TipoLinha.Subtotal)).Celulas[4]);
        f.Produto = "INEXISTENTE";
        Assert.Empty(Linhas(await GerarAsync(c, "AmostrasCliente", f), TipoLinha.Dado));
    }

    [Fact]
    public async Task Parametros_tempo_e_producao_so_da_ultima_revisao_do_resultado()
    {
        await using var c = await Cenario.CriarAsync();
        var (amo, _) = await AmostraRevisadaComPedidoAsync(c);
        var f = await HojeAsync(c);

        // 2 parâmetros da revisão vigente (a revisão anterior também tem 2 linhas, que não contam)
        var r = await GerarAsync(c, "ParametrosAnalisados", f);
        Assert.Equal(2, Linhas(r, TipoLinha.Dado).Count());
        Assert.All(Linhas(r, TipoLinha.Dado), l => Assert.EndsWith("R01", l.Celulas[2]));
        Assert.Equal(["Total — CONTAGEM TOTAL: 1", "Total — SALMONELLA SP: 1"], Linhas(r, TipoLinha.Subtotal).Select(l => l.Celulas[1]));
        Assert.Equal("Total geral: 2 parâmetros analisados", Assert.Single(Linhas(r, TipoLinha.Total)).Celulas[1]);
        Assert.Single(r.Tabelas);
        f.Acreditado = true;
        Assert.Equal("CONTAGEM TOTAL", Assert.Single(Linhas(await GerarAsync(c, "ParametrosAnalisados", f), TipoLinha.Dado)).Celulas[1]);
        f.Acreditado = null;
        f.AgruparPorCliente = true;
        r = await GerarAsync(c, "ParametrosAnalisados", f);
        Assert.Equal("Subtotal — CLIENTE TESTE: 2 parâmetros analisados", Assert.Single(Linhas(r, TipoLinha.Subtotal)).Celulas[1]);
        Assert.Equal("Total por parâmetro", r.Tabelas[1].Titulo);

        // tempo: início e resultado no mesmo dia = 1 dia (inclusivo, RN-09.06)
        r = await GerarAsync(c, "TempoAnalises", f);
        Assert.All(Linhas(r, TipoLinha.Dado), l => Assert.Equal("1", l.Celulas[2]));
        Assert.Equal("1,0", Assert.Single(Linhas(r, TipoLinha.Total)).Celulas[2]);
        Assert.Equal(2, Linhas(r, TipoLinha.Dado, 1).Count());

        // produção: por dia e por parâmetro
        f.Parametro = "SALMONELLA";
        r = await GerarAsync(c, "Producao", f);
        Assert.Single(Linhas(r, TipoLinha.Dado));
        Assert.Equal(["Análises por dia", "Análises por parâmetro"], r.Tabelas.Skip(1).Select(t => t.Titulo));
        Assert.Equal("1", Assert.Single(Linhas(r, TipoLinha.Total, 1)).Celulas[1]);

        // o resultado lançado ontem fica fora do período de hoje
        var ontem = f.De!.Value.AddDays(-1);
        await c.Db.LabResultadosParam.Where(p => p.LabResultadoAnalise.UltimaRevisao && p.LabResultadoAnalise.LabHistAmostra.IdAmostraRaiz == amo.IdLabHistAmostras)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DtResultado, ontem).SetProperty(p => p.DtInicial, ontem));
        f.Parametro = null;
        Assert.Empty(Linhas(await GerarAsync(c, "Producao", f), TipoLinha.Dado));
    }

    [Fact]
    public async Task Pdf_e_csv_saem_do_mesmo_conjunto()
    {
        await using var c = await Cenario.CriarAsync();
        await AmostraRevisadaComPedidoAsync(c);
        var r = await GerarAsync(c, "TempoAnalises", await HojeAsync(c));

        var pdf = RelatorioPdf.Gerar(r, await Servico(c).EmpresaAsync(), DateTime.Now);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));

        var csv = Encoding.UTF8.GetString(r.Csv());
        Assert.StartsWith("﻿\"Tempo de duração das análises\"", csv);
        Assert.Contains("\"Início\";\"Resultado\";\"Dias\"", csv);
        Assert.Contains("\"Por parâmetro (dias)\"", csv);
        Assert.Contains("\"SALMONELLA SP\";\"1\";\"1,0\"", csv);
    }
}
