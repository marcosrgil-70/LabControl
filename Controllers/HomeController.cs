using System.Diagnostics;
using LabControl.Infra;
using LabControl.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabControl.ViewModels;

namespace LabControl.Controllers;

public class HomeController(PainelService painel) : Controller
{
    /// <summary>Painel da T01: contadores da empresa ativa conforme as permissões do usuário.</summary>
    [HttpGet]
    public async Task<IActionResult> Index() =>
        View(new PainelVM { EmpresaNome = User.FindFirst(LabClaims.EmpresaNome)?.Value ?? "", Contadores = await painel.ContadoresAsync() });

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
