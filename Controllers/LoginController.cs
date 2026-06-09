using LabControl.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LabControl.Controllers;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _db;
    public LoginController(ApplicationDbContext db) => _db = db;

    public IActionResult Index()
    {
        // Já logado → redireciona
        if (HttpContext.Session.GetString("UsuarioNome") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string login, string senha)
    {
        var senhaHash = HashSha256(senha);

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Nome == login && u.SenhaHash == senhaHash && !u.Inativo);

        if (usuario == null)
        {
            ViewBag.Erro = "Login ou senha incorretos.";
            return View();
        }

        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        HttpContext.Session.SetString("UsuarioNome", usuario.Nome);
        HttpContext.Session.SetString("UsuarioAdmin", usuario.IsAdmin ? "1" : "0");

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Sair()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Index));
    }

    private static string HashSha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }
}
