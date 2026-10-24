using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 4.3 (T22 recebimento): número pelo banco no ano do servidor, testes = análises do item com data prevista,
/// entrada de estoque, pedido em execução e aviso do contratado, data futura/retroativa (P04), tipo de análise do item,
/// datas parciais, alteração com resultado, revisão (P11), exclusão e duas inclusões simultâneas.
/// </summary>
public class AmostrasTests
{
    private static AmostraService Servico(Cenario c) => new(c.Db, c.Ativa, c.Procedures);

    /// <summary>Amostra como a tela manda, no item do cenário (2 análises do mesmo tipo, prazo de 10 dias).</summary>
    private static AmostraVM Nova(Cenario c, DateOnly entrega) => new()
    {
        IdEntidades = c.Cliente.IdEntidades, IdLabPropostas = c.Proposta.IdLabPropostas, IdLabPropostasProdutos = c.Item.IdLabPropostasProdutos,
        IdAmostrasTipo = c.TipoAmostra.IdAmostrasTipo, IdAnalisesTipo = c.TipoAnalise.IdAnalisesTipo, DtEntrega = entrega, HrEntrega = new TimeOnly(9, 30),
        LocalRecebimento = "RECEPÇÃO", IdEmbalagensTipos = c.Embalagem.IdEmbalagensTipos, QtdeEmbalagens = "3", NrLote = "L-01",
        ValidadeMes = 5, ValidadeAno = 2027, Temperatura = "-18,5", NomeColetor = "JOÃO", DtHrColeta = entrega.ToDateTime(new TimeOnly(8, 0)),
        IdEntidadesFuncResp = c.Ativa.ProfissionalId, IdAmostrasStatus = c.StatusAmostraInicial,
    };

    [Fact]
    public async Task Inclusao_numera_no_ano_do_servidor_gera_testes_estoque_e_pedido()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var ped = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "CLIENTE", null);
        Assert.True(ped.Sucesso, ped.Mensagem);

        var r = await s.GravarAsync(Nova(c, hoje));
        Assert.True(r.Sucesso, r.Mensagem);
        var a = await c.Db.LabHistAmostras.AsNoTracking().SingleAsync(x => x.IdLabHistAmostras == r.Valor.Id);
        Assert.Equal((hoje.Year, $"04-00001-01/{hoje.Year % 100:00}"), (a.AnoAmostra, a.NrAmostra));
        Assert.Equal(c.Ativa.ProfissionalId, a.IdEntidadesFuncDig);
        Assert.Equal(-18.5m, a.TemperaturaVerificacao);

        // testes = as 2 análises do item, com data prevista = entrega + 10 dias do prazo
        var testes = await c.Db.LabHistAmostrasTestes.AsNoTracking().Where(t => t.IdLabHistAmostras == a.IdLabHistAmostras).ToListAsync();
        Assert.Equal(2, testes.Count);
        Assert.All(testes, t => Assert.Equal(hoje.AddDays(10), t.DtPrevista));
        // entrada automática: saldo = qtde; pedido aberto → em execução; validade só mês/ano
        Assert.Equal(3m, await c.Db.LabHistAmostrasSaldo.Where(x => x.IdLabHistAmostras == a.IdLabHistAmostras).Select(x => x.SaldoAtual).SingleAsync());
        Assert.Equal(SituacaoPedido.EmExecucao, await c.Db.PedidosVenda.Where(p => p.IdPedidosVenda == ped.Valor.IdPedido).Select(p => p.Situacao).SingleAsync());
        Assert.Equal("05/2027", await c.Db.VwLabAmostras.Where(v => v.IdLabHistAmostras == a.IdLabHistAmostras).Select(v => v.Validade).SingleAsync());
        Assert.Null(r.Valor.Aviso);

        // o item contratou 2: a 3ª amostra grava com aviso
        Assert.Null((await s.GravarAsync(Nova(c, hoje))).Valor.Aviso);
        var terceira = await s.GravarAsync(Nova(c, hoje));
        Assert.Equal("Atenção: 3 amostras recebidas para este produto e o pedido contratou 2.", terceira.Valor.Aviso);

        // a tela relê: testes, histórico (inclusão), sem bloqueio
        var vm = (await s.CarregarAsync(a.IdLabHistAmostras))!;
        Assert.Equal((2, "Inclusão", (string?)null), (vm.Testes.Count, vm.Historico.Single().Acao, vm.Bloqueio));
        Assert.Equal(3, (await s.ListarAsync(new AmostraFiltroVM { Cliente = "CLIENTE TESTE" })).Total);
        // cascata da tela: propostas do cliente e tipos de análise do item
        Assert.Contains((await s.ClienteAsync(c.Cliente.IdEntidades))!.Propostas, p => p.Id == c.Proposta.IdLabPropostas);
        Assert.Equal([c.TipoAnalise.IdAnalisesTipo], (await s.PropostaAsync(c.Proposta.IdLabPropostas))!.Itens.Single().TiposAnalise);
        var comprovante = (await s.ComprovanteAsync(a.IdLabHistAmostras))!;
        Assert.Equal((a.NrAmostra, 2, "05/2027"), (comprovante.NrAmostra, comprovante.Testes.Count, comprovante.Validade));
    }

    [Fact]
    public async Task Validacoes_data_futura_retroativa_tipo_do_item_e_datas_parciais()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        async Task<Resultado<AmostraGravada>> Gravar(Action<AmostraVM> mudar) { var vm = Nova(c, hoje); mudar(vm); return await s.GravarAsync(vm); }

        Assert.Equal(("Cliente é obrigatório.", "IdEntidades"), Erro(await Gravar(v => v.IdEntidades = null)));
        Assert.Equal(("Tipo de Análise obrigatório.", "IdAnalisesTipo"), Erro(await Gravar(v => v.IdAnalisesTipo = null)));
        Assert.Equal(("Data de entrega maior que a data de hoje.", "DtEntrega"), Erro(await Gravar(v => v.DtEntrega = hoje.AddDays(1))));
        Assert.Equal("A data/hora da coleta não pode ser depois da entrega.", (await Gravar(v => v.DtHrColeta = hoje.ToDateTime(new TimeOnly(23, 0)))).Mensagem);
        // tipo de análise que não está nas análises do item
        var outroTipo = new AnaliseTipo { Codigo = 2, Descricao = "FÍSICO-QUÍMICA" };
        c.Db.Add(outroTipo);
        await c.Db.SaveChangesAsync();
        Assert.Equal("IdAnalisesTipo", (await Gravar(v => v.IdAnalisesTipo = outroTipo.IdAnalisesTipo)).Campo);
        // 31/02 → o banco recusa (mensagem da trigger)
        Assert.Equal("Data de validade inválida.", (await Gravar(v => (v.ValidadeDia, v.ValidadeMes, v.ValidadeAno) = (31, 2, 2027))).Mensagem);

        // retroativa (1 ano atrás): pede confirmação; confirmada sem permissão recusa; com permissão grava no ANO ATUAL
        var antiga = hoje.AddYears(-1);
        var semConfirmar = await Gravar(v => (v.DtEntrega, v.DtHrColeta) = (antiga, null));
        Assert.Equal(nameof(AmostraVM.ConfirmouRetroativa), semConfirmar.Campo);
        Assert.Contains("Confirme que o ANO está correto.", semConfirmar.Mensagem);
        Assert.Contains("exige a permissão", (await Gravar(v => (v.DtEntrega, v.DtHrColeta, v.ConfirmouRetroativa) = (antiga, null, true))).Mensagem);
        c.Ativa.Permissoes.Add(AmostraService.PermissaoRetroativa);
        var r = await Gravar(v => (v.DtEntrega, v.DtHrColeta, v.ConfirmouRetroativa) = (antiga, null, true));
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal((hoje.Year, antiga), await c.Db.LabHistAmostras.Where(a => a.IdLabHistAmostras == r.Valor.Id)
            .Select(a => ValueTuple.Create((int)a.AnoAmostra, a.DtEntrega)).SingleAsync());
    }

    [Fact]
    public async Task Com_resultado_nao_regera_testes_revisao_e_exclusao()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();

        // sem resultado: revisão recusada, exclusão permitida
        var livre = (await s.GravarAsync(Nova(c, hoje))).Valor.Id;
        Assert.Equal("Não existe Resultados para esta Amostra. A Amostra pode ser alterada.", (await s.GerarRevisaoAsync(livre, "x")).Mensagem);
        Assert.True((await s.ExcluirAsync(livre)).Sucesso);

        // com resultado (cenário do roteiro): altera o lote sem regerar os testes; produto travado; exclusão recusada pelo banco
        var (amostra, _, _) = await c.AmostraComBoletimAsync(hoje);
        var testesAntes = await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == amostra.IdLabHistAmostras).Select(t => t.IdLabHistAmostrasTestes).ToListAsync();
        var vm = (await s.CarregarAsync(amostra.IdLabHistAmostras))!;
        Assert.True(vm.TemResultado);
        vm.NrLote = "L-ALTERADO";
        vm.NomeColetor = "MARIA";
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        Assert.Equal(testesAntes, await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == amostra.IdLabHistAmostras).Select(t => t.IdLabHistAmostrasTestes).ToListAsync());
        var outroTipo = new AmostraTipo { Codigo = 5, Descricao = "ÁGUAS" };
        c.Db.Add(outroTipo);
        await c.Db.SaveChangesAsync();
        vm.IdAmostrasTipo = outroTipo.IdAmostrasTipo;
        Assert.StartsWith("Existem resultados", (await s.GravarAsync(vm)).Mensagem);
        Assert.Equal("Amostra possui resultados e não pode ser excluída.", (await s.ExcluirAsync(amostra.IdLabHistAmostras)).Mensagem);

        // revisão: motivo obrigatório; R01 com coletor copiado; estoque continua só na raiz
        Assert.Equal("Informe o motivo da revisão.", (await s.GerarRevisaoAsync(amostra.IdLabHistAmostras, " ")).Mensagem);
        var rev = await s.GerarRevisaoAsync(amostra.IdLabHistAmostras, "Correção do lote informado pelo cliente");
        Assert.True(rev.Sucesso, rev.Mensagem);
        var nova = await c.Db.LabHistAmostras.AsNoTracking().SingleAsync(a => a.IdLabHistAmostras == rev.Valor);
        Assert.Equal((1, amostra.IdLabHistAmostras, "MARIA", "L-ALTERADO"), (nova.Revisao, nova.IdAmostraRaiz, nova.NomeColetor, nova.NrLote));
        Assert.EndsWith(" R01", nova.NrAmostra);
        Assert.False(await c.Db.LabMovAmostras.AnyAsync(m => m.IdLabHistAmostras == nova.IdLabHistAmostras));
        var antiga = (await s.CarregarAsync(amostra.IdLabHistAmostras))!;
        Assert.Equal("Revisão antiga da amostra é somente leitura.", antiga.Bloqueio);
        Assert.Equal("Só é possível revisar a última revisão da amostra.", (await s.GerarRevisaoAsync(amostra.IdLabHistAmostras, "de novo")).Mensagem);
    }

    [Fact]
    public async Task Duas_inclusoes_simultaneas_recebem_numeros_diferentes()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        async Task<string> Incluir()
        {
            await using var db = await BancoTeste.NovoContextoAsync(c.Ativa);
            var r = await new AmostraService(db, c.Ativa, new Procedures(db, c.Ativa)).GravarAsync(Nova(c, hoje));
            Assert.True(r.Sucesso, r.Mensagem);
            return await db.LabHistAmostras.Where(a => a.IdLabHistAmostras == r.Valor.Id).Select(a => a.NrAmostra).SingleAsync();
        }
        var numeros = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Incluir()));
        Assert.Equal(6, numeros.Distinct().Count());
    }

    /// <summary>T29 (4.4): filtros da pesquisa e nunca outra empresa — as duas empresas têm a amostra 04-00001-01/AA.</summary>
    [Fact]
    public async Task Pesquisa_filtra_e_nunca_mostra_outra_empresa()
    {
        await using var c = await Cenario.CriarAsync();
        await using var outra = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var minha = (await Servico(c).GravarAsync(Nova(c, hoje))).Valor.Id;
        var semLote = Nova(c, hoje);
        semLote.NrLote = "OUTRO";
        await Servico(c).GravarAsync(semLote);
        Assert.True((await Servico(outra).GravarAsync(Nova(outra, hoje))).Sucesso);

        var s = Servico(c);
        var porNumero = await s.ListarAsync(new AmostraFiltroVM { Numero = "00001", OrdenarPorNumero = true });
        Assert.Equal([minha], porNumero.Itens.Select(i => i.Id)); // a 00001 da outra empresa não aparece
        Assert.Equal(("ALIMENTOS", "MICROBIOLÓGICA"), (porNumero.Itens[0].TipoAmostra, porNumero.Itens[0].TipoAnalise));
        Assert.Equal([minha], (await s.ListarAsync(new AmostraFiltroVM { Lote = "L-0" })).Itens.Select(i => i.Id));
        Assert.Equal(2, (await s.ListarAsync(new AmostraFiltroVM { Produto = "QUEIJO", Ate = hoje })).Total);
        Assert.Equal(0, (await s.ListarAsync(new AmostraFiltroVM { ApenasComResultado = true })).Total);
        Assert.Equal(2, (await s.ListarAsync(new AmostraFiltroVM { ApenasSemLocalizacao = true })).Total);
    }

    private static (string?, string?) Erro(IResultado r) => (r.Mensagem, r.Campo);
}
