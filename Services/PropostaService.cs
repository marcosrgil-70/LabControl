using System.Text.Json;
using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T20 — Propostas (4.2 essencial; 6.1 completa — P05/P06/P07): consulta, inclusão, alteração sem revisão, exclusão,
/// revisão (<c>SP_GERAR_REVISAO_PROPOSTA</c>), contrato, troca de produto (<c>SP_TROCAR_PRODUTO_PROPOSTA</c>) e PDF.
/// Cabeçalho, textos, produtos e análises gravam juntos numa transação. Número/ano pelo banco (<c>TG_PROPOSTAS_BI</c>),
/// item pelo <c>TG_PROPPROD_BI</c>, valores da linha pelo <c>TG_PRODAN_BI/BU</c> e totais pelo <c>SP_RECALCULAR_PROPOSTA</c>
/// — a tela só recalcula para exibir.
/// </summary>
public sealed partial class PropostaService(LabDbContext db, IEmpresaContexto ctx, Procedures procedures)
{
    public const string MensagemNaoEncontrada = "A proposta não foi encontrada (pode ter sido excluída).";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ------------------------------------------------------------------ consulta

    public async Task<PropostaListaVM> ListarAsync(PropostaFiltroVM f)
    {
        var q = db.VwLabPropostas.AsNoTracking();
        if (f.SoUltima) q = q.Where(p => p.UltimaRevisao);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(p => p.ClienteNome.Contains(c) || (p.ClienteFantasia != null && p.ClienteFantasia.Contains(c))
                             || (p.ClienteCpfCnpj != null && p.ClienteCpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Numero) is { } n && RegexNumero().Match(n) is { Success: true } m)
        {
            var cod = int.Parse(m.Groups["cod"].Value);
            q = q.Where(p => p.CodProposta == cod);
            if (m.Groups["ano"].Success && short.TryParse(m.Groups["ano"].Value, out var a))
            {
                var ano = (short)(a < 100 ? 2000 + a : a);
                q = q.Where(p => p.AnoProposta == ano);
            }
        }
        if (f.NrPedido is { } ped) q = q.Where(p => p.NrPedido == ped);
        if (f.IdStatus is { } st) q = q.Where(p => p.IdLabPropostasStatus == st);
        if (f.De is { } de) q = q.Where(p => p.DtSolicitacao >= de);
        if (f.Ate is { } ate) q = q.Where(p => p.DtSolicitacao <= ate);

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, PropostaFiltroVM.TamanhoPagina);
        var itens = await q.OrderByDescending(p => p.AnoProposta).ThenByDescending(p => p.CodProposta).ThenByDescending(p => p.RevProposta)
            .Skip((f.Pagina - 1) * PropostaFiltroVM.TamanhoPagina).Take(PropostaFiltroVM.TamanhoPagina)
            .Select(p => new PropostaListaItemVM(p.IdLabPropostas, p.NrProposta, p.UltimaRevisao, p.DtSolicitacao, p.Status, p.CorStatus,
                p.ClienteCpfCnpj, p.ClienteNome, p.NrPedidoFmt, p.VrTotalProposta))
            .ToListAsync();
        var status = await db.LabPropostasStatus.AsNoTracking().OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdLabPropostasStatus, s.Descricao)).ToListAsync();
        return new PropostaListaVM { Filtro = f, Itens = itens, Status = status, Total = total };
    }

    [GeneratedRegex(@"^0*(?<cod>\d{1,9})(?:\s*/\s*(?<ano>\d{2}|\d{4}))?(?:\s*-\s*R\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexNumero();

    // ------------------------------------------------------------------ formulário

    /// <summary>Nova proposta: status inicial, hoje e validade = hoje + dias da empresa (P05 › incluir).</summary>
    public async Task<PropostaVM> NovaAsync()
    {
        var par = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        var hoje = await db.HojeAsync();
        var vm = new PropostaVM
        {
            DtSolicitacao = hoje, DtValidade = hoje.AddDays(par?.DiasValidadeProposta ?? 30),
            IdLabPropostasStatus = par?.IdLabPropostasStatusInicial, ItensJson = "[]",
        };
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<PropostaVM?> CarregarAsync(int id)
    {
        var p = await db.LabPropostas.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        if (p is null) return null;
        var t = await db.LabPropostasTextos.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        var produtos = await db.LabPropostasProdutos.AsNoTracking().Where(x => x.IdLabPropostas == id).OrderBy(x => x.Item).ToListAsync();
        var analises = await db.LabPropostasProdanalises.AsNoTracking().Where(x => x.IdLabPropostas == id)
            .OrderBy(x => x.IdLabPropostasProdanalises).ToListAsync();
        var itens = produtos.Select(x => new PropostaItemVM
        {
            Id = x.IdLabPropostasProdutos, Item = x.Item, IdProdutos = x.IdProdutos, QtdeAmostras = x.QtdeAmostras,
            Analises = analises.Where(a => a.IdLabPropostasProdutos == x.IdLabPropostasProdutos).Select(a => new PropostaAnaliseVM
            {
                Id = a.IdLabPropostasProdanalises, IdParametro = a.IdLabParametrosAnalises, IdMetodo = a.IdAnalisesMetodos,
                IdIdioma = a.IdIdiomas, IdPrazo = a.IdPrazos, TipoDocumento = EnumBanco<TipoDocumento>.ParaBanco(a.TipoDocumento), VrUnitario = a.VrUnitario,
                PorcDesconto = a.PorcDesconto, VrDesconto = a.VrDesconto, StRevAnterior = a.StRevAnterior,
            }).ToList(),
        }).ToList();
        var vm = new PropostaVM
        {
            Id = p.IdLabPropostas, NrProposta = p.NrProposta, DtEnvioCliente = p.DtEnvioCliente, Bloqueio = await BloqueioAsync(p),
            Revisao = p.RevProposta, MotivoRevisao = p.MotivoRevisao, UltimaRevisao = p.UltimaRevisao,
            Revisoes = await db.VwLabPropostas.AsNoTracking().Where(v => v.AnoProposta == p.AnoProposta && v.CodProposta == p.CodProposta)
                .OrderByDescending(v => v.RevProposta)
                .Select(v => new PropostaRevisaoVM(v.IdLabPropostas, v.NrProposta, v.RevProposta, v.Status, v.DtValidade, v.VrTotalProposta, v.MotivoRevisao, v.UltimaRevisao))
                .ToListAsync(),
            NrPedido = await db.VwLabPropostas.AsNoTracking().Where(v => v.IdLabPropostas == id).Select(v => v.NrPedidoFmt).SingleOrDefaultAsync(),
            DtSolicitacao = p.DtSolicitacao, DtValidade = p.DtValidade, IdLabPropostasStatus = p.IdLabPropostasStatus, DtAutorizacao = p.DtAutorizacao,
            IdEntidadesComerc = p.IdEntidadesComerc, IdEntidades = p.IdEntidades, IdEntContato = p.IdEntContato,
            IdEntEndLaudo = p.IdEntEndLaudo, IdEntContatoResultado = p.IdEntContatoResultado,
            IdEntCobranca = p.IdEntCobranca, IdEntEndCobranca = p.IdEntEndCobranca, IdEntContatoNf = p.IdEntContatoNf,
            PorcDesconto = Formatar(p.PorcDesconto, "0.####"), VrDesconto = Formatar(p.VrDesconto, "0.00"),
            IdCondicoesPagtos = p.IdCondicoesPagtos, IdMoedas = p.IdMoedas,
            VrSomaVrAnalis = p.VrSomaVrAnalis, VrSomaDescAnalis = p.VrSomaDescAnalis, VrSomaVrTotAnalis = p.VrSomaVrTotAnalis, VrTotalProposta = p.VrTotalProposta,
            TxItensSolicitados = HtmlSimples.Limpar(t?.TxItensSolicitados), TxItensAdquiridos = HtmlSimples.Limpar(t?.TxItensAdquiridos),
            TxObservacao = HtmlSimples.Limpar(t?.TxObservacao), TxContrato = HtmlSimples.Limpar(t?.TxContrato), ItensJson = JsonSerializer.Serialize(itens, Json),
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>
    /// Alteração direta só na revisão vigente ainda não enviada, sem pedido e sem amostra (P05 › alterar); nos demais
    /// casos a alteração é por revisão (T20 completa, 6.1).
    /// </summary>
    private async Task<string?> BloqueioAsync(LabProposta p)
    {
        if (!p.UltimaRevisao) return "Revisão antiga da proposta é somente leitura.";
        if (await db.PedidosVenda.AnyAsync(x => x.AnoProposta == p.AnoProposta && x.CodProposta == p.CodProposta))
            return "Proposta com pedido de venda: só pode ser alterada gerando uma revisão.";
        if (await db.LabHistAmostras.AnyAsync(a => a.LabProposta.AnoProposta == p.AnoProposta && a.LabProposta.CodProposta == p.CodProposta))
            return "Proposta com amostra recebida: só pode ser alterada gerando uma revisão.";
        if (p.DtEnvioCliente is not null) return "Proposta já enviada ao cliente: só pode ser alterada gerando uma revisão.";
        return null;
    }

    /// <summary>Combos (ativos + os já escolhidos) e os contatos/endereços do cliente e da cobrança escolhidos.</summary>
    public async Task PrepararAsync(PropostaVM vm)
    {
        var itens = LerItens(vm.ItensJson) ?? [];
        var produtosUsados = itens.Select(i => i.IdProdutos).ToList();
        var analises = itens.SelectMany(i => i.Analises).ToList();
        var (parametros, metodos, idiomas, prazos) = (analises.Select(a => a.IdParametro).ToList(), analises.Select(a => a.IdMetodo).ToList(),
            analises.Select(a => a.IdIdioma).ToList(), analises.Select(a => a.IdPrazo).ToList());
        int? cliente = vm.IdEntidades, cobranca = vm.IdEntCobranca, comercial = vm.IdEntidadesComerc;

        var o = vm.Opcoes;
        o.Status = await db.LabPropostasStatus.AsNoTracking().Where(s => s.Ativo || s.IdLabPropostasStatus == vm.IdLabPropostasStatus)
            .OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdLabPropostasStatus, s.Descricao)).ToListAsync();
        o.Clientes = (await db.Entidades.AsNoTracking()
            .Where(e => (e.TipoCliente && !e.Inativo) || e.IdEntidades == cliente || e.IdEntidades == cobranca)
            .OrderBy(e => e.Nome).Select(e => new { e.IdEntidades, e.Nome, e.CpfCnpj }).ToListAsync())
            .Select(e => new OpcaoVM(e.IdEntidades, e.CpfCnpj is null ? e.Nome : $"{e.Nome} — {e.CpfCnpj}")).ToList();
        o.Comerciais = await db.Entidades.AsNoTracking()
            .Where(e => ((e.TipoVendedor || e.TipoFuncionario) && !e.Inativo) || e.IdEntidades == comercial)
            .OrderBy(e => e.Nome).Select(e => new OpcaoVM(e.IdEntidades, e.Nome)).ToListAsync();
        o.Condicoes = await db.CondicoesPagtos.AsNoTracking().Where(x => x.Ativo || x.IdCondicoesPagtos == vm.IdCondicoesPagtos)
            .OrderBy(x => x.Codigo).Select(x => new OpcaoVM(x.IdCondicoesPagtos, x.Codigo + " — " + x.Descricao)).ToListAsync();
        o.Moedas = await db.Moedas.AsNoTracking().Where(x => x.Ativo || x.IdMoedas == vm.IdMoedas)
            .OrderBy(x => x.Descricao).Select(x => new OpcaoVM(x.IdMoedas, x.Sigla + " — " + x.Descricao)).ToListAsync();
        o.Unidades = await db.Unidades.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Descricao).Select(x => new OpcaoVM(x.IdUnidades, x.Descricao)).ToListAsync();
        o.Produtos = (await db.Produtos.AsNoTracking().Where(x => !x.Inativo || produtosUsados.Contains(x.IdProdutos))
            .OrderBy(x => x.Descricao).Select(x => new { x.IdProdutos, x.Codigo, x.Descricao, Unidade = x.Unidade.Sigla }).ToListAsync())
            .Select(x => new OpcaoExtraVM(x.IdProdutos, $"{x.Descricao} ({x.Codigo})", new Dictionary<string, object?> { ["unidade"] = x.Unidade })).ToList();
        o.Parametros = (await db.LabParametrosAnalises.AsNoTracking().Where(x => !x.Inativo || parametros.Contains(x.IdLabParametrosAnalises))
            .OrderBy(x => x.Descricao)
            .Select(x => new { x.IdLabParametrosAnalises, x.Descricao, Tipo = x.AnaliseTipo.Descricao, x.VrUnitario, x.IdAnalisesMetodos }).ToListAsync())
            .Select(x => new OpcaoExtraVM(x.IdLabParametrosAnalises, x.Descricao,
                new Dictionary<string, object?> { ["tipo"] = x.Tipo, ["unitario"] = x.VrUnitario, ["metodo"] = x.IdAnalisesMetodos })).ToList();
        o.Metodos = await db.AnalisesMetodos.AsNoTracking().Where(x => x.Ativo || metodos.Contains(x.IdAnalisesMetodos))
            .OrderBy(x => x.Descricao).Select(x => new OpcaoVM(x.IdAnalisesMetodos, x.Descricao)).ToListAsync();
        o.Idiomas = await db.Idiomas.AsNoTracking().Where(x => x.Ativo || idiomas.Contains(x.IdIdiomas))
            .OrderBy(x => x.Descricao).Select(x => new OpcaoVM(x.IdIdiomas, x.Descricao)).ToListAsync();
        o.Prazos = await db.Prazos.AsNoTracking().Where(x => x.Ativo || prazos.Contains(x.IdPrazos))
            .OrderBy(x => x.QtdeDias).ThenBy(x => x.Descricao).Select(x => new OpcaoVM(x.IdPrazos, x.Descricao)).ToListAsync();
        vm.TemContratoPadrao = await ContratoPadraoAsync() is not null;
        o.Cliente = cliente is { } cl ? await EntidadeAsync(cl, vm.IdEntContato, vm.IdEntEndLaudo, vm.IdEntContatoResultado) : null;
        o.Cobranca = cobranca is { } cb ? await EntidadeAsync(cb, vm.IdEntEndCobranca, vm.IdEntContatoNf) : null;
    }

    /// <summary>
    /// Contatos (e-mails) e endereços da entidade, ativos + os já escolhidos, com as sugestões da T20: laudo = finalidade
    /// CORRESPONDENCIA, cobrança = COBRANCA/NOTA_FISCAL, contato do resultado = ENVIAR_LAUDO, contato da NF = ENVIAR_NFE
    /// (senão o principal, senão o primeiro).
    /// </summary>
    public async Task<PropostaEntidadeVM?> EntidadeAsync(int id, params int?[] manter)
    {
        var e = await db.Entidades.AsNoTracking().Where(x => x.IdEntidades == id).Select(x => new { x.IdEntidades, x.CpfCnpj }).SingleOrDefaultAsync();
        if (e is null) return null;
        var ids = manter.Where(x => x is not null).Select(x => x!.Value).ToList();
        var emails = await db.EntidadesEmails.AsNoTracking().Where(x => x.IdEntidades == id && (!x.Inativo || ids.Contains(x.IdEntidadesEmails)))
            .OrderByDescending(x => x.Principal).ThenBy(x => x.NomeContato).ThenBy(x => x.Email).ToListAsync();
        var enderecos = await db.EntidadesEnderecos.AsNoTracking().Where(x => x.IdEntidades == id && (!x.Inativo || ids.Contains(x.IdEntidadesEnderecos)))
            .OrderBy(x => x.EnderecoTipo.Descricao)
            .Select(x => new
            {
                x.IdEntidadesEnderecos, Tipo = x.EnderecoTipo.Descricao, x.EnderecoTipo.Finalidade, x.Logradouro, x.Numero, x.Complemento, x.Bairro, x.Cep,
                Cidade = x.Cidade.Nome, Uf = x.Cidade.Estado.Sigla,
            }).ToListAsync();
        var fone = await db.EntidadesFones.AsNoTracking().Where(x => x.IdEntidades == id && !x.Inativo)
            .OrderByDescending(x => x.Principal).Select(x => "(" + x.Ddd + ") " + x.Fone).FirstOrDefaultAsync();

        // o 1º que atende à preferência mais forte; senão o primeiro da lista
        static int? Escolher<T>(IEnumerable<T> lista, Func<T, int> id, params Func<T, bool>[] preferencias) =>
            preferencias.Select(p => lista.Where(p).Select(id).Cast<int?>().FirstOrDefault()).FirstOrDefault(x => x is not null)
            ?? lista.Select(id).Cast<int?>().FirstOrDefault();
        return new PropostaEntidadeVM(e.IdEntidades, e.CpfCnpj, fone,
            emails.Select(x => new PropostaContatoVM(x.IdEntidadesEmails, x.NomeContato is null ? x.Email : $"{x.NomeContato} <{x.Email}>", x.Email, x.Departamento)).ToList(),
            enderecos.Select(x => new PropostaEnderecoVM(x.IdEntidadesEnderecos, $"{x.Tipo}: {x.Logradouro}{(x.Numero is null ? "" : ", " + x.Numero)}",
                string.Join(" · ", new[] { x.Complemento, x.Bairro, $"{x.Cidade}/{x.Uf}", x.Cep is null ? null : "CEP " + x.Cep }.Where(s => !string.IsNullOrWhiteSpace(s))))).ToList(),
            Escolher(enderecos, x => x.IdEntidadesEnderecos, x => x.Finalidade == FinalidadeEndereco.Correspondencia),
            Escolher(enderecos, x => x.IdEntidadesEnderecos, x => x.Finalidade == FinalidadeEndereco.Cobranca, x => x.Finalidade == FinalidadeEndereco.NotaFiscal),
            Escolher(emails, x => x.IdEntidadesEmails, x => x.EnviarLaudo, x => x.Principal),
            Escolher(emails, x => x.IdEntidadesEmails, x => x.EnviarNfe, x => x.Principal));
    }

    // ------------------------------------------------------------------ gravação

    public static List<PropostaItemVM>? LerItens(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<PropostaItemVM>>(json, Json); }
        catch (JsonException) { return null; }
    }

    /// <summary>Inclui (Id = 0) ou altera sem revisão; devolve o ID. Mensagens dos obrigatórios iguais às do legado (P05).</summary>
    public async Task<Resultado<int>> GravarAsync(PropostaVM vm)
    {
        // bloqueada (revisão antiga, enviada, com pedido ou amostra): isso vem antes de qualquer campo
        var atual = vm.Id == 0 ? null : await db.LabPropostas.SingleOrDefaultAsync(p => p.IdLabPropostas == vm.Id);
        if (vm.Id != 0 && atual is null) return Resultado.Erro(MensagemNaoEncontrada);
        if (atual is not null && await BloqueioAsync(atual) is { } bloqueio) return Resultado.Erro(bloqueio);

        if (vm.IdLabPropostasStatus is not { } status) return Resultado.Erro("Selecionar o Status para a Proposta.", nameof(vm.IdLabPropostasStatus));
        if (vm.IdEntidades is not { } cliente) return Resultado.Erro("Selecionar o Cliente.", nameof(vm.IdEntidades));
        if (vm.IdEntCobranca is not { } cobranca) return Resultado.Erro("Selecionar a Empresa para Cobrança.", nameof(vm.IdEntCobranca));
        if (vm.IdEntContatoNf is not { } contatoNf) return Resultado.Erro("Selecionar o Contato para Cobrança.", nameof(vm.IdEntContatoNf));
        if (vm.IdEntEndCobranca is not { } endCobranca) return Resultado.Erro("Selecionar o endereço para Cobrança.", nameof(vm.IdEntEndCobranca));
        if (vm.IdEntEndLaudo is not { } endLaudo) return Resultado.Erro("Selecionar o endereço para Laudo.", nameof(vm.IdEntEndLaudo));
        if (vm.IdEntContatoResultado is not { } contatoRes) return Resultado.Erro("Selecionar o Contato para Laudo.", nameof(vm.IdEntContatoResultado));
        if (vm.IdCondicoesPagtos is not { } condicao) return Resultado.Erro("Selecionar a Condição de Pagamento.", nameof(vm.IdCondicoesPagtos));
        var hoje = await db.HojeAsync();
        var solicitacao = vm.DtSolicitacao ?? hoje;
        if (vm.DtValidade < solicitacao) return Resultado.Erro("A validade não pode ser anterior à data da solicitação.", nameof(vm.DtValidade));
        if (!LerPercentual(vm.PorcDesconto, out var porc)) return Resultado.Erro("% de desconto geral: de 0 a 100, até 4 casas decimais.", nameof(vm.PorcDesconto));
        if (!LerValor(vm.VrDesconto, out var vrDesc)) return Resultado.Erro("Desconto geral: valor em reais, maior ou igual a zero.", nameof(vm.VrDesconto));

        int? digitador = atual?.IdEntidadesFunc ?? ctx.ProfissionalId;
        if (digitador is null)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional (Usuários › profissional): a proposta registra quem digitou.");

        // combos: da empresa ativa, ativos ou os já gravados na proposta
        if (!await db.LabPropostasStatus.AnyAsync(s => s.IdLabPropostasStatus == status && (s.Ativo || s.IdLabPropostasStatus == (atual == null ? 0 : atual.IdLabPropostasStatus))))
            return Resultado.Erro("Escolha um status da lista.", nameof(vm.IdLabPropostasStatus));
        if (!await db.CondicoesPagtos.AnyAsync(s => s.IdCondicoesPagtos == condicao && (s.Ativo || s.IdCondicoesPagtos == (atual == null ? 0 : atual.IdCondicoesPagtos))))
            return Resultado.Erro("Escolha uma condição de pagamento da lista.", nameof(vm.IdCondicoesPagtos));
        if (vm.IdMoedas is { } moeda && !await db.Moedas.AnyAsync(s => s.IdMoedas == moeda))
            return Resultado.Erro("Escolha uma moeda da lista.", nameof(vm.IdMoedas));
        if (!await db.Entidades.AnyAsync(e => e.IdEntidades == cliente && e.TipoCliente))
            return Resultado.Erro("Escolha um cliente da lista.", nameof(vm.IdEntidades));
        if (!await db.Entidades.AnyAsync(e => e.IdEntidades == cobranca))
            return Resultado.Erro("Escolha a empresa de cobrança da lista.", nameof(vm.IdEntCobranca));
        if (vm.IdEntidadesComerc is { } comerc && !await db.Entidades.AnyAsync(e => e.IdEntidades == comerc && (e.TipoVendedor || e.TipoFuncionario)))
            return Resultado.Erro("Escolha o comercial da lista.", nameof(vm.IdEntidadesComerc));
        // contatos e endereços: do cliente (resultado) e da entidade de cobrança (NF) — o legado não conferia
        if (vm.IdEntContato is { } contato && !await db.EntidadesEmails.AnyAsync(x => x.IdEntidadesEmails == contato && x.IdEntidades == cliente))
            return Resultado.Erro("Escolha um contato do cliente.", nameof(vm.IdEntContato));
        if (!await db.EntidadesEnderecos.AnyAsync(x => x.IdEntidadesEnderecos == endLaudo && x.IdEntidades == cliente))
            return Resultado.Erro("Escolha um endereço do cliente para o resultado.", nameof(vm.IdEntEndLaudo));
        if (!await db.EntidadesEmails.AnyAsync(x => x.IdEntidadesEmails == contatoRes && x.IdEntidades == cliente))
            return Resultado.Erro("Escolha um contato do cliente para o resultado.", nameof(vm.IdEntContatoResultado));
        if (!await db.EntidadesEnderecos.AnyAsync(x => x.IdEntidadesEnderecos == endCobranca && x.IdEntidades == cobranca))
            return Resultado.Erro("Escolha um endereço da empresa de cobrança.", nameof(vm.IdEntEndCobranca));
        if (!await db.EntidadesEmails.AnyAsync(x => x.IdEntidadesEmails == contatoNf && x.IdEntidades == cobranca))
            return Resultado.Erro("Escolha um contato da empresa de cobrança.", nameof(vm.IdEntContatoNf));

        // produtos e análises
        if (LerItens(vm.ItensJson) is not { } itens) return Resultado.Erro("Produtos e análises inválidos: recarregue a página.");
        if (await ValidarItensAsync(itens, atual) is { } erroItens) return erroItens;
        var totalAnalises = itens.SelectMany(i => i.Analises.Select(a => TotalDaLinha(i.QtdeAmostras, a))).Sum();
        if (porc > 0) vrDesc = decimal.Round(totalAnalises * porc / 100, 2);
        if (vrDesc > totalAnalises) return Resultado.Erro("Desconto geral maior que o total das análises.", nameof(vm.VrDesconto));

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var p = atual ?? db.LabPropostas.Add(new LabProposta { IdEntidadesFunc = digitador.Value }).Entity;
            p.DtSolicitacao = solicitacao;
            p.DtValidade = vm.DtValidade;
            p.IdLabPropostasStatus = status;
            p.DtAutorizacao = vm.DtAutorizacao;
            p.IdEntidadesComerc = vm.IdEntidadesComerc;
            p.IdEntidades = cliente;
            p.IdEntContato = vm.IdEntContato;
            (p.IdEntEndLaudo, p.IdEntContatoResultado) = (endLaudo, contatoRes);
            (p.IdEntCobranca, p.IdEntEndCobranca, p.IdEntContatoNf) = (cobranca, endCobranca, contatoNf);
            (p.PorcDesconto, p.VrDesconto, p.IdCondicoesPagtos, p.IdMoedas) = (porc, vrDesc, condicao, vm.IdMoedas);
            await db.SaveChangesAsync(); // inclusão: o banco numera (ano do servidor) e o EF relê

            var textos = await db.LabPropostasTextos.SingleOrDefaultAsync(x => x.IdLabPropostas == p.IdLabPropostas)
                         ?? db.LabPropostasTextos.Add(new LabPropostaTextos { IdLabPropostas = p.IdLabPropostas }).Entity;
            textos.TxItensSolicitados = HtmlSimples.Limpar(vm.TxItensSolicitados);
            textos.TxItensAdquiridos = HtmlSimples.Limpar(vm.TxItensAdquiridos);
            textos.TxObservacao = HtmlSimples.Limpar(vm.TxObservacao);
            textos.TxContrato = HtmlSimples.Limpar(vm.TxContrato);

            await GravarItensAsync(p.IdLabPropostas, itens);
            // desconto geral sem mexer nas análises também recalcula o cabeçalho (P06)
            var rec = await procedures.RecalcularPropostaAsync(p.IdLabPropostas);
            if (!rec.Sucesso) throw new ErroDeNegocioException(rec.Mensagem!);
            await tx.CommitAsync();
            return p.IdLabPropostas;
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    private async Task<Falha?> ValidarItensAsync(List<PropostaItemVM> itens, LabProposta? atual)
    {
        if (itens.Count == 0 || itens.All(i => i.Analises.Count == 0))
            return Resultado.Erro("Inclua ao menos um produto com uma análise.", nameof(PropostaVM.ItensJson));
        var idsAtuais = atual is null ? [] : await db.LabPropostasProdutos.Where(x => x.IdLabPropostas == atual.IdLabPropostas)
            .Select(x => new { x.IdLabPropostasProdutos, x.IdProdutos }).ToDictionaryAsync(x => x.IdLabPropostasProdutos, x => x.IdProdutos);
        var analisesAtuais = atual is null ? [] : await db.LabPropostasProdanalises.Where(x => x.IdLabPropostas == atual.IdLabPropostas)
            .Select(x => x.IdLabPropostasProdanalises).ToListAsync();
        var produtos = itens.Select(i => i.IdProdutos).Distinct().ToList();
        var validos = await db.Produtos.Where(x => produtos.Contains(x.IdProdutos))
            .Select(x => new { x.IdProdutos, x.Inativo, x.Descricao }).ToDictionaryAsync(x => x.IdProdutos);
        var analises = itens.SelectMany(i => i.Analises).ToList();
        var parametros = await IdsAsync(db.LabParametrosAnalises.Where(x => !x.Inativo).Select(x => x.IdLabParametrosAnalises), analises.Select(a => a.IdParametro));
        var metodos = await IdsAsync(db.AnalisesMetodos.Where(x => x.Ativo).Select(x => x.IdAnalisesMetodos), analises.Select(a => a.IdMetodo));
        var idiomas = await IdsAsync(db.Idiomas.Where(x => x.Ativo).Select(x => x.IdIdiomas), analises.Select(a => a.IdIdioma));
        var prazos = await IdsAsync(db.Prazos.Where(x => x.Ativo).Select(x => x.IdPrazos), analises.Select(a => a.IdPrazo));
        // linhas que já estavam gravadas podem manter um cadastro inativado depois
        var gravadas = atual is null ? [] : await db.LabPropostasProdanalises.Where(x => x.IdLabPropostas == atual.IdLabPropostas)
            .Select(x => new { x.IdLabParametrosAnalises, x.IdAnalisesMetodos, x.IdIdiomas, x.IdPrazos }).ToListAsync();
        parametros.UnionWith(gravadas.Select(g => g.IdLabParametrosAnalises));
        metodos.UnionWith(gravadas.Select(g => g.IdAnalisesMetodos));
        idiomas.UnionWith(gravadas.Select(g => g.IdIdiomas));
        prazos.UnionWith(gravadas.Select(g => g.IdPrazos));

        foreach (var i in itens)
        {
            if (!validos.TryGetValue(i.IdProdutos, out var prod) || (prod.Inativo && idsAtuais.GetValueOrDefault(i.Id) != i.IdProdutos))
                return Resultado.Erro("Escolha um produto da lista.", nameof(PropostaVM.ItensJson));
            if (i.Id != 0 && !idsAtuais.ContainsKey(i.Id)) return Resultado.Erro("Produto que não é desta proposta: recarregue a página.", nameof(PropostaVM.ItensJson));
            if (i.QtdeAmostras <= 0) return Resultado.Erro($"{prod.Descricao}: a quantidade de amostras deve ser maior que zero.", nameof(PropostaVM.ItensJson));
            if (i.Analises.Count == 0) return Resultado.Erro($"{prod.Descricao}: inclua ao menos uma análise.", nameof(PropostaVM.ItensJson));
            foreach (var a in i.Analises)
            {
                if (a.Id != 0 && !analisesAtuais.Contains(a.Id)) return Resultado.Erro("Análise que não é desta proposta: recarregue a página.", nameof(PropostaVM.ItensJson));
                if (!parametros.Contains(a.IdParametro)) return Resultado.Erro($"{prod.Descricao}: escolha o parâmetro da lista.", nameof(PropostaVM.ItensJson));
                if (!metodos.Contains(a.IdMetodo)) return Resultado.Erro($"{prod.Descricao}: escolha o método da análise.", nameof(PropostaVM.ItensJson));
                if (!idiomas.Contains(a.IdIdioma)) return Resultado.Erro($"{prod.Descricao}: escolha o idioma da análise.", nameof(PropostaVM.ItensJson));
                if (!prazos.Contains(a.IdPrazo)) return Resultado.Erro($"{prod.Descricao}: escolha o prazo da análise.", nameof(PropostaVM.ItensJson));
                if (a.TipoDocumento is not ("BOLETIM" or "RELATORIO")) return Resultado.Erro($"{prod.Descricao}: tipo de documento inválido.", nameof(PropostaVM.ItensJson));
                if (a.VrUnitario < 0 || a.VrUnitario != decimal.Round(a.VrUnitario, 2) || a.PorcDesconto is < 0 or > 100 || a.VrDesconto < 0)
                    return Resultado.Erro($"{prod.Descricao}: valores da análise inválidos.", nameof(PropostaVM.ItensJson));
                var subtotal = decimal.Round(i.QtdeAmostras * a.VrUnitario, 2);
                if ((a.PorcDesconto > 0 ? decimal.Round(subtotal * a.PorcDesconto / 100, 2) : a.VrDesconto) > subtotal)
                    return Resultado.Erro($"{prod.Descricao}: Desconto maior que o subtotal da análise.", nameof(PropostaVM.ItensJson));
            }
        }
        return null;
    }

    private static async Task<HashSet<int>> IdsAsync(IQueryable<int> ativos, IEnumerable<int> pedidos)
    {
        var lista = pedidos.Distinct().ToList();
        return (await ativos.Where(id => lista.Contains(id)).ToListAsync()).ToHashSet();
    }

    /// <summary>Mesma conta do <c>TG_PRODAN_BI</c> (P06), para validar o desconto geral antes de gravar.</summary>
    private static decimal TotalDaLinha(int qtde, PropostaAnaliseVM a)
    {
        var subtotal = decimal.Round(qtde * a.VrUnitario, 2);
        return subtotal - (a.PorcDesconto > 0 ? decimal.Round(subtotal * a.PorcDesconto / 100, 2) : a.VrDesconto);
    }

    // produtos/análises: o que saiu da tela é excluído, o que mudou é alterado, o novo é incluído (item pelo banco)
    private async Task GravarItensAsync(int idProposta, List<PropostaItemVM> itens)
    {
        var produtos = await db.LabPropostasProdutos.Where(x => x.IdLabPropostas == idProposta).ToDictionaryAsync(x => x.IdLabPropostasProdutos);
        var analises = await db.LabPropostasProdanalises.Where(x => x.IdLabPropostas == idProposta).ToDictionaryAsync(x => x.IdLabPropostasProdanalises);
        var ficam = itens.SelectMany(i => i.Analises).Where(a => a.Id != 0).Select(a => a.Id).ToHashSet();
        db.LabPropostasProdanalises.RemoveRange(analises.Values.Where(a => !ficam.Contains(a.IdLabPropostasProdanalises)));
        var produtosFicam = itens.Where(i => i.Id != 0).Select(i => i.Id).ToHashSet();
        db.LabPropostasProdutos.RemoveRange(produtos.Values.Where(p => !produtosFicam.Contains(p.IdLabPropostasProdutos)));
        await db.SaveChangesAsync();

        var novos = new List<(PropostaItemVM Vm, LabPropostaProduto Entidade)>();
        foreach (var i in itens)
        {
            var p = i.Id != 0 ? produtos[i.Id] : db.LabPropostasProdutos.Add(new LabPropostaProduto { IdLabPropostas = idProposta }).Entity;
            (p.IdProdutos, p.QtdeAmostras) = (i.IdProdutos, i.QtdeAmostras);
            novos.Add((i, p));
        }
        await db.SaveChangesAsync(); // a TG_PROPPROD_AU leva a quantidade nova às análises que ficaram

        foreach (var (i, p) in novos)
            foreach (var a in i.Analises)
            {
                var linha = a.Id != 0 ? analises[a.Id]
                    : db.LabPropostasProdanalises.Add(new LabPropostaProdAnalise { IdLabPropostas = idProposta, IdLabPropostasProdutos = p.IdLabPropostasProdutos }).Entity;
                linha.IdLabPropostasProdutos = p.IdLabPropostasProdutos;
                (linha.IdLabParametrosAnalises, linha.IdAnalisesMetodos, linha.IdIdiomas, linha.IdPrazos) = (a.IdParametro, a.IdMetodo, a.IdIdioma, a.IdPrazo);
                linha.TipoDocumento = a.TipoDocumento == "RELATORIO" ? TipoDocumento.Relatorio : TipoDocumento.Boletim;
                (linha.QtdeAmostras, linha.VrUnitario, linha.PorcDesconto, linha.VrDesconto) =
                    (i.QtdeAmostras, a.VrUnitario, a.PorcDesconto, a.PorcDesconto > 0 ? 0 : a.VrDesconto);
            }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ exclusão

    /// <summary>Só a revisão 0 sem revisões posteriores, sem pedido e sem amostras (P05 › excluir); o resto vai em cascata.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var p = await db.LabPropostas.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrada);
        if (p.RevProposta > 0 || await db.LabPropostas.AnyAsync(x => x.AnoProposta == p.AnoProposta && x.CodProposta == p.CodProposta && x.IdLabPropostas != id))
            return Resultado.Erro($"Existe revisão para a Proposta nº {p.NrProposta}: não será permitido excluir.");
        if (await db.PedidosVenda.AnyAsync(x => x.AnoProposta == p.AnoProposta && x.CodProposta == p.CodProposta))
            return Resultado.Erro("Proposta com pedido de venda não pode ser excluída.");
        if (await db.LabHistAmostras.AnyAsync(a => a.IdLabPropostas == id))
            return Resultado.Erro("Proposta com amostra recebida não pode ser excluída.");
        var r = await ErrosMySql.CapturarAsync(() => db.LabPropostas.Where(x => x.IdLabPropostas == id).ExecuteDeleteAsync());
        return r.Sucesso ? Resultado.Ok() : r.Falha;
    }

    // ------------------------------------------------------------------ revisão, contrato, troca de produto (6.1)

    /// <summary>
    /// Gera a revisão (P07) a partir da revisão vigente e devolve o ID da nova. O banco copia cabeçalho (status inicial,
    /// sem envio, autorização ou pedido), textos sem a autorização, produtos e análises com <c>ST_REV_ANTERIOR</c>.
    /// </summary>
    public async Task<Resultado<int>> GerarRevisaoAsync(int id, string? motivo)
    {
        var p = await db.LabPropostas.AsNoTracking().Where(x => x.IdLabPropostas == id).Select(x => new { x.UltimaRevisao, x.NrProposta }).SingleOrDefaultAsync();
        if (p is null) return Resultado.Erro(MensagemNaoEncontrada);
        if (!p.UltimaRevisao) return Resultado.Erro($"Só é possível revisar a última revisão da proposta ({p.NrProposta} já foi substituída).");
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo da revisão.", "motivo");
        var r = await procedures.GerarRevisaoPropostaAsync(id, m.Length > 500 ? m[..500] : m);
        return r.Sucesso ? Resultado.Ok(r.Valor.IdNovo) : Resultado.Erro(r.Mensagem!);
    }

    /// <summary>Contrato padrão da empresa (T05 › textos), para o botão "Usar contrato padrão".</summary>
    public async Task<string?> ContratoPadraoAsync() =>
        HtmlSimples.Limpar(await db.EmpresasParametros.AsNoTracking().Select(p => p.TextoPadraoContrato).SingleOrDefaultAsync());

    /// <summary>
    /// Troca o produto de um item na revisão vigente e nas amostras recebidas por ela (<c>SP_TROCAR_PRODUTO_PROPOSTA</c>);
    /// o banco recusa amostra com boletim emitido.
    /// </summary>
    public async Task<Resultado> TrocarProdutoAsync(int id, int idProdutoAtual, int? idProdutoNovo)
    {
        var p = await db.LabPropostas.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrada);
        if (!p.UltimaRevisao) return Resultado.Erro("Revisão antiga da proposta é somente leitura.");
        if (idProdutoNovo is not { } novo) return Resultado.Erro("Escolha o produto novo.", "idProdutoNovo");
        if (novo == idProdutoAtual) return Resultado.Erro("O produto novo é o mesmo de agora.", "idProdutoNovo");
        if (!await db.LabPropostasProdutos.AnyAsync(x => x.IdLabPropostas == id && x.IdProdutos == idProdutoAtual))
            return Resultado.Erro("O produto escolhido não está nesta proposta.");
        if (!await db.Produtos.AnyAsync(x => x.IdProdutos == novo && !x.Inativo)) return Resultado.Erro("Escolha um produto ativo da lista.", "idProdutoNovo");
        return await procedures.TrocarProdutoPropostaAsync(id, idProdutoAtual, novo);
    }

    // ------------------------------------------------------------------ PDF (6.1)

    /// <summary>PDF da proposta (com a página "Autorização de Serviço"); revisão substituída sai marcada.</summary>
    public async Task<Resultado<ArquivoPdf>> ImprimirAsync(int id)
    {
        var d = await DocumentoAsync(id);
        if (d is null) return Resultado.Erro(MensagemNaoEncontrada);
        return Resultado.Ok(new ArquivoPdf(Propostas.PropostaPdf.Gerar(d), $"Proposta {d.NrProposta.Replace('/', '-')}.pdf"));
    }

    public async Task<Propostas.PropostaDocumento?> DocumentoAsync(int id)
    {
        var v = await db.VwLabPropostas.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        if (v is null) return null;
        var t = await db.LabPropostasTextos.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabPropostas == id);
        var analises = await db.VwLabPropostasAnalises.AsNoTracking().Where(a => a.IdLabPropostas == id)
            .OrderBy(a => a.Item).ThenBy(a => a.IdLabPropostasProdanalises).ToListAsync();
        var emp = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == v.IdEmpresas)
            .Select(e => new { e.RazaoSocial, e.Cnpj, e.Logo, e.CabecalhoLaudo, e.RodapeLaudo, e.IdEntidades }).SingleAsync();
        var endEmp = await db.VwEntidadesEnderecos.AsNoTracking().Where(e => e.IdEntidades == emp.IdEntidades && !e.Inativo)
            .OrderBy(e => e.IdEntidadesEnderecos).Select(e => new[] { e.EnderecoLinha1, e.EnderecoLinha2 }).FirstOrDefaultAsync();
        var enderecos = await db.VwEntidadesEnderecos.AsNoTracking()
            .Where(e => e.IdEntidadesEnderecos == v.IdEntEndLaudo || e.IdEntidadesEnderecos == v.IdEntEndCobranca)
            .ToDictionaryAsync(e => e.IdEntidadesEnderecos, e => new[] { e.EnderecoLinha1, e.EnderecoLinha2 }.OfType<string>().ToList());
        async Task<string?> FoneAsync(int? entidade) => entidade is null ? null : await db.EntidadesFones.AsNoTracking()
            .Where(x => x.IdEntidades == entidade && !x.Inativo).OrderByDescending(x => x.Principal).Select(x => "(" + x.Ddd + ") " + x.Fone).FirstOrDefaultAsync();
        static string? Contato(string? nome, string? email) => nome is null && email is null ? null : string.Join(" — ", new[] { nome, email }.OfType<string>().Distinct());

        return new Propostas.PropostaDocumento
        {
            EmpresaNome = emp.RazaoSocial, EmpresaCnpj = emp.Cnpj is { Length: > 0 } c ? Documentos.FormatarCnpj(c) : null,
            EmpresaEndereco = endEmp?.OfType<string>().ToList() ?? [], EmpresaFone = await FoneAsync(emp.IdEntidades),
            Logo = ImagemValida(emp.Logo), Cabecalho = Textos.Limpo(emp.CabecalhoLaudo), Rodape = Textos.Limpo(emp.RodapeLaudo),
            NrProposta = v.NrProposta, RevisaoSubstituida = !v.UltimaRevisao, DtSolicitacao = v.DtSolicitacao, DtValidade = v.DtValidade,
            Comercial = v.ComercialNome, ClienteNome = v.ClienteNome, ClienteDocumento = v.ClienteCpfCnpj,
            Contato = v.NomeContato, Departamento = v.ContatoDepartamento, ContatoEmail = v.ContatoEmail, ClienteFone = await FoneAsync(v.IdEntidades),
            EnderecoLaudo = enderecos.GetValueOrDefault(v.IdEntEndLaudo) ?? [], ContatoResultado = Contato(v.ContatoResultado, v.EmailResultado),
            CobrancaNome = v.CobrancaNome, CobrancaDocumento = v.CobrancaCpfCnpj,
            EnderecoCobranca = v.IdEntEndCobranca is { } ec ? enderecos.GetValueOrDefault(ec) ?? [] : [], ContatoNf = Contato(v.ContatoNf, v.EmailNf),
            Produtos = analises.GroupBy(a => (a.Item, a.ProdutoDescricao, a.QtdeProduto))
                .Select(g => new Propostas.ProdutoProposta(g.Key.Item, g.Key.ProdutoDescricao, g.Key.QtdeProduto,
                    g.Select(a => new Propostas.LinhaAnaliseProposta(a.Parametro, a.TipoAnalise, a.Metodo, a.Prazo, a.Idioma,
                        a.TipoDocumento == TipoDocumento.Relatorio ? "Relatório" : "Boletim", a.QtdeAmostras, a.VrUnitario, a.VrDesconto, a.VrTotal)).ToList()))
                .ToList(),
            TotalAnalises = v.VrSomaVrTotAnalis, DescontoGeral = v.VrDesconto, ValorTotal = v.VrTotalProposta,
            CondicaoPagamento = v.CondicaoPagamento, Moeda = v.Moeda,
            ItensSolicitados = HtmlSimples.Limpar(t?.TxItensSolicitados), ItensAdquiridos = HtmlSimples.Limpar(t?.TxItensAdquiridos),
            Observacao = HtmlSimples.Limpar(t?.TxObservacao), Contrato = HtmlSimples.Limpar(t?.TxContrato),
        };
    }

    // ------------------------------------------------------------------ apoio

    // logo que o QuestPDF não lê não pode derrubar a impressão: sai sem ele
    private static byte[]? ImagemValida(byte[]? imagem)
    {
        if (imagem is null) return null;
        try { QuestPDF.Infrastructure.Image.FromBinaryData(imagem).Dispose(); return imagem; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return null; }
    }

    private static bool LerPercentual(string? texto, out decimal valor)
    {
        valor = 0;
        if (Textos.Limpo(texto) is not { } t) return true;
        return Textos.Decimal(t, out valor) && valor <= 100 && decimal.Round(valor, 4) == valor;
    }

    private static bool LerValor(string? texto, out decimal valor)
    {
        valor = 0;
        if (Textos.Limpo(texto) is not { } t) return true;
        return Textos.Decimal(t, out valor) && decimal.Round(valor, 2) == valor;
    }

    private static string? Formatar(decimal v, string formato) => v == 0 ? null : v.ToString(formato, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
}
