using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class MovimentacaoController : Controller
{
    private readonly ApplicationDbContext _db;
    public MovimentacaoController(ApplicationDbContext db) => _db = db;

    // ─── Index: busca de amostras para movimentar ─────────────────────────────
    public async Task<IActionResult> Index(int? idCliente, int? idAnaliseTipo, int? idAmostraTipo,
        string? nrAmostra, string? nrProposta, int? anoAmostra)
    {
        ViewBag.Clientes      = await _db.Entidades
            .Where(e => e.TipoCliente && !e.Inativo)
            .OrderBy(e => e.Nome)
            .ToListAsync();
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.AmostrasTipos = await _db.AmostrasTipos.OrderBy(a => a.Descricao).ToListAsync();

        ViewBag.IdCliente     = idCliente;
        ViewBag.IdAnaliseTipo = idAnaliseTipo;
        ViewBag.IdAmostraTipo = idAmostraTipo;
        ViewBag.NrAmostra     = nrAmostra;
        ViewBag.NrProposta    = nrProposta;
        ViewBag.AnoAmostra    = anoAmostra;

        bool temFiltro = idCliente.HasValue || idAnaliseTipo.HasValue || idAmostraTipo.HasValue
            || anoAmostra.HasValue || !string.IsNullOrWhiteSpace(nrAmostra)
            || !string.IsNullOrWhiteSpace(nrProposta);

        if (!temFiltro)
            return View(new List<HistAmostra>());

        var query = _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.Produto)
            .Include(a => a.AmostraStatus)
            .Include(a => a.Saldo)
            .Include(a => a.Proposta)
            .AsQueryable();

        if (idCliente > 0)
            query = query.Where(a => a.IdEntidade == idCliente);
        if (idAnaliseTipo > 0)
            query = query.Where(a => a.IdAnaliseTipo == idAnaliseTipo);
        if (idAmostraTipo > 0)
            query = query.Where(a => a.IdAmostraTipo == idAmostraTipo);
        if (anoAmostra > 0)
            query = query.Where(a => a.AnoAmostra == anoAmostra);
        if (!string.IsNullOrWhiteSpace(nrAmostra) && int.TryParse(nrAmostra, out var codA))
            query = query.Where(a => a.CodAmostra == codA);
        if (!string.IsNullOrWhiteSpace(nrProposta) && int.TryParse(nrProposta, out var codP))
            query = query.Where(a => a.Proposta != null && a.Proposta.CodProposta == codP);

        return View(await query.OrderByDescending(a => a.AnoAmostra).ThenByDescending(a => a.CodAmostra).ToListAsync());
    }

    // ─── Movimentar: detalhes da amostra + histórico + formulário ────────────
    public async Task<IActionResult> Movimentar(int id)
    {
        var amostra = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.Produto)
            .Include(a => a.AmostraStatus)
            .Include(a => a.Saldo)
            .Include(a => a.Testes).ThenInclude(t => t.ParametroAnalise)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseTipo)
            .Include(a => a.Movimentacoes)
                .ThenInclude(m => m.Params).ThenInclude(p => p.ParametroAnalise)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (amostra == null) return NotFound();
        return View(amostra);
    }

    // ─── Salvar nova movimentação ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarMovimentacao(int idHistAmostra, decimal qtde,
        string entradaSaida, string? justificativa, DateTime dataMov, int[]? parametrosIds)
    {
        var empresaId    = HttpContext.Session.GetInt32("EmpresaId") ?? 1;
        var funcionarioId = HttpContext.Session.GetInt32("UsuarioId");

        var saldo = await _db.HistAmostrasaldos.FirstOrDefaultAsync(s => s.IdHistAmostra == idHistAmostra);
        var saldoAtual = saldo?.SaldoAtual ?? 0;

        if (entradaSaida == "S" && saldoAtual < qtde)
        {
            TempData["Erro"] = $"Saldo insuficiente. Saldo atual: {saldoAtual:N2}. Solicitado: {qtde:N2}.";
            return RedirectToAction(nameof(Movimentar), new { id = idHistAmostra });
        }

        var mov = new MovAmostra
        {
            IdHistAmostra     = idHistAmostra,
            IdEmpresa         = empresaId,
            IdFuncionario     = funcionarioId,
            DataMov           = dataMov,
            Qtde              = qtde,
            EntradaSaida      = entradaSaida,
            Justificativa     = justificativa?.Trim(),
            AmostraComplementar = entradaSaida == "E" ? "C" : "M"
        };
        _db.MovAmostras.Add(mov);
        await _db.SaveChangesAsync();

        // Parâmetros vinculados à movimentação
        if (parametrosIds?.Length > 0)
        {
            var testesPorParametro = await _db.HistAmostrasTestess
                .Where(t => t.IdHistAmostra == idHistAmostra && t.IdParametroAnalise != null)
                .ToListAsync();

            foreach (var idParam in parametrosIds)
            {
                var teste = testesPorParametro.FirstOrDefault(t => t.IdParametroAnalise == idParam);
                _db.MovAmostrasParam.Add(new MovAmostraParam
                {
                    IdEmpresa          = empresaId,
                    IdMovAmostra       = mov.Id,
                    IdParametroAnalise = idParam,
                    IdHistAmostraTeste = teste?.Id
                });
            }
            await _db.SaveChangesAsync();
        }

        // Atualiza saldo
        var delta = entradaSaida == "E" ? qtde : -qtde;
        if (saldo == null)
        {
            _db.HistAmostrasaldos.Add(new HistAmostraSaldo
            {
                IdHistAmostra   = idHistAmostra,
                IdEmpresa       = empresaId,
                SaldoAtual      = delta,
                DataAtualizacao = DateTime.Now
            });
        }
        else
        {
            saldo.SaldoAtual      += delta;
            saldo.DataAtualizacao  = DateTime.Now;
        }
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Movimentação de {(entradaSaida == "E" ? "entrada" : "saída")} registrada. Saldo: {(saldoAtual + delta):N2}.";
        return RedirectToAction(nameof(Movimentar), new { id = idHistAmostra });
    }

    // ─── Excluir movimentação ─────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirMovimentacao(int id)
    {
        var mov = await _db.MovAmostras.FindAsync(id);
        if (mov == null) return NotFound();

        var idHistAmostra = mov.IdHistAmostra;
        var delta = mov.EntradaSaida == "E" ? -mov.Qtde : mov.Qtde;

        _db.MovAmostras.Remove(mov);

        var saldo = await _db.HistAmostrasaldos.FirstOrDefaultAsync(s => s.IdHistAmostra == idHistAmostra);
        if (saldo != null)
        {
            saldo.SaldoAtual     += delta;
            saldo.DataAtualizacao = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Movimentação excluída e saldo recalculado.";
        return RedirectToAction(nameof(Movimentar), new { id = idHistAmostra });
    }
}
