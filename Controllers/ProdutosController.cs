using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class ProdutosController : Controller
{
    private readonly ApplicationDbContext _db;
    public ProdutosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? busca)
    {
        ViewBag.Busca = busca;
        var query = _db.Produtos
            .Include(p => p.Unidade)
            .Include(p => p.EmbalagemTipo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Descricao.Contains(busca) || p.Codigo.Contains(busca));

        return View(await query.OrderBy(p => p.Descricao).ToListAsync());
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarFormulario();
        return View(new Produto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Produto model)
    {
        if (!ModelState.IsValid) { await CarregarFormulario(); return View(model); }

        // Verificar código duplicado
        if (await _db.Produtos.AnyAsync(p => p.Codigo == model.Codigo))
        {
            ModelState.AddModelError("Codigo", "Já existe um produto com este código.");
            await CarregarFormulario();
            return View(model);
        }

        _db.Produtos.Add(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Produto \"{model.Descricao}\" cadastrado!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null) return NotFound();
        await CarregarFormulario();
        return View(produto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Produto model)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null) return NotFound();

        // Verificar código duplicado (exceto o próprio)
        if (await _db.Produtos.AnyAsync(p => p.Codigo == model.Codigo && p.Id != id))
        {
            ModelState.AddModelError("Codigo", "Já existe um produto com este código.");
            await CarregarFormulario();
            return View(model);
        }

        produto.Codigo = model.Codigo;
        produto.Descricao = model.Descricao;
        produto.IdUnidade = model.IdUnidade;
        produto.IdEmbalagemTipo = model.IdEmbalagemTipo;
        produto.QtdeEmbalagem = model.QtdeEmbalagem;

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Produto \"{produto.Descricao}\" atualizado!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Excluir(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto != null)
        {
            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Produto removido.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task CarregarFormulario()
    {
        ViewBag.Unidades = await _db.Unidades.OrderBy(u => u.Descricao).ToListAsync();
        ViewBag.EmbalagensTopos = await _db.EmbalagensTopos.OrderBy(e => e.Descricao).ToListAsync();
    }
}
