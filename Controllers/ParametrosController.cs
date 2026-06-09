using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class ParametrosController : Controller
{
    private readonly ApplicationDbContext _db;
    public ParametrosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? busca, int? idAnaliseTipo)
    {
        ViewBag.Busca = busca;
        ViewBag.IdAnaliseTipo = idAnaliseTipo;
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();

        var query = _db.ParametrosAnalises
            .Include(p => p.AnaliseTipo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Descricao.Contains(busca));

        if (idAnaliseTipo.HasValue)
            query = query.Where(p => p.IdAnaliseTipo == idAnaliseTipo);

        return View(await query.OrderBy(p => p.Descricao).ToListAsync());
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarFormulario();
        return View(new ParametroAnalise());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ParametroAnalise model)
    {
        if (!ModelState.IsValid) { await CarregarFormulario(); return View(model); }

        _db.ParametrosAnalises.Add(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Parâmetro \"{model.Descricao}\" cadastrado!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var param = await _db.ParametrosAnalises
            .Include(p => p.AnaliseTipo)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (param == null) return NotFound();
        await CarregarFormulario();
        return View(param);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ParametroAnalise model)
    {
        var param = await _db.ParametrosAnalises.FindAsync(id);
        if (param == null) return NotFound();

        param.Descricao = model.Descricao;
        param.IdAnaliseTipo = model.IdAnaliseTipo;
        param.VrUnitario = model.VrUnitario;

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Parâmetro \"{param.Descricao}\" atualizado!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Excluir(int id)
    {
        var param = await _db.ParametrosAnalises.FindAsync(id);
        if (param != null)
        {
            _db.ParametrosAnalises.Remove(param);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Parâmetro removido.";
        }
        return RedirectToAction(nameof(Index));
    }

    // AJAX: retorna parâmetros por tipo de análise (usado no formulário de proposta)
    [HttpGet]
    public async Task<IActionResult> PorTipo(int idAnaliseTipo)
    {
        var params_ = await _db.ParametrosAnalises
            .Where(p => p.IdAnaliseTipo == idAnaliseTipo)
            .OrderBy(p => p.Descricao)
            .Select(p => new { p.Id, p.Descricao, p.VrUnitario })
            .ToListAsync();
        return Json(params_);
    }

    private async Task CarregarFormulario()
    {
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
    }
}
