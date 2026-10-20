using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T02 — Usuários (docs/telas/T02-usuarios.md). Regras de acesso entre empresas no <see cref="UsuarioService"/>.</summary>
[Permissao("USUARIOS.CONSULTAR")]
public sealed class UsuariosController(UsuarioService usuarios) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(UsuarioFiltroVM filtro) =>
        View(new UsuarioListaVM { Filtro = filtro, Itens = await usuarios.ListarAsync(filtro) });

    [HttpGet, Permissao("USUARIOS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await usuarios.NovoAsync());

    // a resposta pode conter a senha provisória: nunca em cache
    [HttpPost, Permissao("USUARIOS.INCLUIR"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Criar(UsuarioVM vm)
    {
        if (ModelState.IsValid)
        {
            var r = await usuarios.IncluirAsync(vm);
            // senha provisória exibida uma vez, na própria resposta (sem redirect/TempData)
            if (this.AplicarNoFormulario(r)) return View("SenhaProvisoria", r.Valor);
        }
        await usuarios.PrepararFormularioAsync(vm);
        return View("Form", vm);
    }

    [HttpGet, Permissao("USUARIOS.ALTERAR")]
    public async Task<IActionResult> Editar(int id) =>
        await usuarios.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("USUARIOS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, UsuarioVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await usuarios.AlterarAsync(vm), "Usuário gravado."))
            return RedirectToAction(nameof(Index));
        await usuarios.PrepararFormularioAsync(vm);
        var atual = await usuarios.CarregarAsync(id);
        if (atual is null) return NotFound();
        (vm.TrocarSenha, vm.BloqueadoAte, vm.UltimoAcesso) = (atual.TrocarSenha, atual.BloqueadoAte, atual.UltimoAcesso);
        return View("Form", vm);
    }

    [HttpPost, Permissao("USUARIOS.ALTERAR"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResetarSenha(int id)
    {
        var r = await usuarios.ResetarSenhaAsync(id);
        return this.AplicarNoTempData(r) ? View("SenhaProvisoria", r.Valor) : RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("USUARIOS.ALTERAR")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        this.AplicarNoTempData(await usuarios.DesbloquearAsync(id), "Usuário desbloqueado.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("USUARIOS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (this.AplicarNoTempData(await usuarios.ExcluirAsync(id), "Usuário excluído.")) return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Editar), new { id });
    }
}
