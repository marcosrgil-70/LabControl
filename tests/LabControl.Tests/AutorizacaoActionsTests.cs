using System.Reflection;
using LabControl.Infra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LabControl.Tests;

/// <summary>
/// 1.7 — nenhuma action de escrita sem autorização (RN-02.07): falha quando alguém cria uma action nova e esquece
/// o <see cref="PermissaoAttribute"/>. Vale só para o assembly do app (os controllers de teste ficam de fora).
/// </summary>
public class AutorizacaoActionsTests
{
    // fluxo da própria conta (qualquer usuário logado) e a página de erro (reexecutada com o verbo original)
    private static readonly HashSet<string> Excecoes =
    [
        "Conta.Login", "Conta.TrocarSenha", "Conta.Empresa", "Conta.Sair", "Home.Error",
    ];

    private static IEnumerable<(Type Controller, MethodInfo Action, string Nome)> Actions() =>
        typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() is null)
                .Select(m => (t, m, $"{t.Name[..^"Controller".Length]}.{m.Name}")));

    private static IEnumerable<string> Verbos(MethodInfo m) =>
        m.GetCustomAttributes<HttpMethodAttribute>(true).SelectMany(a => a.HttpMethods);

    [Fact]
    public void Toda_action_declara_o_verbo_http()
    {
        // sem [HttpGet]/[HttpPost] a action aceita qualquer verbo — um GET de consulta viraria uma porta de POST
        var sem = Actions().Where(a => !Verbos(a.Action).Any() && !Excecoes.Contains(a.Nome)).Select(a => a.Nome).ToList();
        Assert.True(sem.Count == 0, "Actions sem verbo HTTP: " + string.Join(", ", sem));
    }

    [Fact]
    public void Toda_action_de_escrita_exige_permissao_na_propria_action_ou_admin_global()
    {
        var faltando = Actions()
            .Where(a => Verbos(a.Action).Any(v => v != "GET" && v != "HEAD") && !Excecoes.Contains(a.Nome))
            .Where(a => a.Action.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                        || (a.Action.GetCustomAttributes<PermissaoAttribute>().All(p => p.Codigo.EndsWith(".CONSULTAR"))
                            && a.Action.GetCustomAttribute<SomenteAdminGlobalAttribute>() is null
                            && a.Controller.GetCustomAttribute<SomenteAdminGlobalAttribute>() is null))
            .Select(a => a.Nome).ToList();
        Assert.True(faltando.Count == 0, "Actions de escrita sem [Permissao] de escrita/[SomenteAdminGlobal]: " + string.Join(", ", faltando));
    }

    [Fact]
    public void Anonimo_so_no_login_e_na_pagina_de_erro()
    {
        var anonimas = Actions()
            .Where(a => a.Action.GetCustomAttribute<AllowAnonymousAttribute>() is not null || a.Controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(a => a.Nome).Distinct().Order().ToList();
        Assert.Equal(["Conta.Login", "Home.Error"], anonimas);
    }
}
