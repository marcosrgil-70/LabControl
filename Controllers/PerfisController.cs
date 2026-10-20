using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T04 — Perfis e permissões (docs/telas/T04-perfis-permissoes.md).</summary>
[Permissao("PERFIS.CONSULTAR")]
public sealed class PerfisController(PerfilService perfis) : Controller
{
    public async Task<IActionResult> Index() => View(await perfis.ListarAsync());

    [HttpGet, Permissao("PERFIS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await perfis.NovoAsync());

    [HttpPost, Permissao("PERFIS.INCLUIR")]
    public async Task<IActionResult> Criar(PerfilVM vm)
    {
        if (ModelState.IsValid && this.AplicarNoFormulario(await perfis.IncluirAsync(vm), "Perfil incluído."))
            return RedirectToAction(nameof(Index));
        vm.Matriz = await perfis.MatrizAsync();
        return View("Form", vm);
    }

    [HttpGet, Permissao("PERFIS.ALTERAR")]
    public async Task<IActionResult> Editar(int id) => await perfis.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("PERFIS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, PerfilVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await perfis.AlterarAsync(vm),
                "Perfil gravado. As permissões valem a partir da próxima entrada ou troca de empresa de cada usuário."))
            return RedirectToAction(nameof(Index));
        vm.Matriz = await perfis.MatrizAsync();
        vm.Administrador = (await perfis.CarregarAsync(id))?.Administrador ?? false;
        return View("Form", vm);
    }

    [HttpPost, Permissao("PERFIS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        this.AplicarNoTempData(await perfis.ExcluirAsync(id), "Perfil excluído.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Permissao("PERFIS.INCLUIR")]
    public async Task<IActionResult> Copiar(int? id) =>
        View(new CopiarPerfilVM { IdOrigem = id, Origens = await perfis.OrigensAsync() });

    [HttpPost, Permissao("PERFIS.INCLUIR")]
    public async Task<IActionResult> Copiar(CopiarPerfilVM vm)
    {
        if (ModelState.IsValid && this.AplicarNoFormulario(await perfis.CopiarAsync(vm.IdOrigem!.Value, vm.Nome), "Perfil copiado."))
            return RedirectToAction(nameof(Index));
        vm.Origens = await perfis.OrigensAsync();
        return View(vm);
    }
}
