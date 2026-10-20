using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T03 — Empresas Usuárias do Sistema (os laboratórios que usam o LabControl; tabela EMPRESAS). Lista, inclusão e
/// alteração de qualquer empresa só para ADMIN_GLOBAL (RN-02.11); os demais vão direto aos dados da empresa ativa
/// (<see cref="EmpresaController.Dados"/>, onde também ficam os contatos). Não existe exclusão.
/// </summary>
public sealed class EmpresasController(EmpresaService empresas, IEmpresaContexto ctx) : Controller
{
    [HttpGet, Permissao("EMPRESAS.CONSULTAR")]
    public async Task<IActionResult> Index() =>
        ctx.AdminGlobal ? View(await empresas.ListarAsync()) : RedirectToAction(nameof(EmpresaController.Dados), "Empresa");

    [HttpGet, SomenteAdminGlobal]
    public async Task<IActionResult> Criar() => View("Form", await empresas.NovaAsync());

    [HttpPost, SomenteAdminGlobal]
    public async Task<IActionResult> Criar(EmpresaVM vm)
    {
        if (ModelState.IsValid)
        {
            var r = await empresas.CriarAsync(vm);
            if (this.AplicarNoFormulario(r, $"Empresa usuária {vm.CodEmpresa.ToUpperInvariant()} criada. Ela já aparece na sua escolha de empresa (menu do usuário → Trocar empresa)."))
                return RedirectToAction(nameof(Index));
        }
        await empresas.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>A empresa ativa abre na tela completa (com contatos); as outras, só com os dados de identificação.</summary>
    [HttpGet, SomenteAdminGlobal]
    public async Task<IActionResult> Editar(int id) =>
        id == ctx.EmpresaId ? RedirectToAction(nameof(EmpresaController.Dados), "Empresa")
        : await empresas.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, SomenteAdminGlobal]
    public async Task<IActionResult> Editar(int id, EmpresaVM vm)
    {
        vm.Id = id;
        ModelState.Remove(nameof(vm.CodEmpresa));
        if (ModelState.IsValid && this.AplicarNoFormulario(await empresas.AlterarAsync(vm), "Empresa usuária gravada."))
            return RedirectToAction(nameof(Index));
        await empresas.PrepararAsync(vm);
        vm.CodEmpresa = (await empresas.CarregarAsync(id))?.CodEmpresa ?? "";
        return View("Form", vm);
    }
}
