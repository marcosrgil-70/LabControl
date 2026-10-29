using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T20 — Propostas (<c>/Propostas</c>, tarefas 4.2 e 6.1): consulta, inclusão e alteração sem revisão
/// (cabeçalho, endereços do resultado e da cobrança, produtos × análises, textos, contrato, totais), exclusão, revisão,
/// troca de produto e PDF (com a Autorização de Serviço).
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
        if (await propostas.CarregarAsync(id) is { } gravada)
        {
            (vm.NrProposta, vm.IdPedido, vm.NrPedido, vm.DtEnvioCliente, vm.Bloqueio) = (gravada.NrProposta, gravada.IdPedido, gravada.NrPedido, gravada.DtEnvioCliente, gravada.Bloqueio);
            (vm.Revisao, vm.MotivoRevisao, vm.UltimaRevisao, vm.Revisoes) = (gravada.Revisao, gravada.MotivoRevisao, gravada.UltimaRevisao, gravada.Revisoes);
        }
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

    /// <summary>Gerar revisão (P07): abre a revisão nova (… R1) em edição.</summary>
    [HttpPost, Permissao("PROPOSTAS.REVISAR")]
    public async Task<IActionResult> Revisao(int id, string? motivo)
    {
        var r = await propostas.GerarRevisaoAsync(id, motivo);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Editar), new { id });
        TempData[Mensagens.Sucesso] = $"Revisão criada: {r.Valor.NrProposta}. A anterior ficou somente leitura.";
        return RedirectToAction(nameof(Editar), new { id = r.Valor.Id });
    }

    /// <summary>Troca o produto de um item na proposta e nas amostras dela.</summary>
    [HttpPost, Permissao("PROPOSTAS.ALTERAR")]
    public async Task<IActionResult> TrocarProduto(int id, int idProdutoAtual, int? idProdutoNovo)
    {
        this.AplicarNoTempData(await propostas.TrocarProdutoAsync(id, idProdutoAtual, idProdutoNovo), "Produto trocado na proposta e nas amostras dela.");
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>PDF da proposta (com a Autorização de Serviço), aberto em nova aba.</summary>
    [HttpGet, Permissao("PROPOSTAS.IMPRIMIR")]
    public async Task<IActionResult> Imprimir(int id)
    {
        var r = await propostas.ImprimirAsync(id);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Index));
        Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = r.Valor.Nome }.ToString();
        return File(r.Valor.Conteudo, "application/pdf");
    }

    /// <summary>Contrato padrão da empresa (HTML) para a aba Contrato.</summary>
    [HttpGet, Permissao("PROPOSTAS.CONSULTAR")]
    public async Task<IActionResult> ContratoPadrao() => Json(new { html = await propostas.ContratoPadraoAsync() });

    /// <summary>"Importar arquivo" do contrato: .docx, .html ou .txt → HTML simples (JSON); erro → 422.</summary>
    [HttpPost, Permissao("PROPOSTAS.ALTERAR"), RequestSizeLimit(ImportadorTexto.TamanhoMaximo + 64 * 1024)]
    public IActionResult ImportarContrato(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0) return UnprocessableEntity(new { erro = "Escolha um arquivo." });
        if (arquivo.Length > ImportadorTexto.TamanhoMaximo) return UnprocessableEntity(new { erro = "Arquivo maior que 2 MB." });
        using var conteudo = arquivo.OpenReadStream();
        var r = ImportadorTexto.Importar(arquivo.FileName, conteudo);
        return r.Sucesso ? Json(new { html = r.Valor.Html, aviso = r.Valor.Aviso }) : UnprocessableEntity(new { erro = r.Mensagem });
    }
}
