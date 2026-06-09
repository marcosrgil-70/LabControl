using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class ResultadosController : Controller
{
    private readonly ApplicationDbContext _db;
    public ResultadosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var resultados = await _db.ResultadosAnalises
            .Include(r => r.HistAmostra).ThenInclude(a => a.AmostraTipo)
            .Include(r => r.HistAmostra).ThenInclude(a => a.Entidade)
            .Include(r => r.Parametros)
            .OrderByDescending(r => r.DtResultado)
            .ToListAsync();
        return View(resultados);
    }

    public async Task<IActionResult> Criar(int idAmostra)
    {
        var amostra = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseTipo)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseMetodo)
            .Include(a => a.Testes).ThenInclude(t => t.ParametroAnalise)
            .FirstOrDefaultAsync(a => a.Id == idAmostra);

        if (amostra == null) return NotFound();

        ViewBag.Amostra = amostra;
        ViewBag.Unidades = await _db.Unidades.OrderBy(u => u.Sigla).ToListAsync();

        var modelo = new ResultadoAnalise
        {
            IdHistAmostra = idAmostra,
            IdEmpresa = 1,
            DtResultado = DateTime.Now
        };
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ResultadoAnalise model,
        List<int?> testeIds, List<string?> tiposResultado,
        List<string?> valores, List<bool?> satisfeitos,
        List<int?> unidadeIds, List<string?> simbolos)
    {
        model.IdEmpresa = 1;
        model.DtResultado = DateTime.Now;

        // Verificar próxima revisão
        var ultimaRev = await _db.ResultadosAnalises
            .Where(r => r.IdHistAmostra == model.IdHistAmostra)
            .MaxAsync(r => (int?)r.Revisao) ?? -1;
        model.Revisao = ultimaRev + 1;

        _db.ResultadosAnalises.Add(model);
        await _db.SaveChangesAsync();

        // Salvar parâmetros
        for (int i = 0; i < testeIds.Count; i++)
        {
            var param = new ResultadoParam
            {
                IdResultadoAnalise = model.Id,
                IdHistAmostraTeste = testeIds[i],
                TipoResultado = tiposResultado.ElementAtOrDefault(i) ?? "N",
                VrResultado = valores.ElementAtOrDefault(i),
                VrSatisfeito = satisfeitos.ElementAtOrDefault(i),
                IdUnidade = unidadeIds.ElementAtOrDefault(i),
                SimboloGrandeza = simbolos.ElementAtOrDefault(i),
                DtResultado = DateTime.Now
            };
            _db.ResultadosParam.Add(param);
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = "Resultado lançado com sucesso!";
        return RedirectToAction("Detalhes", "HistAmostras", new { id = model.IdHistAmostra });
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var resultado = await _db.ResultadosAnalises
            .Include(r => r.HistAmostra).ThenInclude(a => a.AmostraTipo)
            .Include(r => r.HistAmostra).ThenInclude(a => a.Entidade)
            .Include(r => r.Parametros).ThenInclude(p => p.Unidade)
            .Include(r => r.Parametros).ThenInclude(p => p.HistAmostraTeste)
                .ThenInclude(t => t!.AnaliseTipo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resultado == null) return NotFound();
        return View(resultado);
    }
}
