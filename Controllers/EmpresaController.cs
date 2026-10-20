using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T03 — dados da empresa ativa (<c>/Empresa/Dados</c>), logo e combo de cidades. A lista e a inclusão ficam em <see cref="EmpresasController"/>.</summary>
public sealed class EmpresaController(EmpresaService empresas, ContaService conta, IEmpresaContexto ctx) : Controller
{
    private int Ativa => ctx.EmpresaId ?? throw new InvalidOperationException("Sem empresa ativa.");

    [HttpGet, Permissao("EMPRESAS.CONSULTAR")]
    public async Task<IActionResult> Dados() =>
        await empresas.CarregarAsync(Ativa) is { } vm ? View(vm) : NotFound();

    [HttpPost, Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> Dados(EmpresaVM vm)
    {
        vm.Id = Ativa;
        ModelState.Remove(nameof(vm.CodEmpresa)); // o código não muda (não vem no formulário)
        if (ModelState.IsValid && this.AplicarNoFormulario(await empresas.AlterarAsync(vm), "Dados da empresa gravados."))
        {
            await AtualizarTopoAsync();
            return RedirectToAction(nameof(Dados));
        }
        await empresas.PrepararAsync(vm);
        vm.CodEmpresa = (await empresas.CarregarAsync(Ativa))?.CodEmpresa ?? "";
        return View(vm);
    }

    /// <summary>Logo (tela e PDFs): da empresa ativa ou, para ADMIN_GLOBAL, de qualquer uma.</summary>
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Logo(int? id) =>
        await empresas.LogoAsync(id ?? Ativa) is { } logo ? File(logo.Dados, logo.Mime) : NotFound();

    /// <summary>Combo de cidades da UF (JSON), usado pelo formulário de endereço.</summary>
    [HttpGet]
    public async Task<IActionResult> Cidades(int idEstado) => Json(await empresas.CidadesAsync(idEstado));

    // nome/código da empresa no topo vêm das claims: regrava a identidade depois de alterar
    private async Task AtualizarTopoAsync()
    {
        if (ctx.UsuarioId is not { } usuario) return;
        var r = await conta.MontarIdentidadeAsync(usuario, Ativa);
        if (r.Sucesso) await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, r.Valor);
    }
}
