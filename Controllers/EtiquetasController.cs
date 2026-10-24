using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T25 — Etiquetas de amostras (<c>/Etiquetas</c>, tarefa 4.5): filtra amostras liberadas, marca e ajusta a quantidade
/// de cada uma e imprime direto (PPLB em RAW) na impressora escolhida entre as instaladas no servidor.
/// </summary>
public sealed class EtiquetasController(EtiquetaService etiquetas) : Controller
{
    [HttpGet, Permissao("ETIQUETAS.IMPRIMIR")]
    public async Task<IActionResult> Index(EtiquetaFiltroVM filtro)
    {
        // 1ª abertura sem amostra indicada: entregas dos últimos 7 dias (padrão do legado)
        if (Request.Query.Count == 0) filtro.APartirDe = await etiquetas.APartirDePadraoAsync();
        return View(await etiquetas.ListarAsync(filtro));
    }

    [HttpPost, Permissao("ETIQUETAS.IMPRIMIR")]
    public async Task<IActionResult> Imprimir(EtiquetaImprimirVM vm, EtiquetaFiltroVM filtro)
    {
        var r = await etiquetas.ImprimirAsync(vm);
        this.AplicarNoTempData(r, r.Sucesso ? $"{r.Valor} etiqueta(s) enviada(s) para “{vm.Impressora}”." : null);
        return RedirectToAction(nameof(Index), new { filtro.Cliente, filtro.Numero, APartirDe = filtro.APartirDe?.ToString("yyyy-MM-dd"), filtro.SoUltima, filtro.Amostra });
    }
}
