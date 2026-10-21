using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T15 — Parâmetros de análise (<c>/ParametrosAnalise</c>): consulta por descrição, tipo e acreditado; inclusão e
/// alteração (voltam para a lista, como na T14); inativar/reativar e exclusão sem uso.
/// </summary>
public sealed class ParametrosAnaliseController(ParametroAnaliseService parametros) : Controller
{
    [HttpGet, Permissao("PARAMETROS_ANALISE.CONSULTAR")]
    public async Task<IActionResult> Index(ParametroAnaliseFiltroVM filtro) => View(await parametros.ListarAsync(filtro));

    [HttpGet, Permissao("PARAMETROS_ANALISE.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await parametros.NovoAsync());

    [HttpPost, Permissao("PARAMETROS_ANALISE.INCLUIR")]
    public async Task<IActionResult> Criar(ParametroAnaliseVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await parametros.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return VoltarParaLista(vm);
        await parametros.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (sem PARAMETROS_ANALISE.ALTERAR a tela fica só de consulta).</summary>
    [HttpGet, Permissao("PARAMETROS_ANALISE.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await parametros.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("PARAMETROS_ANALISE.ALTERAR")]
    public async Task<IActionResult> Editar(int id, ParametroAnaliseVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await parametros.GravarAsync(vm)))
            return VoltarParaLista(vm);
        await parametros.PrepararAsync(vm);
        return View("Form", vm);
    }

    [HttpPost, Permissao("PARAMETROS_ANALISE.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        this.AplicarNoTempData(await parametros.ExcluirAsync(id), "Parâmetro de análise excluído.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Inativar (ex.: em uso, não pode ser excluído) ou reativar.</summary>
    [HttpPost, Permissao("PARAMETROS_ANALISE.ALTERAR")]
    public async Task<IActionResult> Inativo(int id, bool inativo)
    {
        this.AplicarNoTempData(await parametros.DefinirInativoAsync(id, inativo),
            inativo ? "Parâmetro inativado: não aparece mais na lista nem na proposta (marque “Mostrar inativos” para vê-lo e reativá-lo)." : "Parâmetro reativado.");
        return RedirectToAction(nameof(Index));
    }

    // depois de gravar volta para a lista, pronta para incluir o próximo (mesmo fluxo da T14, pedido do usuário)
    private RedirectToActionResult VoltarParaLista(ParametroAnaliseVM vm)
    {
        TempData[Mensagens.Sucesso] = $"Parâmetro {Textos.Maiusculas(vm.Descricao)} gravado.";
        return RedirectToAction(nameof(Index));
    }
}
