using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T22 — Recebimento de amostras (<c>/Amostras</c>, tarefa 4.3): consulta, inclusão (número pelo banco), alteração,
/// exclusão, revisão e comprovante de recebimento (página de impressão — decisão do usuário). Cascata
/// cliente → proposta → item pelos JSON <c>Cliente/{id}</c> e <c>Proposta/{id}</c>.
/// </summary>
public sealed class AmostrasController(AmostraService amostras) : Controller
{
    [HttpGet, Permissao("AMOSTRAS.CONSULTAR")]
    public async Task<IActionResult> Index(AmostraFiltroVM filtro)
    {
        // 1ª abertura: "exibir a partir de" = hoje − DIAS_PESQUISA_PADRAO
        if (Request.Query.Count == 0) filtro.APartirDe = await amostras.APartirDePadraoAsync();
        return View(await amostras.ListarAsync(filtro));
    }

    [HttpGet, Permissao("AMOSTRAS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await amostras.NovaAsync());

    [HttpPost, Permissao("AMOSTRAS.INCLUIR")]
    public async Task<IActionResult> Criar(AmostraVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await amostras.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return await GravadaAsync(r.Valor);
        await amostras.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (sem AMOSTRAS.ALTERAR, revisão antiga ou boletim emitido: só consulta).</summary>
    [HttpGet, Permissao("AMOSTRAS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await amostras.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("AMOSTRAS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, AmostraVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && await amostras.GravarAsync(vm) is var r && this.AplicarNoFormulario(r))
            return await GravadaAsync(r.Valor);
        // número, revisão, testes e histórico vêm do banco, não do formulário
        if (await amostras.CarregarAsync(id) is { } gravada)
        {
            (vm.NrAmostra, vm.Revisao, vm.MotivoRevisao, vm.NrPedido, vm.Bloqueio, vm.TemResultado, vm.UltimaRevisao) =
                (gravada.NrAmostra, gravada.Revisao, gravada.MotivoRevisao, gravada.NrPedido, gravada.Bloqueio, gravada.TemResultado, gravada.UltimaRevisao);
            (vm.Testes, vm.Historico) = (gravada.Testes, gravada.Historico);
        }
        await amostras.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Depois de gravar abre a amostra com o número dado pelo banco (mensagem do legado) e o aviso, se houver.</summary>
    private async Task<IActionResult> GravadaAsync(AmostraGravada g)
    {
        var gravada = await amostras.CarregarAsync(g.Id);
        TempData[Mensagens.Sucesso] = $"Código da Amostra: {gravada?.NrAmostra}"
            + (gravada?.TemResultado == true ? $" — {AmostraService.MensagemComResultados}" : "")
            + (g.Aviso is null ? "" : $" — {g.Aviso}");
        return RedirectToAction(nameof(Editar), new { id = g.Id });
    }

    [HttpPost, Permissao("AMOSTRAS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (this.AplicarNoTempData(await amostras.ExcluirAsync(id), "Amostra excluída."))
            return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>Gerar revisão (P11): abre a revisão nova (… R01) em edição.</summary>
    [HttpPost, Permissao("AMOSTRAS.REVISAR")]
    public async Task<IActionResult> Revisao(int id, string? motivo)
    {
        var r = await amostras.GerarRevisaoAsync(id, motivo);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Editar), new { id });
        var nova = await amostras.CarregarAsync(r.Valor);
        TempData[Mensagens.Sucesso] = $"Revisão criada: {nova?.NrAmostra} (resultado e boletim também foram revisados, se existiam).";
        return RedirectToAction(nameof(Editar), new { id = r.Valor });
    }

    /// <summary>Comprovante de recebimento: página de impressão (o navegador imprime ou salva em PDF).</summary>
    [HttpGet, Permissao("AMOSTRAS.IMPRIMIR")]
    public async Task<IActionResult> Comprovante(int id) =>
        await amostras.ComprovanteAsync(id) is { } c ? View(c) : NotFound();

    /// <summary>
    /// T29 — pesquisa de amostras (componente modal, <c>pesquisa-amostras.js</c>): devolve só a partial com filtros e grid;
    /// a seleção devolve <c>{ id, nrAmostra }</c> à tela que abriu. Sempre na empresa ativa (filtro global). Permissão de
    /// consulta da tela que usa o componente (4.4: T22; T24/T26/T27 acrescentam as suas quando o adotarem).
    /// </summary>
    [HttpGet, Permissao("AMOSTRAS.CONSULTAR")]
    public async Task<IActionResult> Pesquisa(AmostraFiltroVM filtro, bool inicial = false)
    {
        if (inicial) filtro.APartirDe = await amostras.APartirDePadraoAsync();
        filtro.OrdenarPorNumero = true;
        return PartialView("_Pesquisa", await amostras.ListarAsync(filtro));
    }

    /// <summary>Documento, endereço, fone e propostas que podem receber amostra (JSON).</summary>
    [HttpGet, Permissao("AMOSTRAS.CONSULTAR")]
    public async Task<IActionResult> Cliente(int id) => await amostras.ClienteAsync(id) is { } c ? Json(c) : NotFound();

    /// <summary>Pedido e itens da proposta com os tipos de análise de cada item (JSON).</summary>
    [HttpGet, Permissao("AMOSTRAS.CONSULTAR")]
    public async Task<IActionResult> Proposta(int id) => await amostras.PropostaAsync(id) is { } p ? Json(p) : NotFound();
}
