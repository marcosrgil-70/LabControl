using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T23 — Movimentação de amostras (<c>/Movimentacao</c>, tarefa 7.1): busca por cliente/proposta/número, painel da amostra
/// com saldo e histórico, entrada complementar/saída com os parâmetros atendidos e exclusão dos movimentos manuais.
/// </summary>
public sealed class MovimentacaoController(MovimentacaoService movimentacao) : Controller
{
    [HttpGet, Permissao("MOVIMENTACAO.CONSULTAR")]
    public async Task<IActionResult> Index(MovimentacaoFiltroVM filtro) => View(await movimentacao.ListarAsync(filtro));

    [HttpGet, Permissao("MOVIMENTACAO.CONSULTAR")]
    public async Task<IActionResult> Amostra(int id) =>
        await movimentacao.CarregarAsync(id) is { } vm ? View(vm) : NotFound();

    /// <summary>Recusa reabre o modal com o que foi digitado e a mensagem.</summary>
    [HttpPost, Permissao("MOVIMENTACAO.INCLUIR")]
    public async Task<IActionResult> Incluir(MovimentoNovoVM vm)
    {
        var r = await movimentacao.IncluirAsync(vm);
        if (this.AplicarNoFormulario(r, vm.Tipo == Models.EntradaSaida.Saida ? "Saída registrada." : "Entrada registrada."))
            return RedirectToAction(nameof(Amostra), new { id = vm.IdAmostra });
        return await movimentacao.CarregarAsync(vm.IdAmostra, vm) is { } tela ? View(nameof(Amostra), tela) : NotFound();
    }

    [HttpPost, Permissao("MOVIMENTACAO.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id, int idAmostra)
    {
        this.AplicarNoTempData(await movimentacao.ExcluirAsync(id), "Movimentação excluída.");
        return RedirectToAction(nameof(Amostra), new { id = idAmostra });
    }
}
