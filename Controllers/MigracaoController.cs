using System.Text;
using LabControl.Infra;
using LabControl.Services.Migracao;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T07 — Migração Firebird → MySQL v2 (<c>/Migracao</c>, só ADMIN_GLOBAL). M-01…M-18 (3.5, 4.1 e 5.1), numa execução só. Executa em
/// segundo plano num banco destino vazio; a página se atualiza sozinha enquanto roda e mostra etapas, conferências e
/// anomalias (CSV).
/// </summary>
public sealed class MigracaoController(MigracaoService migracao, MigracaoExecutor executor, IConfiguration config, IEmpresaContexto usuario) : Controller
{
    [HttpGet, SomenteAdminGlobal]
    public IActionResult Index() => View(new MigracaoVM { Form = FormPadrao(), Relatorio = executor.Atual });

    [HttpPost, SomenteAdminGlobal]
    public async Task<IActionResult> Executar(MigracaoFormVM form)
    {
        if (executor.Executando) ModelState.AddModelError("", "Já há uma migração em andamento.");
        if (ModelState.IsValid)
        {
            var pedido = new PedidoMigracao(form.CaminhoFirebird.Trim(), form.UsuarioFirebird.Trim(), form.SenhaFirebird ?? "",
                form.BancoDestino, form.SenhaProvisoria!, usuario.Ip);
            if (this.AplicarNoFormulario(await migracao.ValidarAsync(pedido)) && executor.Iniciar(pedido))
                return RedirectToAction(nameof(Index));
        }
        // senhas nunca voltam para a tela
        (form.SenhaFirebird, form.SenhaProvisoria, form.ConfirmarSenha) = (null, null, null);
        return View(nameof(Index), new MigracaoVM { Form = form, Relatorio = executor.Atual });
    }

    /// <summary>Relatório da última execução em CSV (etapas, conferências e anomalias; sem senhas).</summary>
    [HttpGet, SomenteAdminGlobal]
    public IActionResult Relatorio()
    {
        if (executor.Atual is not { } r) return NotFound();
        var (log, anomalias, conferencias) = r.Copia();
        var csv = new Csv().Linha("Tipo", "Etapa", "Item", "Esperado", "Encontrado", "Situação");
        foreach (var e in r.Etapas) csv.Linha("Etapa", e.Codigo, e.Nome, null, e.Gravados.ToString(), $"{e.Situacao} {e.Erro}".TrimEnd());
        foreach (var c in conferencias) csv.Linha("Conferência", c.Etapa, c.Item, c.Esperado, c.Encontrado, c.Ok ? "OK" : "FALHA");
        foreach (var a in anomalias) csv.Linha("Anomalia", a.Etapa, a.Texto, null, null, null);
        foreach (var l in log) csv.Linha("Log", null, l, null, null, null);
        return File(csv.Bytes(), "text/csv",
            $"migracao-{r.Destino}-{r.Inicio:yyyyMMdd-HHmm}.csv");
    }

    private MigracaoFormVM FormPadrao() => new()
    {
        CaminhoFirebird = config["Firebird:DatabasePath"] ?? @"C:\Project\Laboratorio\DBMAIS.FDB",
        UsuarioFirebird = config["Firebird:User"] ?? "SYSDBA",
    };
}
