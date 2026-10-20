using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T03 — lista de empresas e nova empresa (só ADMIN_GLOBAL, RN-02.11). Não existe exclusão.</summary>
[SomenteAdminGlobal]
public sealed class EmpresasController(EmpresaService empresas) : Controller
{
    public async Task<IActionResult> Index() => View(await empresas.ListarAsync());

    [HttpGet]
    public async Task<IActionResult> Criar() => View("Form", await empresas.NovaAsync());

    [HttpPost]
    public async Task<IActionResult> Criar(EmpresaVM vm)
    {
        if (ModelState.IsValid)
        {
            var r = await empresas.CriarAsync(vm);
            if (this.AplicarNoFormulario(r, $"Empresa {vm.CodEmpresa.ToUpperInvariant()} criada. Ela já aparece na sua escolha de empresa (menu do usuário → Trocar empresa)."))
                return RedirectToAction(nameof(Index));
        }
        await empresas.PrepararAsync(vm);
        return View("Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id) => await empresas.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Editar(int id, EmpresaVM vm)
    {
        vm.Id = id;
        ModelState.Remove(nameof(vm.CodEmpresa));
        if (ModelState.IsValid && this.AplicarNoFormulario(await empresas.AlterarAsync(vm), "Empresa gravada."))
            return RedirectToAction(nameof(Index));
        await empresas.PrepararAsync(vm);
        vm.CodEmpresa = (await empresas.CarregarAsync(id))?.CodEmpresa ?? "";
        return View("Form", vm);
    }
}
