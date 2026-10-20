using LabControl.Infra;
using LabControl.Services.Entidades;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T12 — Clientes / Fornecedores (<c>/Clientes</c>): consulta, dados básicos PF/PJ, observações e as abas de
/// endereços/telefones/e-mails (componente de contatos, P25). Os contatos são gravados na hora (AJAX), por isso
/// só existem depois de gravar os dados básicos.
/// </summary>
public sealed class ClientesController(ClienteService clientes, ContatosService contatos, IEmpresaContexto ctx) : Controller
{
    [HttpGet, Permissao("CLIENTES.CONSULTAR")]
    public async Task<IActionResult> Index(ClienteFiltroVM filtro) => View(await clientes.ListarAsync(filtro));

    [HttpGet, Permissao("CLIENTES.INCLUIR")]
    public IActionResult Criar() => View("Form", new ClienteVM { DataCadastro = DateOnly.FromDateTime(DateTime.Today) });

    [HttpPost, Permissao("CLIENTES.INCLUIR")]
    public async Task<IActionResult> Criar(ClienteVM vm)
    {
        vm.Id = 0;
        if (ModelState.IsValid && await clientes.GravarAsync(vm) is var r && this.AplicarNoFormulario(r,
                "Cadastro gravado. Agora inclua os endereços, telefones e e-mails."))
            return Redirect(Url.Action(nameof(Editar), new { id = r.Valor }) + "#aba-enderecos");
        return View("Form", vm);
    }

    /// <summary>Ver/alterar (sem CLIENTES.ALTERAR a tela fica só de consulta).</summary>
    [HttpGet, Permissao("CLIENTES.CONSULTAR")]
    public async Task<IActionResult> Editar(int id)
    {
        if (await clientes.CarregarAsync(id) is not { } vm) return NotFound();
        await ContatosNaTelaAsync(id);
        return View("Form", vm);
    }

    [HttpPost, Permissao("CLIENTES.ALTERAR")]
    public async Task<IActionResult> Editar(int id, ClienteVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid && this.AplicarNoFormulario(await clientes.GravarAsync(vm), "Cadastro gravado."))
            return RedirectToAction(nameof(Editar), new { id });
        if (await clientes.CarregarAsync(id) is { } gravado) (vm.TipoFuncionario, vm.DataCadastro) = (gravado.TipoFuncionario, gravado.DataCadastro);
        await ContatosNaTelaAsync(id);
        return View("Form", vm);
    }

    [HttpPost, Permissao("CLIENTES.EXCLUIR")]
    public async Task<IActionResult> Excluir(int id)
    {
        this.AplicarNoTempData(await clientes.ExcluirAsync(id), "Cadastro excluído.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Inativar (ex.: com movimento, não pode ser excluído) ou reativar.</summary>
    [HttpPost, Permissao("CLIENTES.ALTERAR")]
    public async Task<IActionResult> Inativo(int id, bool inativo)
    {
        this.AplicarNoTempData(await clientes.DefinirInativoAsync(id, inativo), inativo ? "Cadastro inativado: não aparece mais na lista (marque “Mostrar inativos” para vê-lo e reativá-lo)." : "Cadastro reativado.");
        return RedirectToAction(nameof(Index));
    }

    // ---- contatos (P25)

    [HttpGet("Clientes/{idEntidade:int}/Contatos"), Permissao("CLIENTES.CONSULTAR")]
    public Task<IActionResult> Contatos(int idEntidade) =>
        this.AbasAsync(contatos, CadastroDono.Cliente, idEntidade, Rota(idEntidade), "CLIENTES.ALTERAR", ctx);

    [HttpPost("Clientes/{idEntidade:int}/ContatoEndereco"), Permissao("CLIENTES.ALTERAR")]
    public Task<IActionResult> ContatoEndereco(int idEntidade, EnderecoVM vm) =>
        this.RespostaAsync(contatos.GravarEnderecoAsync(CadastroDono.Cliente, idEntidade, vm));

    [HttpPost("Clientes/{idEntidade:int}/ContatoFone"), Permissao("CLIENTES.ALTERAR")]
    public Task<IActionResult> ContatoFone(int idEntidade, FoneVM vm) =>
        this.RespostaAsync(contatos.GravarFoneAsync(CadastroDono.Cliente, idEntidade, vm));

    [HttpPost("Clientes/{idEntidade:int}/ContatoEmail"), Permissao("CLIENTES.ALTERAR")]
    public Task<IActionResult> ContatoEmail(int idEntidade, EmailVM vm) =>
        this.RespostaAsync(contatos.GravarEmailAsync(CadastroDono.Cliente, idEntidade, vm));

    [HttpPost("Clientes/{idEntidade:int}/ContatoExcluir"), Permissao("CLIENTES.ALTERAR")]
    public Task<IActionResult> ContatoExcluir(int idEntidade, TipoContato tipo, int idContato) =>
        this.RespostaAsync(contatos.ExcluirAsync(CadastroDono.Cliente, idEntidade, tipo, idContato));

    private static string Rota(int id) => $"/Clientes/{id}";

    private async Task ContatosNaTelaAsync(int id) =>
        ViewData["Contatos"] = await contatos.CarregarAsync(CadastroDono.Cliente, id, Rota(id), ctx.TemPermissao("CLIENTES.ALTERAR"));
}
