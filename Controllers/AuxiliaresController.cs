using LabControl.Infra;
using LabControl.Services.Auxiliares;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T10-00 — cadastros auxiliares (<c>/Auxiliares/{tabela}</c>), um controller para todos: a tabela vem do
/// <see cref="CatalogoAuxiliares"/>. Lista com busca/inativos/paginação; incluir/alterar em modal; excluir só sem
/// uso (senão a tela oferece inativar); cadastro rápido "+" e opções de combo em JSON.
/// </summary>
[Route("Auxiliares")]
public sealed class AuxiliaresController(AuxiliarService auxiliares) : Controller
{
    [HttpGet(""), Permissao("AUXILIARES.CONSULTAR")]
    public IActionResult Index() => View(CatalogoAuxiliares.Todos);

    [HttpGet("{tabela}"), Permissao("AUXILIARES.CONSULTAR")]
    public async Task<IActionResult> Lista(string tabela, AuxiliarFiltroVM filtro)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        var vm = await def.ListarAsync(auxiliares, filtro);
        if (TempData[ChaveInativar] is int inativar) vm.OferecerInativar = inativar;
        return View(vm);
    }

    /// <summary>Formulário do modal (HTML parcial): vazio para incluir.</summary>
    [HttpGet("{tabela}/Incluir"), Permissao("AUXILIARES.INCLUIR")]
    public Task<IActionResult> Incluir(string tabela) => FormAsync(tabela, 0);

    [HttpGet("{tabela}/Editar/{id:int}"), Permissao("AUXILIARES.ALTERAR")]
    public Task<IActionResult> Editar(string tabela, int id) => FormAsync(tabela, id);

    [HttpPost("{tabela}/Incluir"), Permissao("AUXILIARES.INCLUIR")]
    public Task<IActionResult> Incluir(string tabela, AuxiliarFormVM vm)
    {
        vm.Id = 0;
        return GravarAsync(tabela, vm);
    }

    [HttpPost("{tabela}/Editar/{id:int}"), Permissao("AUXILIARES.ALTERAR")]
    public Task<IActionResult> Editar(string tabela, int id, AuxiliarFormVM vm)
    {
        vm.Id = id;
        return GravarAsync(tabela, vm);
    }

    [HttpPost("{tabela}/Excluir/{id:int}"), Permissao("AUXILIARES.EXCLUIR")]
    public async Task<IActionResult> Excluir(string tabela, int id, AuxiliarFiltroVM filtro)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        var r = await def.ExcluirAsync(auxiliares, id);
        // "Inativar" só quando o banco recusou por uso (não para "não encontrado" ou regra do auxiliar)
        if (!this.AplicarNoTempData(r, def.Frase("excluído")) && r.Mensagem == ErrosMySql.MensagemEmUso) TempData[ChaveInativar] = id;
        return VoltarParaLista(def, filtro);
    }

    /// <summary>Inativar (ex.: em uso, não pode ser excluído) ou reativar.</summary>
    [HttpPost("{tabela}/Ativo/{id:int}"), Permissao("AUXILIARES.ALTERAR")]
    public async Task<IActionResult> Ativo(string tabela, int id, bool ativo, AuxiliarFiltroVM filtro)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        this.AplicarNoTempData(await def.DefinirAtivoAsync(auxiliares, id, ativo),
            def.Frase(ativo ? "reativado" : "inativado") + (ativo ? "" : " Não aparece mais na lista nem nos combos de inclusão (marque “Mostrar inativos” para vê-lo)."));
        return VoltarParaLista(def, filtro);
    }

    /// <summary>Cadastro rápido "+" (T10-00): inclui e devolve <c>{ id, texto }</c> ao combo que abriu o modal; erro → 422 <c>{ erro, campo }</c>.</summary>
    [HttpPost("{tabela}/Rapido"), Permissao("AUXILIARES.INCLUIR")]
    public async Task<IActionResult> Rapido(string tabela, AuxiliarFormVM vm)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        vm.Id = 0;
        vm.Ativo = true;
        var r = await def.GravarAsync(auxiliares, vm);
        if (!r.Sucesso) return UnprocessableEntity(new { erro = r.Mensagem, campo = r.Campo });
        var texto = (await def.OpcoesAsync(auxiliares, r.Valor)).First(o => o.Id == r.Valor).Nome;
        return Json(new { id = r.Valor, texto });
    }

    /// <summary>Opções de combo (ativos + o já escolhido) para as telas que usam o auxiliar; basta estar logado.</summary>
    [HttpGet("{tabela}/Opcoes")]
    public async Task<IActionResult> Opcoes(string tabela, int? atual) =>
        CatalogoAuxiliares.Encontrar(tabela) is { } def ? Json(await def.OpcoesAsync(auxiliares, atual)) : NotFound();

    // ---------------------------------------------------------------------

    private const string ChaveInativar = "AuxiliarInativar";

    private async Task<IActionResult> FormAsync(string tabela, int id)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        return await def.CarregarAsync(auxiliares, id) is { } vm ? PartialView("_Form", vm) : NotFound();
    }

    /// <summary>Sucesso → volta para a lista no mesmo filtro; erro → a lista com o modal reaberto e o erro no campo.</summary>
    private async Task<IActionResult> GravarAsync(string tabela, AuxiliarFormVM vm)
    {
        if (CatalogoAuxiliares.Encontrar(tabela) is not { } def) return NotFound();
        var filtro = new AuxiliarFiltroVM { Busca = vm.Busca, Inativos = vm.Inativos, Pagina = vm.Pagina };
        var r = await def.GravarAsync(auxiliares, vm);
        if (r.Sucesso)
        {
            TempData[Mensagens.Sucesso] = def.Frase("gravado");
            return VoltarParaLista(def, filtro);
        }
        if (r.Campo is { } campo) vm.Erros[campo] = r.Mensagem!;
        else vm.Erro = r.Mensagem;
        var lista = await def.ListarAsync(auxiliares, filtro);
        await def.PrepararFormAsync(auxiliares, vm);
        lista.Form = vm;
        return View(nameof(Lista), lista);
    }

    private RedirectToActionResult VoltarParaLista(AuxiliarDefinicao def, AuxiliarFiltroVM filtro) =>
        RedirectToAction(nameof(Lista), new { tabela = def.Tabela, filtro.Busca, Inativos = filtro.Inativos ? true : (bool?)null, Pagina = filtro.Pagina > 1 ? filtro.Pagina : (int?)null });

}
