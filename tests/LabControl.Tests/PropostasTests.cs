using System.Text.Json;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 4.2 (T20 proposta essencial): número pelo banco, valores do roteiro P06 (300,00 − 10% = 270,00),
/// mensagens obrigatórias do legado, alteração sem revisão (quantidade propaga às análises), bloqueios e exclusão.
/// </summary>
public class PropostasTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static PropostaService Servico(Cenario c) => new(c.Db, c.Ativa, c.Procedures);

    /// <summary>Proposta como a tela manda: cliente/contatos/endereços do cenário, 1 produto com as 2 análises do roteiro.</summary>
    private static async Task<PropostaVM> NovaAsync(Cenario c, int qtde = 2, decimal unit1 = 100, decimal unit2 = 50)
    {
        var modelo = await c.Db.LabPropostasProdanalises.AsNoTracking().Where(a => a.IdLabPropostas == c.Proposta.IdLabPropostas)
            .OrderBy(a => a.IdLabPropostasProdanalises).ToListAsync();
        var vm = await Servico(c).NovaAsync();
        vm.IdEntidades = vm.IdEntCobranca = c.Cliente.IdEntidades;
        vm.IdEntEndLaudo = vm.IdEntEndCobranca = c.Proposta.IdEntEndLaudo;
        vm.IdEntContatoResultado = vm.IdEntContatoNf = c.Proposta.IdEntContatoResultado;
        vm.IdCondicoesPagtos = c.Proposta.IdCondicoesPagtos;
        vm.PorcDesconto = "10";
        vm.TxObservacao = "<p onclick=\"x()\">Amostra <strong>refrigerada</strong></p><script>alert(1)</script>";
        vm.ItensJson = JsonSerializer.Serialize(new[]
        {
            new PropostaItemVM
            {
                IdProdutos = c.Produto.IdProdutos, QtdeAmostras = qtde,
                Analises = modelo.Select((a, i) => new PropostaAnaliseVM
                {
                    IdParametro = a.IdLabParametrosAnalises, IdMetodo = a.IdAnalisesMetodos, IdIdioma = a.IdIdiomas, IdPrazo = a.IdPrazos,
                    VrUnitario = i == 0 ? unit1 : unit2,
                }).ToList(),
            },
        }, Json);
        return vm;
    }

    [Fact]
    public async Task Inclusao_numera_pelo_banco_e_calcula_os_valores_do_roteiro()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var r = await s.GravarAsync(await NovaAsync(c));
        Assert.True(r.Sucesso, r.Mensagem);

        var p = await c.Db.LabPropostas.AsNoTracking().SingleAsync(x => x.IdLabPropostas == r.Valor);
        var hoje = await c.HojeAsync();
        // o cenário já tem a proposta 1 da empresa: esta é a 2, no ano do servidor, revisão 0
        Assert.Equal($"000002/{hoje.Year % 100:00}-R0", p.NrProposta);
        Assert.Equal((300m, 0m, 300m, 10m, 30m, 270m),
            (p.VrSomaVrAnalis, p.VrSomaDescAnalis, p.VrSomaVrTotAnalis, p.PorcDesconto, p.VrDesconto, p.VrTotalProposta));
        Assert.Equal(c.Ativa.ProfissionalId, p.IdEntidadesFunc);
        var item = await c.Db.LabPropostasProdutos.AsNoTracking().SingleAsync(x => x.IdLabPropostas == p.IdLabPropostas);
        Assert.Equal(1, item.Item); // item automático
        var textos = await c.Db.LabPropostasTextos.AsNoTracking().SingleAsync(x => x.IdLabPropostas == p.IdLabPropostas);
        Assert.Equal("<p>Amostra <strong>refrigerada</strong></p>", textos.TxObservacao); // atributos e script removidos

        // a tela relê tudo: número, itens com o nº do item e as análises
        var vm = (await s.CarregarAsync(p.IdLabPropostas))!;
        Assert.Null(vm.Bloqueio);
        var itens = PropostaService.LerItens(vm.ItensJson)!;
        Assert.Equal([(1, 2, 2)], itens.Select(i => (i.Item, i.QtdeAmostras, i.Analises.Count)));
        // sugestões da entidade: único endereço/e-mail do cliente
        var e = (await s.EntidadeAsync(c.Cliente.IdEntidades))!;
        Assert.Equal((c.Proposta.IdEntEndLaudo, c.Proposta.IdEntContatoResultado), (e.SugestaoEndLaudo, e.SugestaoContatoResultado));

        // consulta: por número (com ou sem zeros/ano), por cliente e só a última revisão
        var lista = await s.ListarAsync(new PropostaFiltroVM { Numero = $"2/{hoje.Year % 100:00}" });
        Assert.Equal([p.NrProposta], lista.Itens.Select(i => i.NrProposta));
        Assert.Equal(2, (await s.ListarAsync(new PropostaFiltroVM { Cliente = "CLIENTE TESTE" })).Total);
    }

    [Fact]
    public async Task Obrigatorios_com_as_mensagens_do_legado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        async Task<(string?, string?)> Erro(Action<PropostaVM> mudar)
        {
            var vm = await NovaAsync(c);
            mudar(vm);
            var r = await s.GravarAsync(vm);
            Assert.False(r.Sucesso);
            return (r.Mensagem, r.Campo);
        }
        Assert.Equal(("Selecionar o Status para a Proposta.", "IdLabPropostasStatus"), await Erro(v => v.IdLabPropostasStatus = null));
        Assert.Equal(("Selecionar a Empresa para Cobrança.", "IdEntCobranca"), await Erro(v => v.IdEntCobranca = null));
        Assert.Equal(("Selecionar o Contato para Cobrança.", "IdEntContatoNf"), await Erro(v => v.IdEntContatoNf = null));
        Assert.Equal(("Selecionar o endereço para Laudo.", "IdEntEndLaudo"), await Erro(v => v.IdEntEndLaudo = null));
        Assert.Equal(("Selecionar o Contato para Laudo.", "IdEntContatoResultado"), await Erro(v => v.IdEntContatoResultado = null));
        Assert.Equal(("Inclua ao menos um produto com uma análise.", "ItensJson"), await Erro(v => v.ItensJson = "[]"));
        Assert.Equal("A validade não pode ser anterior à data da solicitação.", (await Erro(v => v.DtValidade = v.DtSolicitacao!.Value.AddDays(-1))).Item1);
        // desconto da linha maior que o subtotal (mesma mensagem da TG_PRODAN_BI)
        Assert.EndsWith("Desconto maior que o subtotal da análise.", (await Erro(v =>
        {
            var itens = PropostaService.LerItens(v.ItensJson)!;
            itens[0].Analises[0].VrDesconto = 999;
            v.ItensJson = JsonSerializer.Serialize(itens, Json);
        })).Item1);
        // contato que não é do cliente (o legado não conferia)
        var outro = new Entidade { Categoria = Categoria.Juridica, Nome = "OUTRO", TipoCliente = true };
        c.Db.Add(outro);
        await c.Db.SaveChangesAsync();
        var emailOutro = new EntidadeEmail { IdEntidades = outro.IdEntidades, Email = "outro@teste.com" };
        c.Db.Add(emailOutro);
        await c.Db.SaveChangesAsync();
        Assert.Equal(("Escolha um contato do cliente para o resultado.", "IdEntContatoResultado"),
            await Erro(v => v.IdEntContatoResultado = emailOutro.IdEntidadesEmails));
        // usuário sem profissional não inclui (a proposta guarda quem digitou)
        var semProfissional = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId };
        Assert.StartsWith("Seu usuário não está vinculado a um profissional",
            (await new PropostaService(c.Db, semProfissional, c.Procedures).GravarAsync(await NovaAsync(c))).Mensagem);
    }

    [Fact]
    public async Task Alteracao_sem_revisao_propaga_quantidade_e_recalcula()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var id = (await s.GravarAsync(await NovaAsync(c))).Valor;

        // qtde 2 → 3 (as análises acompanham): 450,00 − 10% = 405,00; tira a 2ª análise: 300,00 − 10% = 270,00
        var vm = (await s.CarregarAsync(id))!;
        var itens = PropostaService.LerItens(vm.ItensJson)!;
        itens[0].QtdeAmostras = 3;
        vm.ItensJson = JsonSerializer.Serialize(itens, Json);
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        Assert.Equal((450m, 405m), await c.Db.LabPropostas.AsNoTracking().Where(p => p.IdLabPropostas == id).Select(p => ValueTuple.Create(p.VrSomaVrAnalis, p.VrTotalProposta)).SingleAsync());
        Assert.All(await c.Db.LabPropostasProdanalises.AsNoTracking().Where(a => a.IdLabPropostas == id).ToListAsync(), a => Assert.Equal(3, a.QtdeAmostras));

        vm = (await s.CarregarAsync(id))!;
        itens = PropostaService.LerItens(vm.ItensJson)!;
        itens[0].Analises.RemoveAt(1);
        vm.ItensJson = JsonSerializer.Serialize(itens, Json);
        vm.PorcDesconto = null;
        vm.VrDesconto = "20,00"; // desconto geral digitado (sem %)
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        Assert.Equal((300m, 20m, 280m), await c.Db.LabPropostas.AsNoTracking().Where(p => p.IdLabPropostas == id)
            .Select(p => ValueTuple.Create(p.VrSomaVrTotAnalis, p.VrDesconto, p.VrTotalProposta)).SingleAsync());
        Assert.Equal(1, await c.Db.LabPropostasProdanalises.CountAsync(a => a.IdLabPropostas == id));
    }

    [Fact]
    public async Task Com_amostra_fica_bloqueada_e_nao_exclui_sem_amostra_exclui()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        c.Db.Add(c.NovaAmostra(await c.HojeAsync()));
        await c.Db.SaveChangesAsync();

        var comAmostra = (await s.CarregarAsync(c.Proposta.IdLabPropostas))!;
        Assert.Equal("Proposta com amostra recebida: só pode ser alterada gerando uma revisão.", comAmostra.Bloqueio);
        Assert.Equal(comAmostra.Bloqueio, (await s.GravarAsync(comAmostra)).Mensagem);
        Assert.Equal("Proposta com amostra recebida não pode ser excluída.", (await s.ExcluirAsync(c.Proposta.IdLabPropostas)).Mensagem);

        var id = (await s.GravarAsync(await NovaAsync(c))).Valor;
        Assert.True((await s.ExcluirAsync(id)).Sucesso);
        Assert.False(await c.Db.LabPropostas.AnyAsync(p => p.IdLabPropostas == id));
        Assert.False(await c.Db.LabPropostasProdutos.AnyAsync(p => p.IdLabPropostas == id)); // cascata
    }

    /// <summary>
    /// 6.1 — revisão (P07, roteiro 12 §7 item 10): R1 com as linhas marcadas como da revisão anterior, status inicial,
    /// sem envio/autorização, contrato copiado; R0 somente leitura; motivo obrigatório; só a vigente se revisa.
    /// </summary>
    [Fact]
    public async Task Revisao_copia_tudo_marca_linhas_e_trava_a_anterior()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var vm = await NovaAsync(c);
        vm.TxContrato = "<p>Cláusula <strong>1</strong></p>";
        var r0 = await s.GravarAsync(vm);
        Assert.True(r0.Sucesso, r0.Mensagem);
        var hoje = await c.HojeAsync();
        await c.Db.LabPropostas.Where(p => p.IdLabPropostas == r0.Valor).ExecuteUpdateAsync(u => u.SetProperty(p => p.DtAutorizacao, hoje));

        Assert.Contains("motivo", (await s.GerarRevisaoAsync(r0.Valor, " ")).Mensagem);
        var r1 = await s.GerarRevisaoAsync(r0.Valor, "Cliente pediu mais um parâmetro");
        Assert.True(r1.Sucesso, r1.Mensagem);
        c.Db.ChangeTracker.Clear();

        var nova = (await s.CarregarAsync(r1.Valor.Id))!;
        Assert.Equal(nova.NrProposta, r1.Valor.NrProposta);   // o número da mensagem, sem reler a proposta
        Assert.Equal($"000002/{hoje.Year % 100:00}-R1", nova.NrProposta);
        Assert.Equal((1, "Cliente pediu mais um parâmetro", (string?)null), (nova.Revisao, nova.MotivoRevisao, nova.Bloqueio));
        Assert.Equal("<p>Cláusula <strong>1</strong></p>", nova.TxContrato);
        var p1 = await c.Db.LabPropostas.AsNoTracking().SingleAsync(p => p.IdLabPropostas == r1.Valor.Id);
        Assert.Equal((null, null, 270m), (p1.DtAutorizacao, p1.DtEnvioCliente, p1.VrTotalProposta));
        Assert.Equal(await c.Db.EmpresasParametros.Select(x => x.IdLabPropostasStatusInicial).SingleAsync(), p1.IdLabPropostasStatus);
        var linhas = PropostaService.LerItens(nova.ItensJson)!.SelectMany(i => i.Analises).ToList();
        Assert.Equal(2, linhas.Count);
        Assert.All(linhas, l => Assert.True(l.StRevAnterior));
        Assert.Equal([1, 0], nova.Revisoes.Select(x => x.Revisao));
        Assert.True(nova.Revisoes[0].Vigente);

        // a R0 ficou só de consulta e não se revisa mais
        var antiga = (await s.CarregarAsync(r0.Valor))!;
        Assert.Equal("Revisão antiga da proposta é somente leitura.", antiga.Bloqueio);
        Assert.Equal("Revisão antiga da proposta é somente leitura.", (await s.GravarAsync(antiga)).Mensagem);
        Assert.Contains("Só é possível revisar a última", (await s.GerarRevisaoAsync(r0.Valor, "outra")).Mensagem);
    }

    /// <summary>6.1 — contrato padrão, troca de produto (proposta + amostras) e PDF com a Autorização de Serviço.</summary>
    [Fact]
    public async Task Contrato_padrao_troca_de_produto_e_pdf()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        await c.Db.EmpresasParametros.ExecuteUpdateAsync(u => u.SetProperty(p => p.TextoPadraoContrato, "<p>Contrato <em>padrão</em></p>"));
        Assert.Equal("<p>Contrato <em>padrão</em></p>", await s.ContratoPadraoAsync());

        // troca na proposta e na amostra recebida por ela
        var amostra = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(amostra);
        await c.Db.SaveChangesAsync();
        var outro = new Produto { Descricao = "QUEIJO PRATO", IdUnidades = c.Unidade.IdUnidades };
        c.Db.Add(outro);
        await c.Db.SaveChangesAsync();
        Assert.Contains("mesmo", (await s.TrocarProdutoAsync(c.Proposta.IdLabPropostas, c.Produto.IdProdutos, c.Produto.IdProdutos)).Mensagem);
        Assert.Contains("não está nesta proposta", (await s.TrocarProdutoAsync(c.Proposta.IdLabPropostas, outro.IdProdutos, c.Produto.IdProdutos)).Mensagem);
        var troca = await s.TrocarProdutoAsync(c.Proposta.IdLabPropostas, c.Produto.IdProdutos, outro.IdProdutos);
        Assert.True(troca.Sucesso, troca.Mensagem);
        Assert.Equal(outro.IdProdutos, await c.Db.LabPropostasProdutos.Where(x => x.IdLabPropostas == c.Proposta.IdLabPropostas).Select(x => x.IdProdutos).SingleAsync());
        Assert.Equal(outro.IdProdutos, await c.Db.LabHistAmostras.Where(x => x.IdLabHistAmostras == amostra.IdLabHistAmostras).Select(x => x.IdProdutos).SingleAsync());

        // PDF: proposta + Autorização de Serviço
        var pdf = await s.ImprimirAsync(c.Proposta.IdLabPropostas);
        Assert.True(pdf.Sucesso, pdf.Mensagem);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Valor.Conteudo, 0, 4));
        Assert.StartsWith($"Proposta {c.Proposta.NrProposta.Replace('/', '-')}", pdf.Valor.Nome);
        var doc = (await s.DocumentoAsync(c.Proposta.IdLabPropostas))!;
        Assert.Equal(("QUEIJO PRATO", 2), (doc.Produtos.Single().Produto, doc.Produtos.Single().Analises.Count));
    }

    /// <summary>6.3 — contrato padrão digitado como texto puro na T05 mantém parágrafos, quebras e "&lt;".</summary>
    [Fact]
    public async Task Contrato_padrao_em_texto_puro_mantem_as_linhas()
    {
        await using var c = await Cenario.CriarAsync();
        await c.Db.EmpresasParametros.ExecuteUpdateAsync(u => u.SetProperty(p => p.TextoPadraoContrato, "CLÁUSULA 1\r\nprazo <5 dias\r\n\r\nCLÁUSULA 2"));
        Assert.Equal("<p>CLÁUSULA 1<br>prazo &lt;5 dias</p><p>CLÁUSULA 2</p>", await Servico(c).ContratoPadraoAsync());
    }

    /// <summary>
    /// 6.3 — trocar produto com amostra revisada: só a revisão vigente muda (a antiga é somente leitura e antes fazia
    /// a troca inteira falhar) e o item do pedido acompanha (a T21 mostra o produto do item).
    /// </summary>
    [Fact]
    public async Task Trocar_produto_com_amostra_revisada_e_pedido()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amostra, _, _) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var rev = await c.Procedures.GerarRevisaoAmostraAsync(amostra.IdLabHistAmostras, "LOTE ERRADO");
        Assert.True(rev.Sucesso, rev.Mensagem);
        var pedido = await new PedidoVendaService(c.Db, c.Procedures, s, new ServicoAuditoria(c.Db, c.Ativa))
            .GerarAsync(new PedidoVendaGerarVM { IdProposta = c.Proposta.IdLabPropostas, DtAutorizacao = await c.HojeAsync() });
        Assert.True(pedido.Sucesso, pedido.Mensagem);
        var outro = new Produto { Descricao = "QUEIJO PRATO", IdUnidades = c.Unidade.IdUnidades };
        c.Db.Add(outro);
        await c.Db.SaveChangesAsync();

        var troca = await s.TrocarProdutoAsync(c.Proposta.IdLabPropostas, c.Produto.IdProdutos, outro.IdProdutos);
        Assert.True(troca.Sucesso, troca.Mensagem);
        var produtos = await c.Db.LabHistAmostras.AsNoTracking().OrderBy(a => a.Revisao).Select(a => a.IdProdutos).ToListAsync();
        Assert.Equal([c.Produto.IdProdutos, outro.IdProdutos], produtos);   // revisão antiga intacta, vigente trocada
        Assert.Equal(outro.IdProdutos, await c.Db.PedidosVendaItens.Where(i => i.IdPedidosVenda == pedido.Valor.IdPedido).Select(i => i.IdProdutos).SingleAsync());
        Assert.Equal("QUEIJO PRATO", await c.Db.VwPedidosVendaSaldo.Where(i => i.IdPedidosVenda == pedido.Valor.IdPedido).Select(i => i.ProdutoDescricao).SingleAsync());
    }

    [Fact]
    public void Html_dos_editores_vira_paragrafos_do_pdf()
    {
        var p = LabControl.Services.Propostas.PropostaPdf.TextoSimples("<p>Primeiro <strong>negrito</strong></p><ul><li>um</li><li>dois</li></ul>linha<br>quebrada");
        Assert.Equal([(null, "Primeiro negrito"), ("•", "um"), ("•", "dois"), (null, "linha"), (null, "quebrada")], p);
    }
}
