using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services.Propostas;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T21 — Pedidos de venda (6.2, P08): consulta, geração a partir da proposta (<c>SP_GERAR_PEDIDO_VENDA</c>), tela do
/// pedido (contratado × recebido, amostras), observação, atualizar para a revisão vigente, concluir/reabrir, cancelar
/// e PDF. A conclusão automática na emissão do boletim é do banco (<c>PR_ATUALIZAR_SITUACAO_PEDIDO</c>, pendência 24).
/// </summary>
public sealed partial class PedidoVendaService(LabDbContext db, Procedures procedures, PropostaService propostas, IAuditoria auditoria)
{
    public const string MensagemNaoEncontrado = "O pedido de venda não foi encontrado.";
    public const string MensagemSemAutorizacao = "É obrigatório o preenchimento da Data Autorização para gerar o Pedido de Venda.";

    public static string Numero(int nr) => nr.ToString("D9");

    // ------------------------------------------------------------------ consulta

    public async Task<PedidoVendaListaVM> ListarAsync(PedidoVendaFiltroVM f)
    {
        var q = db.PedidosVenda.AsNoTracking();
        if (f.NrPedido is { } nr) q = q.Where(p => p.NrPedido == nr);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(p => p.Entidade.Nome.Contains(c) || (p.Entidade.NomeFantasia != null && p.Entidade.NomeFantasia.Contains(c))
                             || (p.Entidade.CpfCnpj != null && p.Entidade.CpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Proposta) is { } n && RegexProposta().Match(n) is { Success: true } m)
        {
            var cod = int.Parse(m.Groups["cod"].Value);
            q = q.Where(p => p.CodProposta == cod);
            if (m.Groups["ano"].Success && short.TryParse(m.Groups["ano"].Value, out var a))
            {
                var ano = (short)(a < 100 ? 2000 + a : a);
                q = q.Where(p => p.AnoProposta == ano);
            }
        }
        if (f.Situacao is { } s) q = q.Where(p => p.Situacao == s);
        if (f.De is { } de) q = q.Where(p => p.DtPedido >= de);
        if (f.Ate is { } ate) q = q.Where(p => p.DtPedido <= ate);

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, PedidoVendaFiltroVM.TamanhoPagina);
        var linhas = await q.OrderByDescending(p => p.NrPedido)
            .Skip((f.Pagina - 1) * PedidoVendaFiltroVM.TamanhoPagina).Take(PedidoVendaFiltroVM.TamanhoPagina)
            .Select(p => new
            {
                p.IdPedidosVenda, p.NrPedido, p.DtPedido, p.IdLabPropostas, p.LabProposta.NrProposta, p.Entidade.CpfCnpj, p.Entidade.Nome,
                p.VrTotal, p.Situacao,
                Contratadas = db.PedidosVendaItens.Where(i => i.IdPedidosVenda == p.IdPedidosVenda).Sum(i => (int?)i.QtdeContratada) ?? 0,
                // recebidas = amostras revisão 0 de qualquer revisão da proposta (VW_PEDIDOS_VENDA_SALDO)
                Recebidas = db.LabHistAmostras.Count(a => a.Revisao == 0 && a.LabProposta.AnoProposta == p.AnoProposta && a.LabProposta.CodProposta == p.CodProposta),
            })
            .ToListAsync();
        var itens = linhas.Select(p => new PedidoVendaListaItemVM(p.IdPedidosVenda, Numero(p.NrPedido), p.DtPedido, p.IdLabPropostas, p.NrProposta,
            p.CpfCnpj, p.Nome, p.VrTotal, p.Situacao, p.Recebidas, p.Contratadas)).ToList();
        return new PedidoVendaListaVM { Filtro = f, Itens = itens, Total = total };
    }

    [GeneratedRegex(@"^0*(?<cod>\d{1,9})(?:\s*/\s*(?<ano>\d{2}|\d{4}))?(?:\s*-\s*R\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex RegexProposta();

    // ------------------------------------------------------------------ tela do pedido

    public async Task<PedidoVendaVM?> CarregarAsync(int id)
    {
        var p = await db.PedidosVenda.AsNoTracking().Where(x => x.IdPedidosVenda == id)
            .Select(x => new
            {
                Pedido = x, x.LabProposta.NrProposta, Cliente = x.Entidade.Nome, x.Entidade.CpfCnpj, Cobranca = x.EntCobranca.Nome,
                Condicao = x.CondicaoPagto.Descricao, Moeda = x.Moeda == null ? null : x.Moeda.Descricao,
            })
            .SingleOrDefaultAsync();
        if (p is null) return null;
        var ped = p.Pedido;
        var vigente = await db.LabPropostas.AsNoTracking()
            .Where(x => x.AnoProposta == ped.AnoProposta && x.CodProposta == ped.CodProposta && x.UltimaRevisao && x.IdLabPropostas != ped.IdLabPropostas)
            .Select(x => new { x.IdLabPropostas, x.NrProposta }).SingleOrDefaultAsync();
        var itens = await db.VwPedidosVendaSaldo.AsNoTracking().Where(i => i.IdPedidosVenda == id).OrderBy(i => i.Item)
            .Select(i => new PedidoVendaItemVM(i.Item ?? 0, i.ProdutoDescricao, i.QtdeContratada, (int)(i.QtdeRecebida ?? 0))).ToListAsync();

        var amostras = await db.LabHistAmostras.AsNoTracking().Where(a => a.IdPedidosVenda == id && a.UltimaRevisao)
            .OrderBy(a => a.DtEntrega).ThenBy(a => a.IdLabHistAmostras)
            .Select(a => new { a.IdLabHistAmostras, a.NrAmostra, a.DtEntrega, Raiz = a.IdAmostraRaiz ?? a.IdLabHistAmostras, a.AmostraStatus.Descricao, a.AmostraStatus.Cor })
            .ToListAsync();
        // revisões das amostras pelos índices (ID e ID_AMOSTRA_RAIZ), não por "raiz ?? id" (sem índice — D-23)
        var raizes = amostras.Select(a => a.Raiz).ToList();
        var familia = (await db.LabHistAmostras.AsNoTracking()
                .Where(a => raizes.Contains(a.IdLabHistAmostras) || (a.IdAmostraRaiz != null && raizes.Contains(a.IdAmostraRaiz.Value)))
                .Select(a => new { a.IdLabHistAmostras, Raiz = a.IdAmostraRaiz ?? a.IdLabHistAmostras }).ToListAsync())
            .ToDictionary(a => a.IdLabHistAmostras, a => a.Raiz);
        var ids = familia.Keys.ToList();
        var boletins = (await db.LabBoletimAnalises.AsNoTracking()
                .Where(b => b.UltimaRevisao && ids.Contains(b.IdLabHistAmostras))
                .Select(b => new { b.IdLabHistAmostras, b.DhEmissao }).ToListAsync())
            .ToLookup(b => familia[b.IdLabHistAmostras]);

        return new PedidoVendaVM
        {
            Id = ped.IdPedidosVenda, NrPedido = Numero(ped.NrPedido), DtPedido = ped.DtPedido, Situacao = ped.Situacao,
            IdProposta = ped.IdLabPropostas, NrProposta = p.NrProposta, IdPropostaVigente = vigente?.IdLabPropostas, NrPropostaVigente = vigente?.NrProposta,
            Cliente = p.Cliente, CpfCnpj = p.CpfCnpj, Cobranca = p.Cobranca, CondicaoPagamento = p.Condicao, Moeda = p.Moeda, VrTotal = ped.VrTotal,
            DtAutorizacao = ped.DtAutorizacao, AutorizadoPor = ped.AutorizadoPor, NrPedidoCliente = ped.NrPedidoCliente,
            DtCancelamento = ped.DtCancelamento, MotivoCancelamento = ped.MotivoCancelamento, Observacao = ped.Observacao,
            Itens = itens,
            Amostras = amostras.Select(a =>
            {
                var b = boletins[a.Raiz].ToList();
                var (texto, emitido) = b.Count == 0 ? ("Sem boletim", false)
                    : b.All(x => x.DhEmissao is not null) ? ($"Emitido em {b.Max(x => x.DhEmissao)!.Value:dd/MM/yyyy}", true)
                    : ("Em montagem", false);
                return new PedidoVendaAmostraVM(a.IdLabHistAmostras, a.NrAmostra, a.DtEntrega, a.Descricao, a.Cor, texto, emitido);
            }).ToList(),
        };
    }

    // ------------------------------------------------------------------ gerar

    /// <summary>Modal "Gerar pedido": só a revisão vigente, sem pedido; sugere hoje e o contato da proposta.</summary>
    public async Task<Resultado<PedidoVendaGerarVM>> PrepararGeracaoAsync(int idProposta)
    {
        var v = await db.VwLabPropostas.AsNoTracking().Where(x => x.IdLabPropostas == idProposta)
            .Select(x => new { x.NrProposta, x.ClienteNome, x.NomeContato, x.UltimaRevisao, x.IdPedidosVenda }).SingleOrDefaultAsync();
        if (v is null) return Resultado.Erro(PropostaService.MensagemNaoEncontrada);
        if (!v.UltimaRevisao) return Resultado.Erro("Gere o pedido a partir da última revisão da proposta.");
        if (v.IdPedidosVenda is not null) return Resultado.Erro("Já existe Pedido de Venda para esta proposta.");
        return Resultado.Ok(new PedidoVendaGerarVM
        {
            IdProposta = idProposta, NrProposta = v.NrProposta, Cliente = v.ClienteNome, DtAutorizacao = await db.HojeAsync(), AutorizadoPor = v.NomeContato,
        });
    }

    /// <summary>Gera o pedido (número, itens e amostras já recebidas pelo banco) e devolve o ID.</summary>
    public async Task<Resultado<int>> GerarAsync(PedidoVendaGerarVM vm)
    {
        if (vm.DtAutorizacao is not { } dt) return Resultado.Erro(MensagemSemAutorizacao, nameof(vm.DtAutorizacao));
        if (dt > await db.HojeAsync()) return Resultado.Erro("Data de Autorização não pode ser futura.", nameof(vm.DtAutorizacao));
        var r = await procedures.GerarPedidoVendaAsync(vm.IdProposta, dt, Corte(vm.AutorizadoPor, 100), Corte(vm.NrPedidoCliente, 40));
        return r.Sucesso ? Resultado.Ok(r.Valor.IdPedido) : Resultado.Erro(r.Mensagem!);
    }

    // ------------------------------------------------------------------ ações do pedido

    public async Task<Resultado> GravarObservacaoAsync(int id, string? observacao)
    {
        var p = await db.PedidosVenda.SingleOrDefaultAsync(x => x.IdPedidosVenda == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (p.Situacao == SituacaoPedido.Cancelado) return Resultado.Erro("Pedido cancelado não pode ser alterado.");
        p.Observacao = Textos.Limpo(observacao);
        return await GravarAsync();
    }

    public Task<Resultado> AtualizarRevisaoAsync(int id) => procedures.AtualizarPedidoRevisaoAsync(id);

    /// <summary>Conclusão manual (P08): aberto ou em execução, mesmo com item faltando.</summary>
    public async Task<Resultado> ConcluirAsync(int id)
    {
        var p = await db.PedidosVenda.SingleOrDefaultAsync(x => x.IdPedidosVenda == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (p.Situacao is not (SituacaoPedido.Aberto or SituacaoPedido.EmExecucao))
            return Resultado.Erro($"Pedido {SituacoesPedido.Nome(p.Situacao).ToLowerInvariant()} não pode ser concluído.");
        // concluído à mão: se uma revisão reabrir o pedido, ele volta a concluir sozinho mesmo com item faltando
        // (decisão do usuário, 28/10/2026 — PR_ATUALIZAR_SITUACAO_PEDIDO)
        (p.Situacao, p.ConclusaoManual) = (SituacaoPedido.Concluido, true);
        return await GravarAsync();
    }

    /// <summary>Reabrir (decisão do usuário, 27/10/2026): concluído → em execução, com o motivo na auditoria.</summary>
    public async Task<Resultado> ReabrirAsync(int id, string? motivo)
    {
        var situacao = await db.PedidosVenda.Where(x => x.IdPedidosVenda == id).Select(x => (SituacaoPedido?)x.Situacao).SingleOrDefaultAsync();
        if (situacao is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (situacao != SituacaoPedido.Concluido) return Resultado.Erro("Só pedido concluído pode ser reaberto.");
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo da reabertura.", "motivo");
        return await ErrosMySql.CapturarAsync(async () =>
        {
            // a auditoria automática não guarda o motivo: UPDATE direto + evento com o motivo, na mesma transação
            await using var tx = await db.Database.BeginTransactionAsync();
            var alterados = await db.PedidosVenda.Where(x => x.IdPedidosVenda == id && x.Situacao == SituacaoPedido.Concluido)
                // reaberto pelo usuário: volta a regra normal (só conclui sozinho com tudo recebido)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Situacao, SituacaoPedido.EmExecucao).SetProperty(x => x.ConclusaoManual, false));
            if (alterados == 0) throw new ErroDeNegocioException("Só pedido concluído pode ser reaberto.");
            await auditoria.RegistrarAsync(AcoesAuditoria.Alteracao, "PEDIDOS_VENDA", id.ToString(),
                new { SITUACAO = "CONCLUIDO" }, new { SITUACAO = "EM_EXECUCAO", MOTIVO = Corte(m, 500) });
            await tx.CommitAsync();
            db.ChangeTracker.Clear(); // o UPDATE direto não passa pelo rastreador: nada antigo fica em memória
        });
    }

    /// <summary>Cancelar (P08): motivo obrigatório; recusado se houver amostra ligada ao pedido.</summary>
    public async Task<Resultado> CancelarAsync(int id, string? motivo)
    {
        var p = await db.PedidosVenda.SingleOrDefaultAsync(x => x.IdPedidosVenda == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (p.Situacao == SituacaoPedido.Cancelado) return Resultado.Erro("O pedido já está cancelado.");
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo do cancelamento.", "motivo");
        if (await db.LabHistAmostras.AnyAsync(a => a.IdPedidosVenda == id))
            return Resultado.Erro("Pedido com amostra recebida não pode ser cancelado.");
        (p.Situacao, p.DtCancelamento, p.MotivoCancelamento) = (SituacaoPedido.Cancelado, await db.AgoraAsync(), Corte(m, 500));
        return await GravarAsync();
    }

    // ------------------------------------------------------------------ PDF

    /// <summary>PDF do pedido (<c>frx_PedVenda</c>): a revisão da proposta do pedido com o nº e a autorização.</summary>
    public async Task<Resultado<ArquivoPdf>> ImprimirAsync(int id)
    {
        var p = await db.PedidosVenda.AsNoTracking().SingleOrDefaultAsync(x => x.IdPedidosVenda == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrado);
        var nr = Numero(p.NrPedido);
        var d = await propostas.DocumentoAsync(p.IdLabPropostas, new PedidoImpresso(nr, p.DtPedido, p.DtAutorizacao, p.AutorizadoPor,
            p.NrPedidoCliente, p.Situacao == SituacaoPedido.Cancelado));
        if (d is null) return Resultado.Erro(PropostaService.MensagemNaoEncontrada);
        return Resultado.Ok(new ArquivoPdf(PropostaPdf.GerarPedido(d), $"Pedido de venda {nr}.pdf"));
    }

    // ------------------------------------------------------------------ apoio

    private async Task<Resultado> GravarAsync()
    {
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r.Sucesso ? Resultado.Ok() : r.Falha;
    }

    private static string? Corte(string? texto, int max) => Textos.Limpo(texto) is { } t ? (t.Length > max ? t[..max] : t) : null;
}
