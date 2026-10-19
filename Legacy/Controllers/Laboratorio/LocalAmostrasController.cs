using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class LocalAmostrasController : Controller
{
    private readonly ApplicationDbContext _db;
    public LocalAmostrasController(ApplicationDbContext db) => _db = db;

    // ─── Index: busca de amostras para localizar ──────────────────────────────
    public async Task<IActionResult> Index(int? idCliente, int? idProduto, string? codAmostra)
    {
        ViewBag.Clientes  = await _db.Entidades
            .Where(e => e.TipoCliente && !e.Inativo)
            .OrderBy(e => e.Nome)
            .ToListAsync();
        ViewBag.Produtos  = await _db.Produtos.Where(p => !p.Inativo).OrderBy(p => p.Descricao).ToListAsync();

        ViewBag.IdCliente  = idCliente;
        ViewBag.IdProduto  = idProduto;
        ViewBag.CodAmostra = codAmostra;

        bool temFiltro = idCliente.HasValue || idProduto.HasValue || !string.IsNullOrWhiteSpace(codAmostra);
        if (!temFiltro)
            return View(new List<HistAmostra>());

        var query = _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.Produto)
            .Include(a => a.AmostraStatus)
            .Include(a => a.LocalizacaoAtual)
            .AsQueryable();

        if (idCliente > 0)
            query = query.Where(a => a.IdEntidade == idCliente);
        if (idProduto > 0)
            query = query.Where(a => a.IdProduto == idProduto);
        if (!string.IsNullOrWhiteSpace(codAmostra))
        {
            // Tenta parsear código formatado TT-SSSSS-AA/YY
            if (int.TryParse(codAmostra, out var codNum))
                query = query.Where(a => a.CodAmostra == codNum);
            else
                query = query.Where(a => a.NrLote != null && a.NrLote.Contains(codAmostra));
        }

        return View(await query.OrderByDescending(a => a.AnoAmostra).ThenByDescending(a => a.CodAmostra).ToListAsync());
    }

    // ─── Editar localização de uma amostra ────────────────────────────────────
    public async Task<IActionResult> Editar(int id)
    {
        var amostra = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.Produto)
            .Include(a => a.AmostraStatus)
            .Include(a => a.LocalizacaoAtual)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (amostra == null) return NotFound();

        // Se ainda não foi arquivada, prepara objeto vazio para o form
        if (amostra.LocalizacaoAtual == null)
        {
            ViewBag.LocalVazio = true;
        }
        else
        {
            // Se já foi descartada, carrega nome do funcionário que fez o descarte
            if (amostra.LocalizacaoAtual.IdFuncionarioDescarte.HasValue)
            {
                var func = await _db.EntidadesFuncionarios
                    .Include(f => f.Entidade)
                    .FirstOrDefaultAsync(f => f.Id == amostra.LocalizacaoAtual.IdFuncionarioDescarte);
                ViewBag.NomeFuncionarioDescarte = func?.Entidade?.Nome;
            }
        }

        return View(amostra);
    }

    // ─── Salvar / Arquivar ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarLocalizacao(int idHistAmostra, int status,
        DateTime? dtHrArquivo, string? nrArmario, string? nrPrateleira, string? nrCaixa, string? observacao)
    {
        var empresaId = HttpContext.Session.GetInt32("EmpresaId") ?? 1;

        var local = await _db.LocalAmostras.FirstOrDefaultAsync(l => l.IdHistAmostra == idHistAmostra);
        if (local == null)
        {
            local = new LocalAmostra
            {
                IdHistAmostra = idHistAmostra,
                IdEmpresa     = empresaId,
                Status        = status,
                DtHrArquivo   = dtHrArquivo ?? DateTime.Now,
                NrArmario     = nrArmario?.Trim(),
                NrPrateleira  = nrPrateleira?.Trim(),
                NrCaixa       = nrCaixa?.Trim(),
                Observacao    = observacao?.Trim()
            };
            _db.LocalAmostras.Add(local);
        }
        else
        {
            local.Status       = status;
            local.DtHrArquivo  = dtHrArquivo ?? local.DtHrArquivo;
            local.NrArmario    = nrArmario?.Trim();
            local.NrPrateleira = nrPrateleira?.Trim();
            local.NrCaixa      = nrCaixa?.Trim();
            local.Observacao   = observacao?.Trim();
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Localização salva com sucesso.";
        return RedirectToAction(nameof(Editar), new { id = idHistAmostra });
    }

    // ─── Marcar como Descartada ───────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Descartar(int idHistAmostra)
    {
        var funcionarioId = HttpContext.Session.GetInt32("UsuarioId");
        var local = await _db.LocalAmostras.FirstOrDefaultAsync(l => l.IdHistAmostra == idHistAmostra);
        if (local == null) return NotFound();

        local.Status               = 1;
        local.DtHrDescarte         = DateTime.Now;
        local.IdFuncionarioDescarte = funcionarioId;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Amostra marcada como descartada.";
        return RedirectToAction(nameof(Editar), new { id = idHistAmostra });
    }

    // ─── Desfazer Descarte ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesfazerDescarte(int idHistAmostra)
    {
        var local = await _db.LocalAmostras.FirstOrDefaultAsync(l => l.IdHistAmostra == idHistAmostra);
        if (local == null) return NotFound();

        local.Status                = 0;
        local.DtHrDescarte          = null;
        local.IdFuncionarioDescarte = null;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Descarte desfeito. Amostra retornou ao status Arquivada.";
        return RedirectToAction(nameof(Editar), new { id = idHistAmostra });
    }
}
