using LabControl.Data;
using LabControl.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class RelatoriosController : Controller
{
    private readonly ApplicationDbContext _db;
    public RelatoriosController(ApplicationDbContext db) => _db = db;

    public IActionResult Index() => View();

    // ─── Relatório de Amostras por Período ───────────────────────────────────

    public async Task<IActionResult> Amostras(DateTime? dtInicio, DateTime? dtFim, int? idStatus)
    {
        var vm = new RelAmostrasVM
        {
            Filtro       = new() { DtInicio = dtInicio, DtFim = dtFim, IdStatus = idStatus },
            StatusOpcoes = await _db.AmostrasStatus.OrderBy(s => s.Descricao).ToListAsync()
        };

        if (dtInicio.HasValue && dtFim.HasValue)
        {
            var fim = dtFim.Value.Date.AddDays(1).AddTicks(-1);

            var query = _db.HistAmostras
                .Include(a => a.AmostraTipo)
                .Include(a => a.AnaliseTipo)
                .Include(a => a.Entidade)
                .Include(a => a.Produto)
                .Include(a => a.AmostraStatus)
                .Include(a => a.Saldo)
                .Where(a => a.DtEntrega >= dtInicio.Value.Date
                         && a.DtEntrega <= fim);

            if (idStatus.HasValue)
                query = query.Where(a => a.IdAmostraStatus == idStatus.Value);

            var lista = await query.OrderByDescending(a => a.DtEntrega).ToListAsync();

            vm.Resultado = lista.Select(a => new RelAmostraItem
            {
                Id          = a.Id,
                Codigo      = FormatarCodAmostra(a.AmostraTipo.Descricao, a.CodAmostra, a.AnaliseTipo?.Descricao, a.AnoAmostra),
                Cliente     = a.Entidade?.Nome ?? "-",
                TipoAmostra = a.AmostraTipo.Descricao,
                TipoAnalise = a.AnaliseTipo?.Descricao,
                Produto     = a.Produto?.Descricao,
                NrLote      = a.NrLote,
                DtEntrega   = a.DtEntrega,
                Status      = a.AmostraStatus?.Descricao ?? "-",
                CorStatus   = a.AmostraStatus?.Cor,
                Saldo       = a.Saldo?.SaldoAtual
            }).ToList();
        }

        return View(vm);
    }

    // ─── Relatório de Propostas por Período ──────────────────────────────────

    public async Task<IActionResult> Propostas(DateTime? dtInicio, DateTime? dtFim, int? idStatus)
    {
        var vm = new RelPropostasVM
        {
            Filtro       = new() { DtInicio = dtInicio, DtFim = dtFim, IdStatus = idStatus },
            StatusOpcoes = await _db.PropostasStatus.OrderBy(s => s.Descricao).ToListAsync()
        };

        if (dtInicio.HasValue && dtFim.HasValue)
        {
            var fim = dtFim.Value.Date.AddDays(1).AddTicks(-1);

            var query = _db.Propostas
                .Include(p => p.Entidade)
                .Include(p => p.Status)
                .Include(p => p.Moeda)
                .Include(p => p.Analises)
                .Where(p => p.DtSolicitacao >= dtInicio.Value.Date
                         && p.DtSolicitacao <= fim);

            if (idStatus.HasValue)
                query = query.Where(p => p.IdStatus == idStatus.Value);

            var lista = await query.OrderByDescending(p => p.DtSolicitacao).ToListAsync();

            vm.Resultado = lista.Select(p => new RelPropostaItem
            {
                Id            = p.Id,
                Codigo        = p.CodigoFormatado,
                Cliente       = p.Entidade.Nome,
                DtSolicitacao = p.DtSolicitacao,
                DtValidade    = p.DtValidade,
                Status        = p.Status?.Descricao ?? "-",
                CorStatus     = p.Status?.Cor,
                VrTotal       = p.VrTotal,
                Moeda         = p.Moeda?.Sigla,
                QtdItens      = p.Analises.Count
            }).ToList();
        }

        return View(vm);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static string FormatarCodAmostra(string tipoAmostra, int cod, string? tipoAnalise, int ano)
    {
        var siglaA = tipoAmostra.Length >= 2 ? tipoAmostra[..2] : tipoAmostra;
        var siglaT = tipoAnalise != null
            ? (tipoAnalise.Length >= 2 ? tipoAnalise[..2] : tipoAnalise)
            : "??";
        return $"{siglaA}{cod:D3}{siglaT}{ano}";
    }
}
