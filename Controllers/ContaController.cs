using System.Security.Claims;
using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T00 — login, troca de senha, escolha/troca de empresa e saída (docs/telas/T00-login-empresa.md).</summary>
public sealed class ContaController(ContaService conta, IEmpresaContexto contexto) : Controller
{
    private int UsuarioId => contexto.UsuarioId ?? throw new InvalidOperationException("Usuário não autenticado.");

    [AllowAnonymous, HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginVM());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var r = await conta.EntrarAsync(vm.Login, vm.Senha);
        if (!this.AplicarNoFormulario(r)) return View(new LoginVM { Login = vm.Login });

        var identidade = await conta.MontarIdentidadeAsync(r.Valor);
        if (identidade.Valor.HasClaim(LabClaims.TrocarSenha, "1"))
        {
            await EntrarAsync(identidade.Valor);
            return RedirectToAction(nameof(TrocarSenha));
        }
        return await EscolherOuEntrarAsync(r.Valor, identidade.Valor);
    }

    [HttpGet]
    public IActionResult TrocarSenha() =>
        View(new TrocarSenhaVM { Obrigatoria = User.HasClaim(LabClaims.TrocarSenha, "1") });

    [HttpPost]
    public async Task<IActionResult> TrocarSenha(TrocarSenhaVM vm)
    {
        vm.Obrigatoria = User.HasClaim(LabClaims.TrocarSenha, "1");
        if (!ModelState.IsValid) return View(vm);
        var r = await conta.TrocarSenhaAsync(UsuarioId, vm.SenhaAtual, vm.NovaSenha, vm.Confirmacao);
        if (!this.AplicarNoFormulario(r, "Senha alterada.")) return View(vm);

        if (!vm.Obrigatoria) return RedirectToAction("Index", "Home");
        // 1º acesso: segue para a escolha de empresa com a identidade já sem a troca pendente
        return await EscolherOuEntrarAsync(UsuarioId, (await conta.MontarIdentidadeAsync(UsuarioId)).Valor);
    }

    /// <summary>Escolha (após o login) ou troca de empresa (menu do topo) — sem pedir a senha de novo.</summary>
    [HttpGet]
    public async Task<IActionResult> Empresa()
    {
        var disp = await conta.EmpresasDoUsuarioAsync(UsuarioId);
        return View(new EscolherEmpresaVM { Empresas = disp.Empresas, IdEmpresa = contexto.EmpresaId ?? disp.PreSelecionada, EmpresaAtual = contexto.EmpresaId });
    }

    [HttpPost]
    public async Task<IActionResult> Empresa(EscolherEmpresaVM vm)
    {
        var r = vm.IdEmpresa is { } id ? await conta.MontarIdentidadeAsync(UsuarioId, id) : Resultado.Erro("Selecione a empresa.");
        if (!this.AplicarNoFormulario(r))
        {
            vm.Empresas = (await conta.EmpresasDoUsuarioAsync(UsuarioId)).Empresas;
            vm.EmpresaAtual = contexto.EmpresaId;
            return View(vm);
        }
        await EntrarAsync(r.Valor);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>Destino do <c>Forbid</c> do cookie; o <see cref="PermissaoAttribute"/> devolve o 403 direto.</summary>
    [HttpGet]
    public IActionResult AcessoNegado() => PermissaoAttribute.AcessoNegado(HttpContext, ModelState, null);

    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Uma empresa só: entra direto; nenhuma: recusa; várias: tela de escolha.</summary>
    private async Task<IActionResult> EscolherOuEntrarAsync(int idUsuario, ClaimsPrincipal semEmpresa)
    {
        var disp = await conta.EmpresasDoUsuarioAsync(idUsuario);
        if (disp.Empresas.Count == 0)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ModelState.AddModelError(string.Empty, "Seu usuário não tem acesso a nenhuma empresa ativa. Procure o administrador.");
            return View(nameof(Login), new LoginVM());
        }
        if (disp.Empresas.Count == 1)
        {
            var r = await conta.MontarIdentidadeAsync(idUsuario, disp.Empresas[0].IdEmpresas);
            await EntrarAsync(r.Valor);
            return RedirectToAction("Index", "Home");
        }
        await EntrarAsync(semEmpresa);
        return RedirectToAction(nameof(Empresa));
    }

    private Task EntrarAsync(ClaimsPrincipal principal) =>
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
}
