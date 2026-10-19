using LabControl.Data;
using LabControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LabControl.Controllers;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _db;
    public LoginController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("UsuarioNome") != null)
            return RedirectToAction("Index", "Home");

        await CarregarEmpresasViewBag();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string login, string senha, int? idEmpresa)
    {
        var senhaHash = HashSha256(senha);

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Codigo == login.ToUpper() && u.SenhaHash == senhaHash && !u.Inativo);

        if (usuario == null)
        {
            ViewBag.Erro = "Login ou senha incorretos.";
            await CarregarEmpresasViewBag();
            return View();
        }

        var empresas = await _db.Empresas
            .Include(e => e.Entidade)
            .OrderBy(e => e.Codigo)
            .ToListAsync();

        Empresa? empresa = null;

        if (empresas.Count > 1)
        {
            if (!idEmpresa.HasValue || idEmpresa.Value == 0)
            {
                ViewBag.Erro = "Selecione uma empresa para continuar.";
                await CarregarEmpresasViewBag();
                return View();
            }
            empresa = empresas.FirstOrDefault(e => e.Id == idEmpresa.Value);
            if (empresa == null)
            {
                ViewBag.Erro = "Empresa inválida.";
                await CarregarEmpresasViewBag();
                return View();
            }
        }
        else if (empresas.Count == 1)
        {
            empresa = empresas[0];
        }

        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        HttpContext.Session.SetString("UsuarioNome", usuario.Nome);
        HttpContext.Session.SetString("UsuarioCodigo", usuario.Codigo);
        HttpContext.Session.SetString("UsuarioAdmin", usuario.IsAdmin ? "1" : "0");

        if (empresa != null)
        {
            HttpContext.Session.SetInt32("EmpresaId", empresa.Id);
            HttpContext.Session.SetString("EmpresaNome", empresa.Entidade?.Nome ?? empresa.Codigo);
        }

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Sair()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Index));
    }

    private async Task CarregarEmpresasViewBag()
    {
        var empresas = await _db.Empresas
            .Include(e => e.Entidade)
            .OrderBy(e => e.Codigo)
            .ToListAsync();

        ViewBag.Empresas     = empresas;
        ViewBag.MultiEmpresa = empresas.Count > 1;
    }

    private static string HashSha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }
}
