using LabControl.Infra;
using LabControl.Services;
using LabControl.Services.Email;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T27 — Boletim (<c>/Boletins</c>, tarefa 5.3): consulta, montagem (seleção em dois grids, ordem, textos), prévia,
/// emissão com PDF + SHA-256, reimpressão do arquivo gravado, revisão e desfazer.
/// </summary>
public sealed class BoletinsController(BoletimService boletins, EnvioBoletimService envio) : Controller
{
    [HttpGet, Permissao("BOLETINS.CONSULTAR")]
    public async Task<IActionResult> Index(BoletimFiltroVM filtro)
    {
        if (Request.Query.Count == 0) filtro.APartirDe = await boletins.APartirDePadraoAsync();
        return View(await boletins.ListarAsync(filtro));
    }

    /// <summary>Amostra escolhida na pesquisa: abre o boletim vigente dela ou um novo.</summary>
    [HttpGet, Permissao("BOLETINS.CONSULTAR")]
    public async Task<IActionResult> Amostra(int id)
    {
        var r = await boletins.BoletimDaAmostraAsync(id);
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Index));
        if (r.Valor is { } existente) return RedirectToAction(nameof(Editar), new { id = existente });
        return await boletins.NovoAsync(id) is { } vm ? View("Form", vm) : NotFound();
    }

    [HttpPost, Permissao("BOLETINS.INCLUIR")]
    public async Task<IActionResult> Criar(BoletimVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await boletins.GravarAsync(vm) is var r && this.AplicarNoFormulario(r, "Boletim gravado."))
            return RedirectToAction(nameof(Editar), new { id = r.Valor });
        await boletins.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (emitido, revisão antiga ou sem BOLETINS.ALTERAR: só consulta).</summary>
    [HttpGet, Permissao("BOLETINS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id) =>
        await boletins.CarregarAsync(id) is { } vm ? View("Form", vm) : NotFound();

    [HttpPost, Permissao("BOLETINS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, BoletimVM vm)
    {
        vm.Id = id;
        vm.Selecionados = []; // depois de criado, a seleção é a ação "Concluir seleção"
        if (ModelState.IsValid && await boletins.GravarAsync(vm) is var r && this.AplicarNoFormulario(r, "Boletim gravado."))
            return RedirectToAction(nameof(Editar), new { id });
        await boletins.PrepararAsync(vm);
        return View("Form", vm);
    }

    /// <summary>"Concluir seleção": os parâmetros marcados no grid da esquerda entram no boletim.</summary>
    [HttpPost, Permissao("BOLETINS.ALTERAR")]
    public async Task<IActionResult> IncluirParametros(int id, List<int> selecionados) =>
        Voltar(id, await boletins.IncluirParametrosAsync(id, selecionados), "Parâmetros incluídos no boletim.");

    [HttpPost, Permissao("BOLETINS.ALTERAR")]
    public async Task<IActionResult> ExcluirParametro(int id, int idParametro) =>
        Voltar(id, await boletins.ExcluirParametroAsync(id, idParametro), "Parâmetro retirado do boletim.");

    [HttpPost, Permissao("BOLETINS.ALTERAR")]
    public async Task<IActionResult> Mover(int id, int idParametro, int direcao) =>
        Voltar(id, await boletins.MoverAsync(id, idParametro, direcao), null);

    [HttpPost, Permissao("BOLETINS.ALTERAR")]
    public async Task<IActionResult> AtualizarTextos(int id) =>
        Voltar(id, await boletins.AtualizarTextosAsync(id), "Textos dos parâmetros atualizados a partir dos resultados.");

    [HttpPost, Permissao("BOLETINS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (this.AplicarNoTempData(await boletins.ExcluirAsync(id), "Boletim excluído.")) return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Editar), new { id });
    }

    /// <summary>Prévia em PDF (marca d'água, sem assinaturas), aberta numa nova aba.</summary>
    [HttpGet, Permissao("BOLETINS.CONSULTAR")]
    public async Task<IActionResult> Visualizar(int id) => Pdf(id, await boletins.PreviaAsync(id));

    [HttpPost, Permissao("BOLETINS.EMITIR")]
    public async Task<IActionResult> Emitir(int id) =>
        Voltar(id, await boletins.EmitirAsync(id), "Boletim emitido: o PDF foi gravado e o boletim não pode mais ser alterado.");

    /// <summary>Reimpressão do emitido (o PDF gravado, conferido pelo hash) ou 2ª via do migrado.</summary>
    [HttpGet, Permissao("BOLETINS.IMPRIMIR")]
    public async Task<IActionResult> Imprimir(int id) => Pdf(id, await boletins.ImprimirAsync(id));

    /// <summary>Enviar o boletim emitido por e-mail (8.3): destinatários sugeridos, assunto e texto do modelo da empresa.</summary>
    [HttpGet, Permissao("BOLETINS.IMPRIMIR")]
    public async Task<IActionResult> Email(int id)
    {
        var r = await envio.PrepararAsync(id);
        return this.AplicarNoTempData(r) ? View(r.Valor) : RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("BOLETINS.IMPRIMIR")]
    public async Task<IActionResult> Email(int id, EnvioBoletimVM vm)
    {
        vm.IdBoletim = id;
        var r = await envio.EnviarAsync(vm);
        if (this.AplicarNoFormulario(r, $"Boletim enviado por e-mail para {vm.Para?.Trim()}."))
            return RedirectToAction(nameof(Editar), new { id });
        if (vm.CodBoletim.Length > 0) return View(vm);
        // sem dados (boletim não encontrado ou não emitido): o ModelState se perde no redirect, a mensagem vai pelo TempData
        TempData[Mensagens.Erro] = r.Mensagem;
        return RedirectToAction(nameof(Editar), new { id });
    }

    [HttpPost, Permissao("BOLETINS.REVISAR")]
    public async Task<IActionResult> Revisao(int id, string? motivo)
    {
        var r = await boletins.GerarRevisaoAsync(id, motivo);
        if (!this.AplicarNoTempData(r, "Revisão do boletim criada (não emitida).")) return RedirectToAction(nameof(Editar), new { id });
        return RedirectToAction(nameof(Editar), new { id = r.Valor });
    }

    [HttpPost, Permissao("BOLETINS.DESFAZER_REVISAO")]
    public async Task<IActionResult> DesfazerRevisao(int id)
    {
        var r = await boletins.DesfazerRevisaoAsync(id);
        if (!this.AplicarNoTempData(r, "Revisão desfeita: a anterior voltou a ser a vigente.")) return RedirectToAction(nameof(Editar), new { id });
        return RedirectToAction(nameof(Editar), new { id = r.Valor });
    }

    /// <summary>T29 para a T27: só amostras que podem ter boletim (última revisão, com resultado, 400 dias).</summary>
    [HttpGet, Permissao("BOLETINS.CONSULTAR")]
    public async Task<IActionResult> Pesquisa(AmostraFiltroVM filtro, bool inicial = false) =>
        PartialView("~/Views/Amostras/_Pesquisa.cshtml", await boletins.PesquisarAmostrasAsync(filtro, inicial));

    private IActionResult Voltar(int id, IResultado r, string? sucesso)
    {
        this.AplicarNoTempData(r, sucesso);
        return RedirectToAction(nameof(Editar), new { id });
    }

    // PDF inline (o navegador mostra; imprimir/salvar por ele); erro volta para o boletim com a mensagem
    private IActionResult Pdf(int id, Resultado<ArquivoPdf> r)
    {
        if (!this.AplicarNoTempData(r)) return RedirectToAction(nameof(Editar), new { id });
        Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = r.Valor.Nome }.ToString();
        return File(r.Valor.Conteudo, "application/pdf");
    }
}
