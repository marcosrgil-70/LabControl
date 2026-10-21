using LabControl.Infra;
using LabControl.Services.Auxiliares;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T10-11 — parcelas da condição de pagamento (<c>/Auxiliares/CONDICOES_PAGTOS/Parcelas/{id}</c>). Código e
/// descrição são do CRUD genérico (<see cref="AuxiliaresController"/>); esta tela só edita a grade.
/// </summary>
[Route("Auxiliares/CONDICOES_PAGTOS/Parcelas")]
public sealed class CondicoesPagamentoController(CondicaoPagamentoService condicoes) : Controller
{
    [HttpGet("{id:int}"), Permissao("AUXILIARES.CONSULTAR")]
    public async Task<IActionResult> Parcelas(int id) =>
        await condicoes.CarregarAsync(id) is { } vm ? View(vm) : NotFound();

    [HttpPost("{id:int}"), Permissao("AUXILIARES.ALTERAR")]
    public async Task<IActionResult> Parcelas(int id, ParcelasCondicaoVM vm)
    {
        vm.Id = id;
        var r = await condicoes.GravarAsync(vm);
        if (r.Sucesso)
        {
            TempData[Mensagens.Sucesso] = "Parcelas gravadas.";
            return RedirectToAction(nameof(Parcelas), new { id });
        }
        if (!await condicoes.PrepararAsync(vm)) return NotFound();
        vm.Erro = r.Mensagem;
        return View(vm);
    }
}
