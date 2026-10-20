using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T03 — dados da empresa ativa (<c>/Empresa/Dados</c>), logo e combo de cidades. A lista e a inclusão ficam em <see cref="EmpresasController"/>.</summary>
public sealed class EmpresaController(EmpresaService empresas, ParametrosEmpresaService parametros, ContaService conta, IEmpresaContexto ctx) : Controller
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

    /// <summary>T05 — parâmetros da empresa ativa (aba "Parâmetros") e numeração somente leitura (aba "Numeração").</summary>
    [HttpGet, Permissao("PARAMETROS_EMPRESA.CONSULTAR")]
    public async Task<IActionResult> Parametros() =>
        await parametros.CarregarAsync() is { } vm ? View(vm) : NotFound();

    [HttpPost, Permissao("PARAMETROS_EMPRESA.ALTERAR")]
    public async Task<IActionResult> Parametros(ParametrosEmpresaVM vm)
    {
        if (ModelState.IsValid && this.AplicarNoFormulario(await parametros.AlterarAsync(vm), "Parâmetros da empresa gravados."))
            return RedirectToAction(nameof(Parametros));
        await parametros.PrepararAsync(vm);
        return View(vm);
    }

    /// <summary>T05 — ajuste manual de uma linha da numeração, com motivo (só ADMIN_GLOBAL).</summary>
    [HttpPost, SomenteAdminGlobal]
    public async Task<IActionResult> AjustarNumeracao([Bind(Prefix = nameof(ParametrosEmpresaVM.Ajuste))] AjusteNumeracaoVM ajuste)
    {
        Resultado r = ModelState.IsValid
            ? await parametros.AjustarNumeracaoAsync(ajuste)
            : Resultado.Erro(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage);
        this.AplicarNoTempData(r, $"Numeração ajustada: {ParametrosEmpresaService.Documentos.GetValueOrDefault(ajuste.Tipo, ajuste.Tipo)} → {ajuste.NovoValor}.");
        return Redirect(Url.Action(nameof(Parametros)) + "#numeracao");
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
