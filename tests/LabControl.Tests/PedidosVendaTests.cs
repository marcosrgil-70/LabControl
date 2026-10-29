using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 6.2 (T21 pedidos de venda, P08): data de autorização obrigatória, um pedido por proposta, amostra recebida
/// antes do pedido, contratado × recebido, conclusão automática no último boletim emitido e reabertura pela revisão
/// (pendência 24), conclusão/reabertura manual, cancelamento, revisão vigente, consulta e PDF.
/// </summary>
public class PedidosVendaTests
{
    private static PedidoVendaService Servico(Cenario c) =>
        new(c.Db, c.Procedures, new PropostaService(c.Db, c.Ativa, c.Procedures), new ServicoAuditoria(c.Db, c.Ativa));

    private static Task<SituacaoPedido> SituacaoAsync(Cenario c, int id) =>
        c.Db.PedidosVenda.AsNoTracking().Where(p => p.IdPedidosVenda == id).Select(p => p.Situacao).SingleAsync();

    private static async Task<int> GerarAsync(Cenario c, int? idProposta = null)
    {
        var r = await Servico(c).GerarAsync(new PedidoVendaGerarVM
        {
            IdProposta = idProposta ?? c.Proposta.IdLabPropostas, DtAutorizacao = await c.HojeAsync(), AutorizadoPor = "MARIA DO CLIENTE", NrPedidoCliente = "PO-77",
        });
        Assert.True(r.Sucesso, r.Mensagem);
        return r.Valor.IdPedido;
    }

    private static async Task EmitirAsync(Cenario c, int idBoletim) =>
        Assert.True((await c.Procedures.EmitirBoletimAsync(idBoletim, $"boletins/{idBoletim}.pdf", new string('a', 64))).Sucesso);

    [Fact]
    public async Task Gerar_exige_autorizacao_e_aceita_um_pedido_por_proposta()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();

        var modal = await s.PrepararGeracaoAsync(c.Proposta.IdLabPropostas);
        Assert.True(modal.Sucesso, modal.Mensagem);
        Assert.Equal((hoje, "CLIENTE TESTE"), (modal.Valor.DtAutorizacao, modal.Valor.Cliente));

        var semData = await s.GerarAsync(new PedidoVendaGerarVM { IdProposta = c.Proposta.IdLabPropostas });
        Assert.Equal(("É obrigatório o preenchimento da Data Autorização para gerar o Pedido de Venda.", "DtAutorizacao"), (semData.Mensagem, semData.Campo));
        var futura = await s.GerarAsync(new PedidoVendaGerarVM { IdProposta = c.Proposta.IdLabPropostas, DtAutorizacao = hoje.AddDays(1) });
        Assert.Equal("Data de Autorização não pode ser futura.", futura.Mensagem);

        var id = await GerarAsync(c);
        var vm = (await s.CarregarAsync(id))!;
        Assert.Equal((SituacaoPedido.Aberto, "MARIA DO CLIENTE", "PO-77", hoje), (vm.Situacao, vm.AutorizadoPor, vm.NrPedidoCliente, vm.DtAutorizacao));
        Assert.Matches(@"^\d{9}$", vm.NrPedido);
        Assert.Equal(270m, vm.VrTotal);
        Assert.Equal([new PedidoVendaItemVM(1, "QUEIJO MINAS", 2, 0)], vm.Itens);
        Assert.Equal(hoje, await c.Db.LabPropostas.Where(p => p.IdLabPropostas == c.Proposta.IdLabPropostas).Select(p => p.DtAutorizacao).SingleAsync());

        // outra revisão da mesma proposta não ganha outro pedido
        var rev = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "AJUSTE DE PREÇO");
        Assert.True(rev.Sucesso, rev.Mensagem);
        Assert.Equal("Já existe Pedido de Venda para esta proposta.", (await s.PrepararGeracaoAsync(rev.Valor.IdNovo)).Mensagem);
        var segundo = await s.GerarAsync(new PedidoVendaGerarVM { IdProposta = rev.Valor.IdNovo, DtAutorizacao = hoje });
        Assert.Equal("Já existe Pedido de Venda para esta proposta.", segundo.Mensagem);
        Assert.Equal("Gere o pedido a partir da última revisão da proposta.", (await s.PrepararGeracaoAsync(c.Proposta.IdLabPropostas)).Mensagem);

        // revisão vigente: o pedido avisa e atualiza para ela
        vm = (await s.CarregarAsync(id))!;
        Assert.Equal(rev.Valor.IdNovo, vm.IdPropostaVigente);
        Assert.True(vm.PodeAtualizar);
        Assert.True((await s.AtualizarRevisaoAsync(id)).Sucesso);
        vm = (await s.CarregarAsync(id))!;
        Assert.Equal((rev.Valor.IdNovo, (int?)null), (vm.IdProposta, vm.IdPropostaVigente));
        Assert.Single(vm.Itens);
    }

    [Fact]
    public async Task Amostra_recebida_antes_entra_no_pedido_e_ultimo_boletim_emitido_conclui()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var (amo1, _, bol1) = await c.AmostraComBoletimAsync(hoje);
        var (amo2, _, bol2) = await c.AmostraComBoletimAsync(hoje);

        // as 2 amostras recebidas antes do pedido passam a apontar para ele, que já nasce em execução
        var id = await GerarAsync(c);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        Assert.Equal(2, await c.Db.LabHistAmostras.CountAsync(a => a.IdPedidosVenda == id));
        var vm = (await s.CarregarAsync(id))!;
        Assert.Equal([new PedidoVendaItemVM(1, "QUEIJO MINAS", 2, 2)], vm.Itens);
        Assert.Equal(["Em montagem", "Em montagem"], vm.Amostras.Select(a => a.Boletim));
        Assert.False(vm.PodeCancelar);
        Assert.Equal("Pedido com amostra recebida não pode ser cancelado.", (await s.CancelarAsync(id, "DESISTÊNCIA")).Mensagem);

        // pendência 24: um boletim emitido não basta; o último conclui
        await EmitirAsync(c, bol1.IdLabBoletimAnalises);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        await EmitirAsync(c, bol2.IdLabBoletimAnalises);
        Assert.Equal(SituacaoPedido.Concluido, await SituacaoAsync(c, id));
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "PEDIDOS_VENDA" && a.IdRegistro == id.ToString() && a.DadosDepois!.Contains("Concluído automaticamente")));
        Assert.All((await s.CarregarAsync(id))!.Amostras, a => Assert.True(a.Emitido));

        // revisão do boletim reabre; desfazer a revisão conclui de novo (decisão do usuário)
        var rev = await c.Procedures.GerarRevisaoBoletimAsync(bol2.IdLabBoletimAnalises, "CORRIGIR UNIDADE");
        Assert.True(rev.Sucesso, rev.Mensagem);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        Assert.True((await c.Procedures.ExcluirRevisaoBoletimAsync(rev.Valor.IdNovo)).Sucesso);
        Assert.Equal(SituacaoPedido.Concluido, await SituacaoAsync(c, id));

        // revisão da amostra também reabre
        var revAmo = await c.Procedures.GerarRevisaoAmostraAsync(amo1.IdLabHistAmostras, "LOTE ERRADO");
        Assert.True(revAmo.Sucesso, revAmo.Mensagem);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        _ = amo2;
    }

    [Fact]
    public async Task Item_faltando_nao_conclui_sozinho_conclusao_e_reabertura_manuais()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var (_, _, bol) = await c.AmostraComBoletimAsync(hoje);
        var id = await GerarAsync(c);
        await EmitirAsync(c, bol.IdLabBoletimAnalises);
        // contratou 2 e recebeu 1: continua em execução mesmo com o boletim emitido
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));

        Assert.True((await s.ConcluirAsync(id)).Sucesso);
        Assert.Equal(SituacaoPedido.Concluido, await SituacaoAsync(c, id));
        Assert.Equal("Pedido concluído não pode ser concluído.", (await s.ConcluirAsync(id)).Mensagem);

        var semMotivo = await s.ReabrirAsync(id, " ");
        Assert.Equal(("Informe o motivo da reabertura.", "motivo"), (semMotivo.Mensagem, semMotivo.Campo));
        Assert.True((await s.ReabrirAsync(id, "CLIENTE MANDOU A 2ª AMOSTRA")).Sucesso);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "PEDIDOS_VENDA" && a.IdRegistro == id.ToString() && a.DadosDepois!.Contains("CLIENTE MANDOU")));
        Assert.Equal("Só pedido concluído pode ser reaberto.", (await s.ReabrirAsync(id, "DE NOVO")).Mensagem);

        // concluído à mão e chega amostra nova pelo recebimento: volta para em execução
        Assert.True((await s.ConcluirAsync(id)).Sucesso);
        var amostra = await new AmostraService(c.Db, c.Ativa, c.Procedures).GravarAsync(new AmostraVM
        {
            IdEntidades = c.Cliente.IdEntidades, IdLabPropostas = c.Proposta.IdLabPropostas, IdLabPropostasProdutos = c.Item.IdLabPropostasProdutos,
            IdAmostrasTipo = c.TipoAmostra.IdAmostrasTipo, IdAnalisesTipo = c.TipoAnalise.IdAnalisesTipo, DtEntrega = hoje, HrEntrega = new TimeOnly(9, 30),
            LocalRecebimento = "RECEPÇÃO", IdEmbalagensTipos = c.Embalagem.IdEmbalagensTipos, QtdeEmbalagens = "1",
            IdEntidadesFuncResp = c.Ativa.ProfissionalId, IdAmostrasStatus = c.StatusAmostraInicial,
        });
        Assert.True(amostra.Sucesso, amostra.Mensagem);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        // 6.3: a reabertura pelo recebimento é do banco e fica na auditoria com o motivo (antes, UPDATE sem auditoria)
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "PEDIDOS_VENDA" && a.IdRegistro == id.ToString() && a.DadosDepois!.Contains("amostra ou boletim pendente")));
    }

    /// <summary>
    /// 6.3, decisão do usuário (opção a): concluído à mão com item faltando, reaberto pela revisão do boletim, volta a
    /// concluir sozinho na emissão; reaberto pelo usuário, volta à regra normal (só conclui com tudo recebido).
    /// </summary>
    [Fact]
    public async Task Concluido_a_mao_volta_a_concluir_depois_da_revisao_do_boletim()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (_, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var id = await GerarAsync(c);
        await EmitirAsync(c, bol.IdLabBoletimAnalises);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id)); // contratou 2, recebeu 1
        Assert.True((await s.ConcluirAsync(id)).Sucesso);

        var rev = await c.Procedures.GerarRevisaoBoletimAsync(bol.IdLabBoletimAnalises, "CORRIGIR UNIDADE");
        Assert.True(rev.Sucesso, rev.Mensagem);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
        await EmitirAsync(c, rev.Valor.IdNovo);
        Assert.Equal(SituacaoPedido.Concluido, await SituacaoAsync(c, id));
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "PEDIDOS_VENDA" && a.IdRegistro == id.ToString() && a.DadosDepois!.Contains("concluído à mão antes")));

        // reaberto pelo usuário: a próxima emissão não conclui (ainda falta 1 amostra)
        Assert.True((await s.ReabrirAsync(id, "FALTA A 2ª AMOSTRA")).Sucesso);
        var rev2 = await c.Procedures.GerarRevisaoBoletimAsync(rev.Valor.IdNovo, "OUTRA CORREÇÃO");
        Assert.True(rev2.Sucesso, rev2.Mensagem);
        await EmitirAsync(c, rev2.Valor.IdNovo);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));
    }

    /// <summary>6.3 — "Atualizar para a revisão vigente" que reduz o contratado ao recebido conclui o pedido.</summary>
    [Fact]
    public async Task Atualizar_revisao_que_reduz_o_contratado_conclui()
    {
        await using var c = await Cenario.CriarAsync();
        var (_, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var id = await GerarAsync(c);
        await EmitirAsync(c, bol.IdLabBoletimAnalises);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, id));

        var rev = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "CLIENTE REDUZIU PARA 1");
        Assert.True(rev.Sucesso, rev.Mensagem);
        await c.Db.LabPropostasProdutos.Where(p => p.IdLabPropostas == rev.Valor.IdNovo).ExecuteUpdateAsync(u => u.SetProperty(p => p.QtdeAmostras, 1));
        Assert.True((await Servico(c).AtualizarRevisaoAsync(id)).Sucesso);
        Assert.Equal(SituacaoPedido.Concluido, await SituacaoAsync(c, id));
    }

    /// <summary>
    /// 6.3 — amostra que troca de proposta passa para o pedido da proposta nova: etiqueta e boletim leem o nº do pedido
    /// da amostra (VW_LAB_AMOSTRAS) e antes saíam com o pedido antigo.
    /// </summary>
    [Fact]
    public async Task Amostra_que_troca_de_proposta_muda_de_pedido()
    {
        await using var c = await Cenario.CriarAsync();
        var amostras = new AmostraService(c.Db, c.Ativa, c.Procedures);
        var amo = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        var pedido1 = await GerarAsync(c);
        var (proposta2, item2) = await OutraPropostaAsync(c);
        var pedido2 = await GerarAsync(c, proposta2);
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, pedido1));
        Assert.Equal(SituacaoPedido.Aberto, await SituacaoAsync(c, pedido2));

        var vm = (await amostras.CarregarAsync(amo.IdLabHistAmostras))!;
        (vm.IdLabPropostas, vm.IdLabPropostasProdutos) = (proposta2, item2);
        var r = await amostras.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);

        Assert.Equal(pedido2, await c.Db.LabHistAmostras.Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).Select(a => a.IdPedidosVenda).SingleAsync());
        var nr2 = PedidoVendaService.Numero(await c.Db.PedidosVenda.Where(p => p.IdPedidosVenda == pedido2).Select(p => p.NrPedido).SingleAsync());
        Assert.Equal(nr2, await c.Db.VwLabAmostras.Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).Select(a => a.NrPedidoFmt).SingleAsync());
        Assert.Equal(SituacaoPedido.EmExecucao, await SituacaoAsync(c, pedido2));
        Assert.Empty((await Servico(c).CarregarAsync(pedido1))!.Amostras);
    }

    // 2ª proposta do mesmo cliente, com um item e uma análise do tipo da amostra do cenário
    internal static async Task<(int Proposta, int Item)> OutraPropostaAsync(Cenario c)
    {
        var p = new LabProposta
        {
            IdEntidades = c.Proposta.IdEntidades, IdEntidadesFunc = c.Proposta.IdEntidadesFunc, IdEntEndLaudo = c.Proposta.IdEntEndLaudo,
            IdEntContatoResultado = c.Proposta.IdEntContatoResultado, IdEntCobranca = c.Proposta.IdEntCobranca,
            IdLabPropostasStatus = c.Proposta.IdLabPropostasStatus, IdCondicoesPagtos = c.Proposta.IdCondicoesPagtos,
        };
        c.Db.Add(p);
        await c.Db.SaveChangesAsync();
        var item = new LabPropostaProduto { IdLabPropostas = p.IdLabPropostas, IdProdutos = c.Produto.IdProdutos, QtdeAmostras = 1 };
        c.Db.Add(item);
        await c.Db.SaveChangesAsync();
        var modelo = await c.Db.LabPropostasProdanalises.AsNoTracking().FirstAsync(a => a.IdLabPropostasProdutos == c.Item.IdLabPropostasProdutos);
        c.Db.Add(new LabPropostaProdAnalise { IdLabPropostas = p.IdLabPropostas, IdLabPropostasProdutos = item.IdLabPropostasProdutos,
            IdLabParametrosAnalises = modelo.IdLabParametrosAnalises, IdAnalisesMetodos = modelo.IdAnalisesMetodos,
            IdIdiomas = modelo.IdIdiomas, IdPrazos = modelo.IdPrazos, QtdeAmostras = 1, VrUnitario = 100 });
        await c.Db.SaveChangesAsync();
        return (p.IdLabPropostas, item.IdLabPropostasProdutos);
    }

    [Fact]
    public async Task Cancelar_exige_motivo_trava_o_pedido_e_o_pdf_sai_marcado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var id = await GerarAsync(c);
        Assert.True((await s.GravarObservacaoAsync(id, "  Entregar na portaria 2  ")).Sucesso);

        var semMotivo = await s.CancelarAsync(id, "");
        Assert.Equal(("Informe o motivo do cancelamento.", "motivo"), (semMotivo.Mensagem, semMotivo.Campo));
        Assert.True((await s.CancelarAsync(id, "CLIENTE DESISTIU")).Sucesso);
        var vm = (await s.CarregarAsync(id))!;
        Assert.Equal((SituacaoPedido.Cancelado, "CLIENTE DESISTIU", "Entregar na portaria 2"), (vm.Situacao, vm.MotivoCancelamento, vm.Observacao));
        Assert.NotNull(vm.DtCancelamento);
        Assert.Equal("O pedido já está cancelado.", (await s.CancelarAsync(id, "DE NOVO")).Mensagem);
        Assert.Equal("Pedido cancelado não pode ser alterado.", (await s.GravarObservacaoAsync(id, "x")).Mensagem);
        Assert.Equal("Pedido cancelado não pode ser concluído.", (await s.ConcluirAsync(id)).Mensagem);

        // consulta: filtros por situação, proposta e cliente
        var nrProposta = await c.Db.LabPropostas.Where(p => p.IdLabPropostas == c.Proposta.IdLabPropostas).Select(p => p.NrProposta).SingleAsync();
        var lista = await s.ListarAsync(new PedidoVendaFiltroVM { Situacao = SituacaoPedido.Cancelado, Proposta = nrProposta, Cliente = "CLIENTE TESTE" });
        var linha = Assert.Single(lista.Itens);
        Assert.Equal((id, 0, 2), (linha.Id, linha.Recebidas, linha.Contratadas));
        Assert.Empty((await s.ListarAsync(new PedidoVendaFiltroVM { Situacao = SituacaoPedido.Aberto, Proposta = nrProposta })).Itens);

        // amostra recebida depois não entra em pedido cancelado
        var amo = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        Assert.Null(await c.Db.LabHistAmostras.Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).Select(a => a.IdPedidosVenda).SingleAsync());

        var pdf = await s.ImprimirAsync(id);
        Assert.True(pdf.Sucesso, pdf.Mensagem);
        Assert.Equal($"Pedido de venda {vm.NrPedido}.pdf", pdf.Valor.Nome);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Valor.Conteudo, 0, 4));
    }
}
