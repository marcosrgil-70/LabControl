using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T20 — Propostas, parte essencial (<c>/Propostas</c>, tarefa 4.2): consulta, inclusão e alteração sem revisão
/// (cabeçalho, endereços do resultado e da cobrança, produtos × análises, textos, totais) e exclusão. Revisão, seleção
/// múltipla, contrato e PDF na 6.1.
/// </summary>
public sealed class PropostasController(PropostaService propostas) : Controller
{
    [HttpGet, Permissao("PROPOSTAS.CONSULTAR")]
    public async Task<IActionResult> Index(PropostaFiltroVM filtro) => View(await propostas.ListarAsync(filtro));

    [HttpGet, Permissao("PROPOSTAS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await propostas.NovaAsync());

    [HttpPost, Permissao("PROPOSTAS.INCLUIR")]
    public async Task<IActionResult> Criar(PropostaVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await propostas.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return await GravadaAsync(r.Valor);
        await propostas.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (sem PROPOSTAS.ALTERAR, ou com a proposta bloqueada, fica só de consulta).</summary>
    [HttpGet, Permissao("PROPOSTAS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await propostas.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("PROPOSTAS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, PropostaVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && await propostas.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return await GravadaAsync(id);
        // número/pedido/bloqueio vêm do banco, não do formulário
        if (await propostas.CarregarAsync(id) is { } gravada) (vm.NrProposta, vm.NrPedido, vm.DtEnvioCliente, vm.Bloqueio) = (gravada.NrProposta, gravada.NrPedido, gravada.DtEnvioCliente, gravada.Bloqueio);
        await propostas.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Depois de gravar abre a proposta gravada, com o número devolvido pelo banco (mensagem do legado).</summary>
    private async Task<IActionResult> GravadaAsync(int id)
    {
        var gravada = await propostas.CarregarAsync(id);
        TempData[Mensagens.Sucesso] = $"Nr. da Proposta: {gravada?.NrProposta}";
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("PROPOSTAS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (this.AplicarNoTempData(await propostas.ExcluirAsync(id), "Proposta excluída."))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>Contatos, endereços e sugestões do cliente ou da empresa de cobrança (JSON para o <c>propostas.js</c>).</summary>
    [HttpGet, Permissao("PROPOSTAS.CONSULTAR")]
    public async Task<IActionResult> Entidade(int id) =>
        await propostas.EntidadeAsync(id) is { } e ? Json(e) : NotFound();
}
