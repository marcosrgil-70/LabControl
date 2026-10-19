using LabControl.Data;
using LabControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class PerfisController : Controller
{
    private readonly ApplicationDbContext _db;
    public PerfisController(ApplicationDbContext db) => _db = db;

    // ─── Index ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        ViewBag.Perfis     = await _db.Perfis.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.Permissoes = await _db.Permissoes.OrderBy(p => p.Descricao).ToListAsync();
        return View();
    }

    // ─── CRUD de Perfil (AJAX) ────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> SalvarPerfil(int id, string codigo, string descricao)
    {
        codigo    = (codigo    ?? "").Trim().ToUpper();
        descricao = (descricao ?? "").Trim();

        if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(descricao))
            return BadRequest("Código e descrição são obrigatórios.");

        if (await _db.Perfis.AnyAsync(p => p.Codigo == codigo && p.Id != id))
            return BadRequest("Já existe um perfil com este código.");

        Perfil perfil;
        if (id == 0)
        {
            perfil = new Perfil { Codigo = codigo, Descricao = descricao };
            _db.Perfis.Add(perfil);
            await _db.SaveChangesAsync();
        }
        else
        {
            perfil = await _db.Perfis.FindAsync(id) ?? throw new InvalidOperationException();
            perfil.Codigo    = codigo;
            perfil.Descricao = descricao;
            await _db.SaveChangesAsync();
        }

        return Json(new { id = perfil.Id, codigo = perfil.Codigo, descricao = perfil.Descricao });
    }

    [HttpPost]
    public async Task<IActionResult> ExcluirPerfil(int id)
    {
        var perfil = await _db.Perfis.FindAsync(id);
        if (perfil == null) return NotFound();

        var emUso = await _db.UsuariosPerfis.AnyAsync(up => up.IdPerfil == id);
        if (emUso)
            return BadRequest("Perfil está vinculado a um ou mais usuários.");

        _db.Perfis.Remove(perfil);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ─── Permissões do Perfil (AJAX) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> PermissoesDoPerfil(int idPerfil)
    {
        var todas = await _db.Permissoes.OrderBy(p => p.Descricao).ToListAsync();

        var vinculadas = await _db.PerfisPermissoes
            .Where(pp => pp.IdPerfil == idPerfil)
            .ToListAsync();

        var result = todas.Select(p =>
        {
            var v = vinculadas.FirstOrDefault(x => x.IdPermissao == p.Id);
            return new
            {
                idPermissao = p.Id,
                descricao   = p.Descricao,
                incluir     = v?.Incluir   ?? false,
                alterar     = v?.Alterar   ?? false,
                consultar   = v?.Consultar ?? false,
                excluir     = v?.Excluir   ?? false,
                imprimir    = v?.Imprimir  ?? false,
            };
        });

        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SalvarPermissoesPerfil(
        int idPerfil,
        [FromBody] List<PerfilPermissaoInput> permissoes)
    {
        if (!await _db.Perfis.AnyAsync(p => p.Id == idPerfil))
            return NotFound();

        var existentes = await _db.PerfisPermissoes
            .Where(pp => pp.IdPerfil == idPerfil)
            .ToListAsync();

        _db.PerfisPermissoes.RemoveRange(existentes);

        foreach (var perm in permissoes)
        {
            if (!perm.Incluir && !perm.Alterar && !perm.Consultar && !perm.Excluir && !perm.Imprimir)
                continue;

            _db.PerfisPermissoes.Add(new PerfilPermissao
            {
                IdPerfil    = idPerfil,
                IdPermissao = perm.IdPermissao,
                Incluir     = perm.Incluir,
                Alterar     = perm.Alterar,
                Consultar   = perm.Consultar,
                Excluir     = perm.Excluir,
                Imprimir    = perm.Imprimir,
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class PerfilPermissaoInput
{
    public int IdPermissao { get; set; }
    public bool Incluir    { get; set; }
    public bool Alterar    { get; set; }
    public bool Consultar  { get; set; }
    public bool Excluir    { get; set; }
    public bool Imprimir   { get; set; }
}
