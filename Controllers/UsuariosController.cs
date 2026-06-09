using LabControl.Data;
using LabControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LabControl.Controllers;

public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _db;

    private static readonly (string Form, string Nome)[] _formularios =
    [
        ("Clientes",          "Clientes"),
        ("Produtos",          "Produtos"),
        ("Parametros",        "Parâmetros de Análise"),
        ("TabelasAuxiliares", "Tabelas Auxiliares"),
        ("HistAmostras",      "Amostras"),
        ("Propostas",         "Propostas"),
        ("Resultados",        "Resultados"),
        ("Usuarios",          "Usuários"),
    ];

    public UsuariosController(ApplicationDbContext db) => _db = db;

    // ─── Index ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var lista = await _db.Usuarios.OrderBy(u => u.Nome).ToListAsync();
        return View(lista);
    }

    // ─── Criar ───────────────────────────────────────────────────────────────

    public IActionResult Criar() => View(NovoVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(UsuarioEditVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nome))
            ModelState.AddModelError(nameof(vm.Nome), "O nome é obrigatório.");
        else if (await _db.Usuarios.AnyAsync(u => u.Nome == vm.Nome))
            ModelState.AddModelError(nameof(vm.Nome), "Já existe um usuário com este nome.");

        if (string.IsNullOrWhiteSpace(vm.NovaSenha))
            ModelState.AddModelError(nameof(vm.NovaSenha), "A senha é obrigatória para novos usuários.");
        else if (vm.NovaSenha != vm.ConfirmarSenha)
            ModelState.AddModelError(nameof(vm.ConfirmarSenha), "As senhas não conferem.");

        if (!ModelState.IsValid)
            return View(EnsureNomes(vm));

        var usuario = new Usuario
        {
            Nome     = vm.Nome,
            IsAdmin  = vm.IsAdmin,
            Inativo  = vm.Inativo,
            SenhaHash = HashSha256(vm.NovaSenha!)
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        SalvarAcoes(vm, usuario.Id);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Usuário \"{usuario.Nome}\" cadastrado!";
        return RedirectToAction(nameof(Index));
    }

    // ─── Editar ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        var salvas = await _db.AcoesUsuarios.Where(a => a.IdUsuario == id).ToListAsync();

        var vm = new UsuarioEditVM
        {
            Id      = usuario.Id,
            Nome    = usuario.Nome,
            IsAdmin = usuario.IsAdmin,
            Inativo = usuario.Inativo,
            Acoes   = _formularios.Select(f =>
            {
                var s = salvas.FirstOrDefault(a => a.Form == f.Form);
                return new AcaoVM
                {
                    Form      = f.Form,
                    NomeForm  = f.Nome,
                    Incluir   = s?.Incluir   ?? false,
                    Alterar   = s?.Alterar   ?? false,
                    Consultar = s?.Consultar ?? false,
                    Excluir   = s?.Excluir   ?? false,
                    Imprimir  = s?.Imprimir  ?? false,
                };
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioEditVM vm)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        if (string.IsNullOrWhiteSpace(vm.Nome))
            ModelState.AddModelError(nameof(vm.Nome), "O nome é obrigatório.");
        else if (await _db.Usuarios.AnyAsync(u => u.Nome == vm.Nome && u.Id != id))
            ModelState.AddModelError(nameof(vm.Nome), "Já existe um usuário com este nome.");

        if (!string.IsNullOrWhiteSpace(vm.NovaSenha) && vm.NovaSenha != vm.ConfirmarSenha)
            ModelState.AddModelError(nameof(vm.ConfirmarSenha), "As senhas não conferem.");

        if (!ModelState.IsValid)
            return View(EnsureNomes(vm));

        usuario.Nome    = vm.Nome;
        usuario.IsAdmin = vm.IsAdmin;
        usuario.Inativo = vm.Inativo;

        if (!string.IsNullOrWhiteSpace(vm.NovaSenha))
            usuario.SenhaHash = HashSha256(vm.NovaSenha);

        var existentes = await _db.AcoesUsuarios.Where(a => a.IdUsuario == id).ToListAsync();
        _db.AcoesUsuarios.RemoveRange(existentes);

        SalvarAcoes(vm, id);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Usuário \"{usuario.Nome}\" atualizado!";
        return RedirectToAction(nameof(Index));
    }

    // ─── Alternar Status ─────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AlternarStatus(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario != null)
        {
            usuario.Inativo = !usuario.Inativo;
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = usuario.Inativo
                ? $"Usuário \"{usuario.Nome}\" desativado."
                : $"Usuário \"{usuario.Nome}\" ativado.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SalvarAcoes(UsuarioEditVM vm, int idUsuario)
    {
        if (vm.IsAdmin) return;
        foreach (var a in vm.Acoes)
        {
            _db.AcoesUsuarios.Add(new AcaoUsuario
            {
                IdUsuario = idUsuario,
                Form      = a.Form,
                Incluir   = a.Incluir,
                Alterar   = a.Alterar,
                Consultar = a.Consultar,
                Excluir   = a.Excluir,
                Imprimir  = a.Imprimir,
            });
        }
    }

    private UsuarioEditVM NovoVM() => new()
    {
        Acoes = _formularios.Select(f => new AcaoVM { Form = f.Form, NomeForm = f.Nome }).ToList()
    };

    private static UsuarioEditVM EnsureNomes(UsuarioEditVM vm)
    {
        var mapa = _formularios.ToDictionary(f => f.Form, f => f.Nome);
        foreach (var a in vm.Acoes)
            if (mapa.TryGetValue(a.Form, out var nome))
                a.NomeForm = nome;
        return vm;
    }

    private static string HashSha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }
}
