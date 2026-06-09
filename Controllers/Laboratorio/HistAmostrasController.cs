using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class HistAmostrasController : Controller
{
    private readonly ApplicationDbContext _db;

    public HistAmostrasController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var amostras = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.AmostraStatus)
            .Include(a => a.Saldo)
            .OrderByDescending(a => a.AnoAmostra)
            .ThenByDescending(a => a.CodAmostra)
            .ToListAsync();

        return View(amostras);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var amostra = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaFisica)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaJuridica)
            .Include(a => a.Produto)
            .Include(a => a.EmbalagemTipo)
            .Include(a => a.AmostraStatus)
            .Include(a => a.Saldo)
            .Include(a => a.LocalizacaoAtual)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseTipo)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseMetodo)
            .Include(a => a.Movimentacoes)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (amostra == null) return NotFound();

        return View(amostra);
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarDadosFormulario();
        return View(new HistAmostra { AnoAmostra = DateTime.Now.Year });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(HistAmostra model)
    {
        if (!ModelState.IsValid)
        {
            await CarregarDadosFormulario();
            return View(model);
        }

        // Gerar próximo código de amostra do ano
        var ultimoCod = await _db.HistAmostras
            .Where(a => a.AnoAmostra == model.AnoAmostra && a.IdAmostraTipo == model.IdAmostraTipo)
            .MaxAsync(a => (int?)a.CodAmostra) ?? 0;

        model.CodAmostra = ultimoCod + 1;
        model.IdEmpresa = 1; // TODO: pegar empresa da sessão

        _db.HistAmostras.Add(model);
        await _db.SaveChangesAsync();

        // Criar movimentação de entrada automática
        var mov = new MovAmostra
        {
            IdHistAmostra = model.Id,
            IdEmpresa = model.IdEmpresa,
            Qtde = model.QtdeEmbalagensEntregue ?? 1,
            EntradaSaida = "E",
            DataMov = DateTime.Now,
            Justificativa = "Entrada inicial de amostra"
        };
        _db.MovAmostras.Add(mov);

        // Inicializar saldo
        var saldo = new HistAmostraSaldo
        {
            IdHistAmostra = model.Id,
            IdEmpresa = model.IdEmpresa,
            SaldoAtual = mov.Qtde,
            DataAtualizacao = DateTime.Now
        };
        _db.HistAmostrasaldos.Add(saldo);

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Amostra {model.CodigoFormatado} registrada com sucesso!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    private async Task CarregarDadosFormulario()
    {
        ViewBag.AmostrasTipos = await _db.AmostrasTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.Produtos = await _db.Produtos.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.EmbalagensTopos = await _db.EmbalagensTopos.OrderBy(e => e.Descricao).ToListAsync();
        ViewBag.AmostrasStatus = await _db.AmostrasStatus.OrderBy(s => s.Descricao).ToListAsync();
    }
}
