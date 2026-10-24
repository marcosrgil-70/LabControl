using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// 4.6 — cenários de aceite da Fase 4 que faltavam pelo serviço (P09 extras e P10). Os demais estão em
/// <see cref="AmostrasTests"/>, <see cref="PropostasTests"/>, <see cref="EtiquetasTests"/>, no roteiro SQL e no navegador.
/// </summary>
public class AceiteFase4Tests
{
    private static AmostraVM Nova(Cenario c, DateOnly entrega) => new()
    {
        IdEntidades = c.Cliente.IdEntidades, IdLabPropostas = c.Proposta.IdLabPropostas, IdLabPropostasProdutos = c.Item.IdLabPropostasProdutos,
        IdAmostrasTipo = c.TipoAmostra.IdAmostrasTipo, IdAnalisesTipo = c.TipoAnalise.IdAnalisesTipo, DtEntrega = entrega, HrEntrega = new TimeOnly(9, 0),
        LocalRecebimento = "RECEPÇÃO", IdEmbalagensTipos = c.Embalagem.IdEmbalagensTipos, QtdeEmbalagens = "1",
        IdEntidadesFuncResp = c.Ativa.ProfissionalId, IdAmostrasStatus = c.StatusAmostraInicial,
    };

    [Fact]
    public async Task P09_proposta_de_outro_cliente_item_de_outra_proposta_e_revisao_antiga_sao_recusados()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AmostraService(c.Db, c.Ativa, c.Procedures);
        var hoje = await c.HojeAsync();

        // outro cliente com a proposta do cenário (mensagem da TG_AMOSTRAS_BI)
        var outro = new Entidade { Categoria = Categoria.Juridica, Nome = "OUTRO CLIENTE", TipoCliente = true };
        c.Db.Add(outro);
        await c.Db.SaveChangesAsync();
        var vm = Nova(c, hoje);
        vm.IdEntidades = outro.IdEntidades;
        Assert.Equal("A proposta selecionada não é do cliente da amostra.", (await s.GravarAsync(vm)).Mensagem);

        // item de outra proposta (mesmo cliente, mesmo produto e tipo de análise)
        var outraProposta = new LabProposta
        {
            IdEntidades = c.Cliente.IdEntidades, IdEntidadesFunc = c.Proposta.IdEntidadesFunc, IdEntEndLaudo = c.Proposta.IdEntEndLaudo,
            IdEntContatoResultado = c.Proposta.IdEntContatoResultado, IdEntCobranca = c.Cliente.IdEntidades,
            IdLabPropostasStatus = c.Proposta.IdLabPropostasStatus, IdCondicoesPagtos = c.Proposta.IdCondicoesPagtos,
        };
        c.Db.Add(outraProposta);
        await c.Db.SaveChangesAsync();
        var itemOutro = new LabPropostaProduto { IdLabPropostas = outraProposta.IdLabPropostas, IdProdutos = c.Produto.IdProdutos, QtdeAmostras = 1 };
        c.Db.Add(itemOutro);
        await c.Db.SaveChangesAsync();
        var modelo = await c.Db.LabPropostasProdanalises.AsNoTracking().FirstAsync(a => a.IdLabPropostas == c.Proposta.IdLabPropostas);
        c.Db.Add(new LabPropostaProdAnalise
        {
            IdLabPropostas = outraProposta.IdLabPropostas, IdLabPropostasProdutos = itemOutro.IdLabPropostasProdutos,
            IdLabParametrosAnalises = modelo.IdLabParametrosAnalises, IdAnalisesMetodos = modelo.IdAnalisesMetodos,
            IdIdiomas = modelo.IdIdiomas, IdPrazos = modelo.IdPrazos, VrUnitario = 10,
        });
        await c.Db.SaveChangesAsync();
        vm = Nova(c, hoje);
        vm.IdLabPropostasProdutos = itemOutro.IdLabPropostasProdutos;
        Assert.Equal("O produto selecionado não pertence à proposta.", (await s.GravarAsync(vm)).Mensagem);

        // revisão antiga da proposta: depois de revisar, a R0 não recebe amostra
        var rev = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "Aceite 4.6");
        Assert.True(rev.Sucesso, rev.Mensagem);
        Assert.Equal("Selecione a última revisão da proposta.", (await s.GravarAsync(Nova(c, hoje))).Mensagem);
        // e a tela não oferece mais a R0 para o cliente
        Assert.DoesNotContain((await s.ClienteAsync(c.Cliente.IdEntidades))!.Propostas, p => p.Id == c.Proposta.IdLabPropostas);
    }

    /// <summary>Code-review 4.6: recusa de procedure dentro da transação (já convertida em Resultado) chega à tela, não vira 500.</summary>
    [Fact]
    public async Task Recusa_de_negocio_lancada_dentro_da_transacao_vira_mensagem()
    {
        var embrulhada = new InvalidOperationException("camada", new ErroDeNegocioException("Existem resultados para esta amostra."));
        Assert.Equal("Existem resultados para esta amostra.", Infra.ErrosMySql.MensagemDeNegocio(embrulhada));
        var r = await Infra.ErrosMySql.CapturarAsync<int>(() => throw new ErroDeNegocioException("Recusado pela procedure."));
        Assert.Equal((false, "Recusado pela procedure."), (r.Sucesso, r.Mensagem));
        Assert.Null(Infra.ErrosMySql.MensagemDeNegocio(new InvalidOperationException("técnico"))); // erro técnico continua técnico
    }

    [Fact]
    public async Task P10_29_de_fevereiro_bissexto_aceito_e_revisao_copia_as_datas_parciais()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new AmostraService(c.Db, c.Ativa, c.Procedures);
        var hoje = await c.HojeAsync();

        var vm = Nova(c, hoje);
        (vm.ValidadeDia, vm.ValidadeMes, vm.ValidadeAno) = (29, 2, 2028);
        (vm.FabricacaoMes, vm.FabricacaoAno) = (10, 2026);
        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        var v = await c.Db.VwLabAmostras.AsNoTracking().SingleAsync(x => x.IdLabHistAmostras == r.Valor.Id);
        Assert.Equal(("29/02/2028", "10/2026"), (v.Validade, v.Fabricacao));

        // revisão (precisa de resultado): as seis colunas das datas parciais vão para a R01
        var (amostra, _, _) = await c.AmostraComBoletimAsync(hoje);
        var comDatas = (await s.CarregarAsync(amostra.IdLabHistAmostras))!;
        (comDatas.ValidadeDia, comDatas.ValidadeMes, comDatas.ValidadeAno, comDatas.FabricacaoMes, comDatas.FabricacaoAno) = (29, 2, 2028, 10, 2026);
        Assert.True((await s.GravarAsync(comDatas)).Sucesso);
        var nova = (await s.GerarRevisaoAsync(amostra.IdLabHistAmostras, "Aceite 4.6")).Valor;
        var r01 = await c.Db.LabHistAmostras.AsNoTracking().SingleAsync(a => a.IdLabHistAmostras == nova);
        Assert.Equal(((byte?)29, (byte?)2, (short?)2028, (byte?)null, (byte?)10, (short?)2026),
            (r01.ValidadeDia, r01.ValidadeMes, r01.ValidadeAno, r01.FabricacaoDia, r01.FabricacaoMes, r01.FabricacaoAno));
    }
}
