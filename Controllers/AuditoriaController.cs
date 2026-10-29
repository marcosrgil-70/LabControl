using LabControl.Data;
using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T06 — Auditoria (<c>/Auditoria</c>, tarefa 8.4): eventos da <c>AUDITORIA</c> com filtros e paginação, "Ver diferença"
/// campo a campo, CSV do filtro e a aba "Histórico da amostra" (<c>LAB_HIST_AMOSTRAS_LOG</c>). Somente leitura.
/// </summary>
public sealed class AuditoriaController(AuditoriaService auditoria, LabDbContext db) : Controller
{
    [HttpGet, Permissao("AUDITORIA.CONSULTAR")]
    public async Task<IActionResult> Index(AuditoriaFiltroVM filtro) => View(await auditoria.ListarAsync(filtro));

    [HttpGet, Permissao("AUDITORIA.CONSULTAR")]
    public async Task<IActionResult> Detalhe(long id) =>
        await auditoria.DetalheAsync(id) is { } vm ? View(vm) : NotFound();

    [HttpGet, Permissao("AUDITORIA.IMPRIMIR")]
    public async Task<IActionResult> Csv(AuditoriaFiltroVM filtro) =>
        File(await auditoria.CsvAsync(filtro), "text/csv", $"auditoria-{await db.AgoraAsync():yyyyMMdd-HHmm}.csv");

    /// <summary>Aba "Histórico da amostra": <c>/Auditoria/Amostra?numero=04-00001-01/26</c>.</summary>
    [HttpGet, Permissao("AUDITORIA.CONSULTAR")]
    public async Task<IActionResult> Amostra(string? numero) => View(await auditoria.HistoricoAmostraAsync(numero));
}
