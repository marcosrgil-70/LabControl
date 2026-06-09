using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class TabelasAuxiliaresController : Controller
{
    private readonly ApplicationDbContext _db;
    public TabelasAuxiliaresController(ApplicationDbContext db) => _db = db;

    public IActionResult Index() => View();

    // ── TIPOS DE AMOSTRA ──────────────────────────────────────────────
    public async Task<IActionResult> AmostrasTipos() =>
        View("Lista", new ListaAuxiliar("Tipos de Amostra", "AmostraTipo",
            await _db.AmostrasTipos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAmostraTipo(int? id, string descricao)
    {
        if (id.HasValue)
        {
            var reg = await _db.AmostrasTipos.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; }
        }
        else
            _db.AmostrasTipos.Add(new AmostraTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de amostra salvo!";
        return RedirectToAction(nameof(AmostrasTipos));
    }

    // ── TIPOS DE ANÁLISE ──────────────────────────────────────────────
    public async Task<IActionResult> AnalisesTipos() =>
        View("Lista", new ListaAuxiliar("Tipos de Análise", "AnaliseTipo",
            await _db.AnalisesTipos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAnaliseTipo(int? id, string descricao)
    {
        if (id.HasValue)
        {
            var reg = await _db.AnalisesTipos.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; }
        }
        else
            _db.AnalisesTipos.Add(new AnaliseTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de análise salvo!";
        return RedirectToAction(nameof(AnalisesTipos));
    }

    // ── MÉTODOS DE ANÁLISE ────────────────────────────────────────────
    public async Task<IActionResult> AnalisesMetodos() =>
        View("Lista", new ListaAuxiliar("Métodos de Análise", "AnaliseMetodo",
            await _db.AnalisesMetodos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAnaliseMetodo(int? id, string descricao)
    {
        if (id.HasValue)
        {
            var reg = await _db.AnalisesMetodos.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; }
        }
        else
            _db.AnalisesMetodos.Add(new AnaliseMetodo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Método de análise salvo!";
        return RedirectToAction(nameof(AnalisesMetodos));
    }

    // ── STATUS DE AMOSTRA ─────────────────────────────────────────────
    public async Task<IActionResult> AmostrasStatus() =>
        View("ListaStatus", new ListaAuxiliar("Status de Amostra", "AmostraStatus",
            await _db.AmostrasStatus.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAmostraStatus(int? id, string descricao, string? cor)
    {
        if (id.HasValue)
        {
            var reg = await _db.AmostrasStatus.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; reg.Cor = cor; }
        }
        else
            _db.AmostrasStatus.Add(new AmostraStatus { Descricao = descricao, Cor = cor });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Status de amostra salvo!";
        return RedirectToAction(nameof(AmostrasStatus));
    }

    // ── PRAZOS ────────────────────────────────────────────────────────
    public async Task<IActionResult> Prazos() =>
        View("ListaPrazos", await _db.Prazos.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarPrazo(int? id, string descricao, int qtde)
    {
        if (id.HasValue)
        {
            var reg = await _db.Prazos.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; reg.QtdeDias = qtde; }
        }
        else
            _db.Prazos.Add(new Prazo { Descricao = descricao, QtdeDias = qtde });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Prazo salvo!";
        return RedirectToAction(nameof(Prazos));
    }

    // ── UNIDADES ──────────────────────────────────────────────────────
    public async Task<IActionResult> Unidades() =>
        View("ListaUnidades", await _db.Unidades.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarUnidade(int? id, string descricao, string sigla)
    {
        if (id.HasValue)
        {
            var reg = await _db.Unidades.FindAsync(id);
            if (reg != null) { reg.Descricao = descricao; reg.Sigla = sigla; }
        }
        else
            _db.Unidades.Add(new Unidade { Descricao = descricao, Sigla = sigla });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Unidade salva!";
        return RedirectToAction(nameof(Unidades));
    }
}

// ViewModel para telas de listagem simples
public record ListaAuxiliar(string Titulo, string Entidade, List<object> Itens);
