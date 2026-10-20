using LabControl.Infra;
using LabControl.Services.Entidades;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T13 — Profissionais / funcionários (<c>/Profissionais</c>): dados do profissional (cargo, registro, assina boletim,
/// executa análise, assinatura), vínculo com o usuário do sistema e as abas de endereços/telefones/e-mails (P25).
/// </summary>
public sealed class ProfissionaisController(ProfissionalService profissionais, ContatosService contatos, IEmpresaContexto ctx) : Controller
{
    [HttpGet, Permissao("PROFISSIONAIS.CONSULTAR")]
    public async Task<IActionResult> Index(ProfissionalFiltroVM filtro) => View(await profissionais.ListarAsync(filtro));

    [HttpGet, Permissao("PROFISSIONAIS.INCLUIR")]
    public async Task<IActionResult> Criar() => View("Form", await profissionais.NovoAsync());

    [HttpPost, Permissao("PROFISSIONAIS.INCLUIR")]
    public async Task<IActionResult> Criar(ProfissionalVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await profissionais.GravarAsync(vm) is var r && this.AplicarNoFormulario(r,
                "Profissional gravado. Agora inclua os endereços, telefones e e-mails."))
            return Redirect(Url.Action(nameof(Editar), new { id = r.Valor }) + "#aba-enderecos");
        await profissionais.PrepararAsync(vm);
        return View("Form", vm);
    }

    [HttpGet, Permissao("PROFISSIONAIS.CONSULTAR")]
    public async Task<IActionResult> Editar(int id)
    {
        if (await profissionais.CarregarAsync(id) is not { } vm) return NotFound();
        await ContatosNaTelaAsync(id);
        return View("Form", vm);
    }

    [HttpPost, Permissao("PROFISSIONAIS.ALTERAR")]
    public async Task<IActionResult> Editar(int id, ProfissionalVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await profissionais.GravarAsync(vm), "Profissional gravado."))
            return RedirectToAction(nameof(Editar), new { id });
        await profissionais.PrepararAsync(vm);
        await ContatosNaTelaAsync(id);
        return View("Form", vm);
    }

    [HttpPost, Permissao("PROFISSIONAIS.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        this.AplicarNoTempData(await profissionais.ExcluirAsync(id), "Profissional excluído.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Imagem da assinatura (prévia na tela; o PDF do boletim lê do banco).</summary>
    [HttpGet, Permissao("PROFISSIONAIS.CONSULTAR"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Assinatura(int id) =>
        await profissionais.AssinaturaAsync(id) is { } png ? File(png, "image/png") : NotFound();

    // ---- contatos (P25)

    [HttpGet("Profissionais/{idEntidade:int}/Contatos"), Permissao("PROFISSIONAIS.CONSULTAR")]
    public Task<IActionResult> Contatos(int idEntidade) =>
        this.AbasAsync(contatos, CadastroDono.Profissional, idEntidade, Rota(idEntidade), "PROFISSIONAIS.ALTERAR", ctx);

    [HttpPost("Profissionais/{idEntidade:int}/ContatoEndereco"), Permissao("PROFISSIONAIS.ALTERAR")]
    public Task<IActionResult> ContatoEndereco(int idEntidade, EnderecoVM vm) =>
        this.RespostaAsync(contatos.GravarEnderecoAsync(CadastroDono.Profissional, idEntidade, vm));

    [HttpPost("Profissionais/{idEntidade:int}/ContatoFone"), Permissao("PROFISSIONAIS.ALTERAR")]
    public Task<IActionResult> ContatoFone(int idEntidade, FoneVM vm) =>
        this.RespostaAsync(contatos.GravarFoneAsync(CadastroDono.Profissional, idEntidade, vm));

    [HttpPost("Profissionais/{idEntidade:int}/ContatoEmail"), Permissao("PROFISSIONAIS.ALTERAR")]
    public Task<IActionResult> ContatoEmail(int idEntidade, EmailVM vm) =>
        this.RespostaAsync(contatos.GravarEmailAsync(CadastroDono.Profissional, idEntidade, vm));

    [HttpPost("Profissionais/{idEntidade:int}/ContatoExcluir"), Permissao("PROFISSIONAIS.ALTERAR")]
    public Task<IActionResult> ContatoExcluir(int idEntidade, TipoContato tipo, int idContato) =>
        this.RespostaAsync(contatos.ExcluirAsync(CadastroDono.Profissional, idEntidade, tipo, idContato));

    private static string Rota(int id) => $"/Profissionais/{id}";

    private async Task ContatosNaTelaAsync(int id) =>
        ViewData["Contatos"] = await contatos.CarregarAsync(CadastroDono.Profissional, id, Rota(id), ctx.TemPermissao("PROFISSIONAIS.ALTERAR"));
}
