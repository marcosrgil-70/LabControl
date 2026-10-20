using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LabControl.Infra;

/// <param name="Permissao">Código <c>TELA.ACAO</c> exigido (null = qualquer usuário logado).</param>
/// <param name="SoAdminGlobal">Só ADMIN_GLOBAL vê (Migração).</param>
public sealed record ItemMenu(string Texto, string Icone, string Controller, string Action, string Href, string? Permissao, bool SoAdminGlobal = false);

public sealed record SecaoMenu(string Titulo, IReadOnlyList<ItemMenu> Itens);

/// <summary>
/// Menu lateral da T01 (docs/telas/T01-layout-menu.md), que espelha o menu do Delphi reorganizado.
/// Um item aparece se o usuário tem a permissão (ADMIN_GLOBAL tem todas) <b>e</b> se a tela já existe
/// (controller/action registrados): o menu cresce sozinho conforme as telas são reescritas, sem link quebrado.
/// </summary>
public static class Menu
{
    private static ItemMenu I(string texto, string icone, string controller, string permissao, string action = "Index", string? href = null) =>
        new(texto, icone, controller, action, href ?? (action == "Index" ? $"/{controller}" : $"/{controller}/{action}"), permissao);

    public static readonly IReadOnlyList<SecaoMenu> Catalogo =
    [
        new("Principal", [new ItemMenu("Início", "bi-house", "Home", "Index", "/", null)]),
        new("Laboratório",
        [
            I("Propostas", "bi-file-text", "Propostas", "PROPOSTAS.CONSULTAR"),
            I("Pedidos de Venda", "bi-cart-check", "PedidosVenda", "PEDIDOS_VENDA.CONSULTAR"),
            I("Recebimento de Amostras", "bi-droplet", "Amostras", "AMOSTRAS.CONSULTAR"),
            I("Movimentação", "bi-arrow-left-right", "Movimentacao", "MOVIMENTACAO.CONSULTAR"),
            I("Localização", "bi-geo-alt", "Localizacao", "LOCALIZACAO.CONSULTAR"),
            I("Etiquetas", "bi-upc", "Etiquetas", "ETIQUETAS.IMPRIMIR"),
            I("Resultados", "bi-clipboard-check", "Resultados", "RESULTADOS.CONSULTAR"),
            I("Boletins", "bi-file-earmark-check", "Boletins", "BOLETINS.CONSULTAR"),
            I("Relatório de Ensaio", "bi-journal-text", "RelatorioEnsaio", "RELATORIO_ENSAIO.CONSULTAR"),
        ]),
        new("Cadastros",
        [
            I("Clientes/Fornecedores", "bi-people", "Clientes", "CLIENTES.CONSULTAR"),
            I("Profissionais", "bi-person-badge", "Profissionais", "PROFISSIONAIS.CONSULTAR"),
            I("Produtos", "bi-box-seam", "Produtos", "PRODUTOS.CONSULTAR"),
            I("Parâmetros de Análise", "bi-sliders", "ParametrosAnalise", "PARAMETROS_ANALISE.CONSULTAR"),
            I("Auxiliares", "bi-tags", "Auxiliares", "AUXILIARES.CONSULTAR"),
            I("Países", "bi-globe-americas", "Geografia", "GEOGRAFIA.CONSULTAR", "Paises"),
            I("Estados", "bi-map", "Geografia", "GEOGRAFIA.CONSULTAR", "Estados"),
            I("Cidades", "bi-pin-map", "Geografia", "GEOGRAFIA.CONSULTAR", "Cidades"),
        ]),
        new("Relatórios",
        [
            I("Faturamento", "bi-cash-coin", "Relatorios", "RELATORIOS.CONSULTAR", "Faturamento"),
            I("Amostras por período", "bi-calendar-range", "Relatorios", "RELATORIOS.CONSULTAR", "AmostrasPeriodo"),
            I("Amostras por cliente", "bi-person-lines-fill", "Relatorios", "RELATORIOS.CONSULTAR", "AmostrasCliente"),
            I("Parâmetros analisados", "bi-list-check", "Relatorios", "RELATORIOS.CONSULTAR", "ParametrosAnalisados"),
            I("Tempo de análise", "bi-stopwatch", "Relatorios", "RELATORIOS.CONSULTAR", "TempoAnalises"),
            I("Produção por período", "bi-bar-chart", "Relatorios", "RELATORIOS.CONSULTAR", "Producao"),
        ]),
        new("Sistema",
        [
            I("Empresa", "bi-building-gear", "Empresa", "EMPRESAS.CONSULTAR", "Dados"),
            new ItemMenu("Empresas", "bi-buildings", "Empresas", "Index", "/Empresas", null, SoAdminGlobal: true),
            I("Parâmetros da Empresa", "bi-gear", "Empresa", "PARAMETROS_EMPRESA.CONSULTAR", "Parametros"),
            I("Usuários", "bi-people-fill", "Usuarios", "USUARIOS.CONSULTAR"),
            I("Perfis de Acesso", "bi-shield-lock", "Perfis", "PERFIS.CONSULTAR"),
            I("Auditoria", "bi-clock-history", "Auditoria", "AUDITORIA.CONSULTAR"),
            new ItemMenu("Migração de Dados", "bi-database-up", "Migracao", "Index", "/Migracao", null, SoAdminGlobal: true),
        ]),
    ];

    /// <summary>Seções e itens que o usuário vê (seções vazias somem).</summary>
    public static IReadOnlyList<SecaoMenu> Visivel(IEmpresaContexto usuario, Func<string, string, bool> telaExiste) =>
        Catalogo
            .Select(s => s with
            {
                Itens = s.Itens.Where(i => telaExiste(i.Controller, i.Action)
                                           && (i.SoAdminGlobal ? usuario.AdminGlobal : i.Permissao is null || usuario.TemPermissao(i.Permissao)))
                               .ToList(),
            })
            .Where(s => s.Itens.Count > 0)
            .ToList();
}

/// <summary>Actions MVC registradas (controller + action), para o menu e o painel só mostrarem telas que existem.</summary>
public sealed class TelasExistentes(IActionDescriptorCollectionProvider acoes)
{
    private readonly Lazy<HashSet<(string, string)>> _telas = new(() =>
        acoes.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .Select(a => (a.ControllerName.ToUpperInvariant(), a.ActionName.ToUpperInvariant()))
            .ToHashSet());

    public bool Existe(string controller, string action = "Index") =>
        _telas.Value.Contains((controller.ToUpperInvariant(), action.ToUpperInvariant()));
}
