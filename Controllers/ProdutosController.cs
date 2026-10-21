using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T14 — Produtos (<c>/Produtos</c>): consulta por código/descrição, inclusão com código automático, alteração,
/// inativar/reativar e exclusão sem uso; cadastro rápido em JSON para o "+" da proposta (4.2).
/// </summary>
public sealed class ProdutosController(ProdutoService produtos) : Controller
{
    [HttpGet, Permissao("PRODUTOS.CONSULTAR")]
    public async Task<IActionResult> Index(ProdutoFiltroVM filtro) => View(await produtos.ListarAsync(filtro));

    [HttpGet, Permissao("PRODUTOS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await produtos.NovoAsync());

    [HttpPost, Permissao("PRODUTOS.INCLUIR")]
    public async Task<IActionResult> Criar(ProdutoVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await produtos.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return await VoltarParaListaAsync(r.Valor);
        await produtos.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (sem PRODUTOS.ALTERAR a tela fica só de consulta).</summary>
    [HttpGet, Permissao("PRODUTOS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await produtos.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("PRODUTOS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, ProdutoVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await produtos.GravarAsync(vm)))
            return await VoltarParaListaAsync(id);
        await produtos.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Depois de gravar volta para a lista (pedido do usuário): de lá já se inclui o próximo. A mensagem traz o código gerado.</summary>
    private async Task<IActionResult> VoltarParaListaAsync(int id)
    {
        var gravado = await produtos.CarregarAsync(id);
        TempData[Mensagens.Sucesso] = $"Produto {gravado?.Codigo} — {gravado?.Descricao} gravado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Permissao("PRODUTOS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        this.AplicarNoTempData(await produtos.ExcluirAsync(id), "Produto excluído.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Inativar (ex.: em uso, não pode ser excluído) ou reativar.</summary>
    [HttpPost, Permissao("PRODUTOS.ALTERAR")]
    public async Task<IActionResult> Inativo(int id, bool inativo)
    {
        this.AplicarNoTempData(await produtos.DefinirInativoAsync(id, inativo),
            inativo ? "Produto inativado: não aparece mais na lista nem nos combos (marque “Mostrar inativos” para vê-lo e reativá-lo)." : "Produto reativado.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Cadastro rápido "+" (proposta): <c>{ id, texto }</c>; erro → 422 <c>{ erro, campo }</c>.</summary>
    [HttpPost, Permissao("PRODUTOS.INCLUIR")]
    public async Task<IActionResult> Rapido(ProdutoRapidoVM vm)
    {
        var r = await produtos.IncluirRapidoAsync(vm);
        return r.Sucesso ? Json(new { id = r.Valor.Id, texto = r.Valor.Nome }) : UnprocessableEntity(new { erro = r.Mensagem, campo = r.Campo });
    }
}
