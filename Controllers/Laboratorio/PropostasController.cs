using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class PropostasController : Controller
{
    private readonly ApplicationDbContext _db;
    public PropostasController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var propostas = await _db.Propostas
            .Include(p => p.Entidade)
            .Include(p => p.Status)
            .Include(p => p.Moeda)
            .Include(p => p.Analises)
            .OrderByDescending(p => p.AnoProposta)
            .ThenByDescending(p => p.CodProposta)
            .ToListAsync();
        return View(propostas);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Entidade).ThenInclude(e => e.PessoaFisica)
            .Include(p => p.Entidade).ThenInclude(e => e.PessoaJuridica)
            .Include(p => p.Status)
            .Include(p => p.CondicaoPagamento)
            .Include(p => p.Moeda)
            .Include(p => p.Analises).ThenInclude(a => a.Produto)
            .Include(p => p.Analises).ThenInclude(a => a.AnaliseTipo)
            .Include(p => p.Analises).ThenInclude(a => a.AnaliseMetodo)
            .Include(p => p.Analises).ThenInclude(a => a.Prazo)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proposta == null) return NotFound();
        return View(proposta);
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarFormulario();
        return View(new Proposta { AnoProposta = DateTime.Now.Year, DtSolicitacao = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Proposta model)
    {
        var ultimoCod = await _db.Propostas
            .Where(p => p.AnoProposta == model.AnoProposta)
            .MaxAsync(p => (int?)p.CodProposta) ?? 0;

        model.CodProposta = ultimoCod + 1;
        model.RevProposta = 0;
        model.IdEmpresa = 1;

        _db.Propostas.Add(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Proposta {model.CodigoFormatado} criada!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    // POST: adicionar item de análise à proposta
    [HttpPost]
    public async Task<IActionResult> AdicionarItem(int idProposta,
        int? idProduto, int? idAnaliseTipo, int? idAnaliseMetodo,
        int? idParametro, int? idIdioma, int? idPrazo,
        int qtde, decimal vrUnitario, decimal vrDesconto)
    {
        var item = new PropostaAnalise
        {
            IdProposta = idProposta,
            IdProduto = idProduto,
            IdAnaliseTipo = idAnaliseTipo,
            IdAnaliseMetodo = idAnaliseMetodo,
            IdParametroAnalise = idParametro,
            IdIdioma = idIdioma,
            IdPrazo = idPrazo,
            QtdeAmostras = qtde,
            VrUnitario = vrUnitario,
            VrDesconto = vrDesconto,
            VrTotal = (vrUnitario * qtde) - vrDesconto
        };
        _db.PropostasAnalises.Add(item);
        await _db.SaveChangesAsync();

        // Recalcular total da proposta
        await RecalcularTotal(idProposta);

        TempData["Sucesso"] = "Item adicionado!";
        return RedirectToAction(nameof(Detalhes), new { id = idProposta });
    }

    [HttpPost]
    public async Task<IActionResult> RemoverItem(int idItem, int idProposta)
    {
        var item = await _db.PropostasAnalises.FindAsync(idItem);
        if (item != null)
        {
            _db.PropostasAnalises.Remove(item);
            await _db.SaveChangesAsync();
            await RecalcularTotal(idProposta);
        }
        return RedirectToAction(nameof(Detalhes), new { id = idProposta });
    }

    [HttpPost]
    public async Task<IActionResult> AplicarDesconto(int id, decimal porcDesconto)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Analises)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proposta != null)
        {
            var subtotal = proposta.Analises.Sum(a => a.VrTotal);
            proposta.PorcDesconto = porcDesconto;
            proposta.VrDesconto = subtotal * (porcDesconto / 100);
            proposta.VrTotal = subtotal - (proposta.VrDesconto ?? 0);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    private async Task RecalcularTotal(int idProposta)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Analises)
            .FirstOrDefaultAsync(p => p.Id == idProposta);

        if (proposta == null) return;

        var subtotal = proposta.Analises.Sum(a => a.VrTotal);
        proposta.VrDesconto = subtotal * ((proposta.PorcDesconto ?? 0) / 100);
        proposta.VrTotal = subtotal - (proposta.VrDesconto ?? 0);
        await _db.SaveChangesAsync();
    }

    private async Task CarregarFormulario()
    {
        ViewBag.PropostasStatus = await _db.PropostasStatus.OrderBy(s => s.Descricao).ToListAsync();
        ViewBag.CondicoesPagamentos = await _db.CondicoesPagamentos.OrderBy(c => c.Descricao).ToListAsync();
        ViewBag.Moedas = await _db.Moedas.OrderBy(m => m.Descricao).ToListAsync();
        ViewBag.Clientes = await _db.Entidades
            .Where(e => e.TipoCliente && !e.Inativo)
            .OrderBy(e => e.Nome).ToListAsync();
        ViewBag.Produtos = await _db.Produtos.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.AnalisesMetodos = await _db.AnalisesMetodos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.Prazos = await _db.Prazos.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.Idiomas = await _db.Idiomas.OrderBy(i => i.Descricao).ToListAsync();
        ViewBag.Parametros = await _db.ParametrosAnalises.OrderBy(p => p.Descricao).ToListAsync();
    }
}
