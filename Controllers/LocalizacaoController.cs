using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T24 — Localização das amostras (<c>/Localizacao</c>, tarefa 7.2): consulta com um filtro só, arquivar em lote,
/// localização da amostra (arquivo e descarte; voltar para arquivada desfaz o descarte), exclusão e pesquisa (T29).
/// </summary>
public sealed class LocalizacaoController(LocalizacaoService localizacao) : Controller
{
    [HttpGet, Permissao("LOCALIZACAO.CONSULTAR")]
    public async Task<IActionResult> Index(LocalizacaoFiltroVM filtro) => View(await localizacao.ListarAsync(filtro));

    [HttpGet, Permissao("LOCALIZACAO.CONSULTAR")]
    public async Task<IActionResult> Amostra(int id) =>
        await localizacao.CarregarAsync(id) is { } vm ? View(vm) : NotFound();

    /// <summary>T29 para a T24: só amostras ainda sem localização.</summary>
    [HttpGet, Permissao("LOCALIZACAO.CONSULTAR")]
    public async Task<IActionResult> Pesquisa(AmostraFiltroVM filtro, bool inicial = false) =>
        PartialView("~/Views/Amostras/_Pesquisa.cshtml", await localizacao.PesquisarAmostrasAsync(filtro, inicial));

    [HttpPost, Permissao("LOCALIZACAO.INCLUIR")]
    public Task<IActionResult> Incluir(LocalizacaoVM vm)
    {
        vm.Id = 0; // quem só inclui não altera a existente
        return GravarAsync(vm, "Localização gravada.");
    }

    [HttpPost, Permissao("LOCALIZACAO.ALTERAR")]
    public async Task<IActionResult> Alterar(LocalizacaoVM vm)
    {
        if (vm.Id == 0) return BadRequest();
        return await GravarAsync(vm, "Localização gravada.");
    }

    [HttpPost, Permissao("LOCALIZACAO.INCLUIR")]
    public async Task<IActionResult> ArquivarLote([Bind(Prefix = "Lote")] LocalizacaoLoteVM vm, LocalizacaoFiltroVM filtro)
    {
        var r = await localizacao.ArquivarLoteAsync(vm);
        if (this.AplicarNoTempData(r))
            TempData[Mensagens.Sucesso] = $"{r.Valor.Arquivadas} amostra(s) arquivada(s)."
                + (r.Valor.JaLocalizadas.Count == 0 ? "" : $" Já tinham localização (não alteradas): {string.Join(", ", r.Valor.JaLocalizadas)}.");
        return RedirectToAction(nameof(Index), new { filtro.Cliente, filtro.Produto, filtro.Numero, filtro.Situacao, filtro.Caixa, filtro.Armario, filtro.Prateleira });
    }

    [HttpPost, Permissao("LOCALIZACAO.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id, int idAmostra)
    {
        this.AplicarNoTempData(await localizacao.ExcluirAsync(id), "Localização excluída.");
        return RedirectToAction(nameof(Amostra), new { id = idAmostra });
    }

    // recusa: a tela volta com o que foi digitado e a mensagem
    private async Task<IActionResult> GravarAsync(LocalizacaoVM vm, string sucesso)
    {
        var r = await localizacao.GravarAsync(vm);
        if (this.AplicarNoFormulario(r, sucesso)) return RedirectToAction(nameof(Amostra), new { id = vm.IdAmostra });
        return await localizacao.CarregarAsync(vm.IdAmostra, vm) is { } tela ? View(nameof(Amostra), tela) : NotFound();
    }
}
