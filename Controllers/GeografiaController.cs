using LabControl.Infra;
using LabControl.Services.Geografia;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T11-01…03 — países, estados e cidades (tabelas globais): consulta com GEOGRAFIA.CONSULTAR; incluir, alterar,
/// excluir e importar IBGE só ADMIN_GLOBAL. Também o apoio ao componente de endereço (P25): cidades da UF e CEP.
/// </summary>
public sealed class GeografiaController(ConsultaCepService cep, GeografiaService geografia) : Controller
{
    private static readonly string[] Telas = [GeografiaService.Paises, GeografiaService.Estados, GeografiaService.Cidades];

    [HttpGet, Permissao("GEOGRAFIA.CONSULTAR")]
    public Task<IActionResult> Paises(GeoFiltroVM filtro) => ListaAsync(GeografiaService.Paises, filtro);

    [HttpGet, Permissao("GEOGRAFIA.CONSULTAR")]
    public Task<IActionResult> Estados(GeoFiltroVM filtro) => ListaAsync(GeografiaService.Estados, filtro);

    [HttpGet, Permissao("GEOGRAFIA.CONSULTAR")]
    public Task<IActionResult> Cidades(GeoFiltroVM filtro) => ListaAsync(GeografiaService.Cidades, filtro);

    /// <summary>Inclui (Id = 0) ou altera; erro → a lista com o modal reaberto e o erro no campo.</summary>
    [HttpPost("Geografia/{tela}/Gravar"), SomenteAdminGlobal]
    public async Task<IActionResult> Gravar(string tela, GeoFormVM vm)
    {
        if (!Telas.Contains(tela)) return NotFound();
        var filtro = new GeoFiltroVM { Busca = vm.Busca, IdPais = vm.IdPais, IdEstado = vm.IdEstado, Pagina = vm.Pagina };
        var r = await geografia.GravarAsync(tela, vm);
        if (r.Sucesso)
        {
            TempData[Mensagens.Sucesso] = Frase(tela, "gravado");
            return VoltarParaLista(tela, filtro);
        }
        if (r.Campo is { } campo) vm.Erros[campo] = r.Mensagem!;
        else vm.Erro = r.Mensagem;
        var lista = await geografia.ListarAsync(tela, filtro);
        lista.Form = vm;
        return View("Lista", lista);
    }

    [HttpPost("Geografia/{tela}/Excluir/{id:int}"), SomenteAdminGlobal]
    public async Task<IActionResult> Excluir(string tela, int id, GeoFiltroVM filtro)
    {
        if (!Telas.Contains(tela)) return NotFound();
        this.AplicarNoTempData(await geografia.ExcluirAsync(tela, id), Frase(tela, "excluído"));
        return VoltarParaLista(tela, filtro);
    }

    /// <summary>Importar municípios IBGE (CSV): inclui os que faltam, atualiza nomes pelo código e mostra o relatório.</summary>
    [HttpPost, SomenteAdminGlobal, RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> ImportarIbge(IFormFile? arquivo)
    {
        var filtro = new GeoFiltroVM();
        if (arquivo is null || arquivo.Length == 0)
        {
            TempData[Mensagens.Erro] = "Escolha o arquivo CSV do IBGE.";
            return VoltarParaLista(GeografiaService.Cidades, filtro);
        }
        await using var conteudo = arquivo.OpenReadStream();
        var r = await geografia.ImportarIbgeAsync(conteudo);
        if (!r.Sucesso)
        {
            TempData[Mensagens.Erro] = r.Mensagem;
            return VoltarParaLista(GeografiaService.Cidades, filtro);
        }
        var lista = await geografia.ListarAsync(GeografiaService.Cidades, filtro);
        lista.Importacao = r.Valor;
        return View("Lista", lista);
    }

    /// <summary>Cidades da UF para o combo (JSON <c>[{ id, nome }]</c>).</summary>
    [HttpGet]
    // não se chama "Cidades": esse nome é a tela T11-03
    public async Task<IActionResult> CidadesDaUf(int idEstado) => Json(await geografia.CidadesDaUfAsync(idEstado));

    /// <summary>Endereço do CEP (ViaCEP no servidor, cache de 1 dia); erro → 422 <c>{ erro }</c>.</summary>
    [HttpGet("Geografia/Cep/{numero}")]
    public async Task<IActionResult> Cep(string numero)
    {
        var r = await cep.ConsultarAsync(numero);
        return r.Sucesso ? Json(r.Valor) : UnprocessableEntity(new { erro = r.Mensagem });
    }

    // ---------------------------------------------------------------------

    private async Task<IActionResult> ListaAsync(string tela, GeoFiltroVM filtro) =>
        View("Lista", await geografia.ListarAsync(tela, filtro));

    // "País gravado" / "Cidade gravada" (particípio no masculino)
    private static string Frase(string tela, string participio) => tela switch
    {
        GeografiaService.Paises => $"País {participio}.",
        GeografiaService.Estados => $"Estado {participio}.",
        _ => $"Cidade {participio[..^1]}a.",
    };

    private RedirectToActionResult VoltarParaLista(string tela, GeoFiltroVM f) =>
        RedirectToAction(tela, new { f.Busca, f.IdPais, f.IdEstado, Pagina = f.Pagina > 1 ? f.Pagina : (int?)null });
}
