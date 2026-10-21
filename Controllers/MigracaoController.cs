using System.Text;
using LabControl.Infra;
using LabControl.Services.Migracao;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// T07 — Migração Firebird → MySQL v2 (<c>/Migracao</c>, só ADMIN_GLOBAL). Parte 1 (3.5): M-01…M-10. Executa em
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
        var csv = new StringBuilder("Tipo;Etapa;Item;Esperado;Encontrado;Situação\r\n");
        static string C(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        foreach (var e in r.Etapas) csv.Append($"Etapa;{e.Codigo};{C(e.Nome)};;{e.Gravados};{e.Situacao} {C(e.Erro)}\r\n");
        foreach (var c in conferencias) csv.Append($"Conferência;{c.Etapa};{C(c.Item)};{C(c.Esperado)};{C(c.Encontrado)};{(c.Ok ? "OK" : "FALHA")}\r\n");
        foreach (var a in anomalias) csv.Append($"Anomalia;{a.Etapa};{C(a.Texto)};;;\r\n");
        foreach (var l in log) csv.Append($"Log;;{C(l)};;;\r\n");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv",
            $"migracao-{r.Destino}-{r.Inicio:yyyyMMdd-HHmm}.csv");
    }

    private MigracaoFormVM FormPadrao() => new()
    {
        CaminhoFirebird = config["Firebird:DatabasePath"] ?? @"C:\Project\Laboratorio\DBMAIS.FDB",
        UsuarioFirebird = config["Firebird:User"] ?? "SYSDBA",
    };
}
