using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T21 — Pedidos de venda (<c>/PedidosVenda</c>, tarefa 6.2): consulta, geração a partir da proposta, tela do pedido
/// (contratado × recebido, amostras, observação), atualizar para a revisão vigente, concluir/reabrir, cancelar e PDF.
/// </summary>
public sealed class PedidosVendaController(PedidoVendaService pedidos) : Controller
{
    [HttpGet, Permissao("PEDIDOS_VENDA.CONSULTAR")]
    public async Task<IActionResult> Index(PedidoVendaFiltroVM filtro) => View(await pedidos.ListarAsync(filtro));

    [HttpGet, Permissao("PEDIDOS_VENDA.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await pedidos.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    /// <summary>Conteúdo do modal "Gerar pedido" para a proposta (partial); recusa → 422 com a mensagem.</summary>
    [HttpGet, Permissao("PEDIDOS_VENDA.GERAR")]
    public async Task<IActionResult> Gerar(int idProposta)
    {
        var r = await pedidos.PrepararGeracaoAsync(idProposta);
        return r.Sucesso ? PartialView("_Gerar", r.Valor) : UnprocessableEntity(r.Mensagem);
    }

    [HttpPost, Permissao("PEDIDOS_VENDA.GERAR")]
    public async Task<IActionResult> Gerar(PedidoVendaGerarVM vm)
    {
        var r = await pedidos.GerarAsync(vm);
        if (!this.AplicarNoTempData(r)) return RedirectToAction("Editar", "Propostas", new { id = vm.IdProposta });
        TempData[Mensagens.Sucesso] = $"Pedido de venda {PedidoVendaService.Numero(r.Valor.NrPedido)} gerado.";
        return RedirectToAction(nameof(Editar), new { id = r.Valor.IdPedido });
    }

    [HttpPost, Permissao("PEDIDOS_VENDA.ALTERAR")]
    public async Task<IActionResult> Observacao(int id, string? observacao)
    {
        this.AplicarNoTempData(await pedidos.GravarObservacaoAsync(id, observacao), "Observação gravada.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>Atualiza o pedido para a revisão vigente da proposta (valores, cobrança, condição e itens).</summary>
    [HttpPost, Permissao("PEDIDOS_VENDA.ALTERAR")]
    public async Task<IActionResult> AtualizarRevisao(int id)
    {
        this.AplicarNoTempData(await pedidos.AtualizarRevisaoAsync(id), "Pedido atualizado para a revisão vigente da proposta.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("PEDIDOS_VENDA.ALTERAR")]
    public async Task<IActionResult> Concluir(int id)
    {
        this.AplicarNoTempData(await pedidos.ConcluirAsync(id), "Pedido concluído.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("PEDIDOS_VENDA.ALTERAR")]
    public async Task<IActionResult> Reabrir(int id, string? motivo)
    {
        this.AplicarNoTempData(await pedidos.ReabrirAsync(id, motivo), "Pedido reaberto: voltou para em execução.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("PEDIDOS_VENDA.CANCELAR")]
    public async Task<IActionResult> Cancelar(int id, string? motivo)
    {
        this.AplicarNoTempData(await pedidos.CancelarAsync(id, motivo), "Pedido cancelado.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>PDF do pedido (<c>frx_PedVenda</c>), aberto em nova aba.</summary>
    [HttpGet, Permissao("PEDIDOS_VENDA.IMPRIMIR")]
    public async Task<IActionResult> Imprimir(int id)
    {
        var r = await pedidos.ImprimirAsync(id);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Index));
        Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = r.Valor.Nome }.ToString();
        return File(r.Valor.Conteudo, "application/pdf");
    }
}
