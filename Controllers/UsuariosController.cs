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
    public UsuariosController(ApplicationDbContext db) => _db = db;

    // ─── Index ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var lista = await _db.Usuarios
            .Include(u => u.UsuariosPerfis).ThenInclude(up => up.Perfil)
            .OrderBy(u => u.Nome)
            .ToListAsync();
        return View(lista);
    }

    // ─── Criar ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Criar() => View(await NovoVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(UsuarioEditVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Codigo))
            ModelState.AddModelError(nameof(vm.Codigo), "O código (login) é obrigatório.");
        else if (await _db.Usuarios.AnyAsync(u => u.Codigo == vm.Codigo))
            ModelState.AddModelError(nameof(vm.Codigo), "Já existe um usuário com este código.");

        if (string.IsNullOrWhiteSpace(vm.Nome))
            ModelState.AddModelError(nameof(vm.Nome), "O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(vm.NovaSenha))
            ModelState.AddModelError(nameof(vm.NovaSenha), "A senha é obrigatória para novos usuários.");
        else if (vm.NovaSenha != vm.ConfirmarSenha)
            ModelState.AddModelError(nameof(vm.ConfirmarSenha), "As senhas não conferem.");

        if (!ModelState.IsValid)
            return View(await RecarregarPerfis(vm));

        var usuario = new Usuario
        {
            Codigo    = vm.Codigo.Trim().ToUpper(),
            Nome      = vm.Nome.Trim(),
            IsAdmin   = vm.IsAdmin,
            Inativo   = vm.Inativo,
            SenhaHash = HashSha256(vm.NovaSenha!)
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        await SalvarPerfis(vm, usuario.Id);

        TempData["Sucesso"] = $"Usuário \"{usuario.Codigo}\" cadastrado!";
        return RedirectToAction(nameof(Index));
    }

    // ─── Editar ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        var perfisDoUsuario = await _db.UsuariosPerfis
            .Where(up => up.IdUsuario == id)
            .Select(up => up.IdPerfil)
            .ToListAsync();

        var todosPerfis = await _db.Perfis.OrderBy(p => p.Descricao).ToListAsync();

        var vm = new UsuarioEditVM
        {
            Id      = usuario.Id,
            Codigo  = usuario.Codigo,
            Nome    = usuario.Nome,
            IsAdmin = usuario.IsAdmin,
            Inativo = usuario.Inativo,
            Perfis  = todosPerfis.Select(p => new PerfilSelecaoVM
            {
                IdPerfil    = p.Id,
                Codigo      = p.Codigo,
                Descricao   = p.Descricao,
                Selecionado = perfisDoUsuario.Contains(p.Id)
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

        if (string.IsNullOrWhiteSpace(vm.Codigo))
            ModelState.AddModelError(nameof(vm.Codigo), "O código (login) é obrigatório.");
        else if (await _db.Usuarios.AnyAsync(u => u.Codigo == vm.Codigo && u.Id != id))
            ModelState.AddModelError(nameof(vm.Codigo), "Já existe um usuário com este código.");

        if (string.IsNullOrWhiteSpace(vm.Nome))
            ModelState.AddModelError(nameof(vm.Nome), "O nome é obrigatório.");

        if (!string.IsNullOrWhiteSpace(vm.NovaSenha) && vm.NovaSenha != vm.ConfirmarSenha)
            ModelState.AddModelError(nameof(vm.ConfirmarSenha), "As senhas não conferem.");

        if (!ModelState.IsValid)
            return View(await RecarregarPerfis(vm));

        usuario.Codigo  = vm.Codigo.Trim().ToUpper();
        usuario.Nome    = vm.Nome.Trim();
        usuario.IsAdmin = vm.IsAdmin;
        usuario.Inativo = vm.Inativo;

        if (!string.IsNullOrWhiteSpace(vm.NovaSenha))
            usuario.SenhaHash = HashSha256(vm.NovaSenha);

        var existentes = await _db.UsuariosPerfis.Where(up => up.IdUsuario == id).ToListAsync();
        _db.UsuariosPerfis.RemoveRange(existentes);

        await SalvarPerfis(vm, id);

        TempData["Sucesso"] = $"Usuário \"{usuario.Codigo}\" atualizado!";
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
                ? $"Usuário \"{usuario.Codigo}\" desativado."
                : $"Usuário \"{usuario.Codigo}\" ativado.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task SalvarPerfis(UsuarioEditVM vm, int idUsuario)
    {
        foreach (var p in vm.Perfis.Where(p => p.Selecionado))
        {
            _db.UsuariosPerfis.Add(new UsuarioPerfil
            {
                IdUsuario = idUsuario,
                IdPerfil  = p.IdPerfil
            });
        }
        await _db.SaveChangesAsync();
    }

    private async Task<UsuarioEditVM> NovoVM()
    {
        var perfis = await _db.Perfis.OrderBy(p => p.Descricao).ToListAsync();
        return new UsuarioEditVM
        {
            Perfis = perfis.Select(p => new PerfilSelecaoVM
            {
                IdPerfil  = p.Id,
                Codigo    = p.Codigo,
                Descricao = p.Descricao
            }).ToList()
        };
    }

    private async Task<UsuarioEditVM> RecarregarPerfis(UsuarioEditVM vm)
    {
        var todosPerfis = await _db.Perfis.OrderBy(p => p.Descricao).ToListAsync();
        var selecionados = vm.Perfis.Where(p => p.Selecionado).Select(p => p.IdPerfil).ToHashSet();

        vm.Perfis = todosPerfis.Select(p => new PerfilSelecaoVM
        {
            IdPerfil    = p.Id,
            Codigo      = p.Codigo,
            Descricao   = p.Descricao,
            Selecionado = selecionados.Contains(p.Id)
        }).ToList();
        return vm;
    }

    private static string HashSha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }
}
