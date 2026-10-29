using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T26 — Resultados (<c>/Resultados</c>, tarefa 5.2): consulta e fila de análises, lançamento por amostra (um cartão por
/// teste, "Lançar todos"), exclusão de parâmetro ou de todos, revisão (com o boletim) e desfazer.
/// </summary>
public sealed class ResultadosController(ResultadoService resultados) : Controller
{
    [HttpGet, Permissao("RESULTADOS.CONSULTAR")]
    public async Task<IActionResult> Index(ResultadoFiltroVM filtro)
    {
        if (Request.Query.Count == 0) filtro.APartirDe = await resultados.APartirDePadraoAsync();
        return View(await resultados.ListarAsync(filtro));
    }

    /// <summary>Amostra escolhida (pesquisa ou fila): abre o resultado vigente dela ou um novo.</summary>
    [HttpGet, Permissao("RESULTADOS.CONSULTAR")]
    public async Task<IActionResult> Amostra(int id)
    {
        var r = await resultados.ResultadoDaAmostraAsync(id);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Index));
        if (r.Valor is { } existente) return RedirectToAction(nameof(Editar), new { id = existente });
        return await resultados.NovoAsync(id) is { } vm ? View("Form", vm) : NotFound();
    }

    [HttpPost, Permissao("RESULTADOS.INCLUIR")]
    public async Task<IActionResult> Criar(ResultadoVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await resultados.GravarAsync(vm) is var r && this.AplicarNoFormulario(r, "Resultado gravado."))
            return RedirectToAction(nameof(Editar), new { id = r.Valor });
        await resultados.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (revisão antiga, boletim emitido ou sem RESULTADOS.ALTERAR: só consulta).</summary>
    [HttpGet, Permissao("RESULTADOS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await resultados.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("RESULTADOS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, ResultadoVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && await resultados.GravarAsync(vm) is var r && this.AplicarNoFormulario(r, "Resultado gravado."))
            return RedirectToAction(nameof(Editar), new { id });
        await resultados.PrepararAsync(vm);
        return View("Form", vm);
    }

    [HttpPost, Permissao("RESULTADOS.EXCLUIR")]
    public async Task<IActionResult> ExcluirParametro(int id, int idParam)
    {
        this.AplicarNoTempData(await resultados.ExcluirLinhaAsync(id, idParam), "Resultado do parâmetro excluído.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("RESULTADOS.EXCLUIR")]
    public async Task<IActionResult> ExcluirTodos(int id)
    {
        if (this.AplicarNoTempData(await resultados.ExcluirTodosAsync(id), "Resultados da amostra excluídos."))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>Gerar revisão (P15): abre a revisão nova em edição.</summary>
    [HttpPost, Permissao("RESULTADOS.REVISAR")]
    public async Task<IActionResult> Revisao(int id, string? motivo, bool revisarBoletim = false)
    {
        var r = await resultados.GerarRevisaoAsync(id, motivo, revisarBoletim);
        if (!this.AplicarNoTempData(r, "Revisão do resultado criada" + (revisarBoletim ? " (e do boletim, se havia)." : ".")))
            return RedirectToAction(nameof(Editar), new { id });
        return RedirectToAction(nameof(Editar), new { id = r.Valor });
    }

    /// <summary>Desfazer a última revisão (sem boletim): volta para a anterior.</summary>
    [HttpPost, Permissao("RESULTADOS.DESFAZER_REVISAO")]
    public async Task<IActionResult> DesfazerRevisao(int id)
    {
        var r = await resultados.DesfazerRevisaoAsync(id);
        if (!this.AplicarNoTempData(r, "Revisão desfeita: a anterior voltou a ser a vigente.")) return RedirectToAction(nameof(Editar), new { id });
        return RedirectToAction(nameof(Editar), new { id = r.Valor });
    }

    /// <summary>T29 para a T26: só amostras que podem receber resultado (última revisão, status que permite, período padrão da empresa).</summary>
    [HttpGet, Permissao("RESULTADOS.CONSULTAR")]
    public async Task<IActionResult> Pesquisa(AmostraFiltroVM filtro, bool inicial = false) =>
        PartialView("~/Views/Amostras/_Pesquisa.cshtml", await resultados.PesquisarAmostrasAsync(filtro, inicial));
}
