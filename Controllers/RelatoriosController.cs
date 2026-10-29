using LabControl.Data;
using LabControl.Infra;
using LabControl.Services.Relatorios;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T30-01…06 — Relatórios gerenciais (<c>/Relatorios/{nome}</c>, tarefa 8.1): filtros, tabela na tela com totais,
/// PDF e CSV do mesmo conjunto de dados (T30-00). Ver na tela exige consultar; baixar PDF/CSV exige imprimir.
/// </summary>
public sealed class RelatoriosController(RelatoriosService relatorios, LabDbContext db) : Controller
{
    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> Faturamento(RelatorioFiltroVM filtro) => TelaAsync(nameof(Faturamento), filtro);

    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> AmostrasPeriodo(RelatorioFiltroVM filtro) => TelaAsync(nameof(AmostrasPeriodo), filtro);

    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> AmostrasCliente(RelatorioFiltroVM filtro) => TelaAsync(nameof(AmostrasCliente), filtro);

    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> ParametrosAnalisados(RelatorioFiltroVM filtro) => TelaAsync(nameof(ParametrosAnalisados), filtro);

    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> TempoAnalises(RelatorioFiltroVM filtro) => TelaAsync(nameof(TempoAnalises), filtro);

    [HttpGet, Permissao("RELATORIOS.CONSULTAR")]
    public Task<IActionResult> Producao(RelatorioFiltroVM filtro) => TelaAsync(nameof(Producao), filtro);

    /// <summary><c>/Relatorios/Pdf/{nome}?filtros</c></summary>
    [HttpGet, Permissao("RELATORIOS.IMPRIMIR")]
    public Task<IActionResult> Pdf(string id, RelatorioFiltroVM filtro) =>
        ArquivoAsync(id, filtro, async d => File(RelatorioPdf.Gerar(d, await relatorios.EmpresaAsync(), await db.AgoraAsync()), "application/pdf"));

    /// <summary><c>/Relatorios/Csv/{nome}?filtros</c></summary>
    [HttpGet, Permissao("RELATORIOS.IMPRIMIR")]
    public Task<IActionResult> Csv(string id, RelatorioFiltroVM filtro) =>
        ArquivoAsync(id, filtro, d => Task.FromResult<IActionResult>(File(d.Csv(), "text/csv", $"{d.Nome}-{filtro.De:yyyyMMdd}-{filtro.Ate:yyyyMMdd}.csv")));

    private async Task<IActionResult> TelaAsync(string nome, RelatorioFiltroVM filtro)
    {
        var definicao = RelatoriosService.Definicao(nome)!;
        RelatorioDados? dados = null;
        if (filtro.Visualizar)
        {
            var r = await relatorios.GerarAsync(definicao, filtro);
            if (this.AplicarNoFormulario(r)) dados = r.Valor;
        }
        else await relatorios.PreencherPadraoAsync(filtro);
        return View("Relatorio", new RelatorioTelaVM { Definicao = definicao, Filtro = filtro, Dados = dados });
    }

    // período inválido: volta para a tela com a mensagem
    private async Task<IActionResult> ArquivoAsync(string id, RelatorioFiltroVM filtro, Func<RelatorioDados, Task<IActionResult>> arquivo)
    {
        if (RelatoriosService.Definicao(id) is not { } definicao) return NotFound();
        var r = await relatorios.GerarAsync(definicao, filtro);
        if (this.AplicarNoFormulario(r)) return await arquivo(r.Valor);
        return View("Relatorio", new RelatorioTelaVM { Definicao = definicao, Filtro = filtro });
    }
}
