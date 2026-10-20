using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabControl.ViewModels;

namespace LabControl.Controllers;

public class HomeController : Controller
{
    // Painel com os contadores da T01 entra na tarefa 1.2
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
