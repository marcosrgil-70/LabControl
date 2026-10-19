using LabControl.Data;
using LabControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class PermissoesController : Controller
{
    private readonly ApplicationDbContext _db;
    public PermissoesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var lista = await _db.Permissoes.OrderBy(p => p.Descricao).ToListAsync();
        return View(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Salvar(int id, string codigo, string descricao)
    {
        codigo    = (codigo    ?? "").Trim().ToUpper();
        descricao = (descricao ?? "").Trim();

        if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(descricao))
            return BadRequest("Código e descrição são obrigatórios.");

        if (await _db.Permissoes.AnyAsync(p => p.Codigo == codigo && p.Id != id))
            return BadRequest("Já existe uma permissão com este código.");

        if (id == 0)
        {
            _db.Permissoes.Add(new Permissao { Codigo = codigo, Descricao = descricao });
        }
        else
        {
            var p = await _db.Permissoes.FindAsync(id);
            if (p == null) return NotFound();
            p.Codigo    = codigo;
            p.Descricao = descricao;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Excluir(int id)
    {
        var p = await _db.Permissoes.FindAsync(id);
        if (p == null) return NotFound();

        var emUso = await _db.PerfisPermissoes.AnyAsync(pp => pp.IdPermissao == id);
        if (emUso)
            return BadRequest("Permissão está vinculada a um ou mais perfis.");

        _db.Permissoes.Remove(p);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
