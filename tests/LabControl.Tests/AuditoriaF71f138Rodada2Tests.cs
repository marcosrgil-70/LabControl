using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LabControl.Tests;

/// <summary>
/// Auditoria SABOTADOR do commit f71f138, rodada 2 (09/11/2026): proteção contra regressão das correções B-2 e B-4.
/// Nenhum destes testes acessa o MySQL: o recriar.ps1 recebe um mysql.exe inexistente, então, mesmo que a guarda
/// do -Forcar deixe de existir, o script para em "mysql.exe não encontrado" antes de qualquer DROP.
/// </summary>
public class AuditoriaF71f138Rodada2Tests
{
    private static string Recriar => Path.Combine(BancoTeste.RaizDoRepositorio(), "Scripts", "v2", "recriar.ps1");

    private static async Task<(int Codigo, string Saida)> RodarRecriarAsync(params string[] args)
    {
        var psi = new ProcessStartInfo("powershell.exe",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Recriar,
             "-MySql", @"C:\nao\existe\mysql.exe", "-Servidor", "192.0.2.1", "-Porta", "1", .. args])
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.Environment.Remove("MYSQL_PWD");
        using var p = Process.Start(psi)!;
        var saida = p.StandardOutput.ReadToEndAsync();
        var erro = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, await saida + await erro);
    }

    /// <summary>B-4: -Forcar sem -Banco explícito é recusado antes de procurar o mysql.exe (o padrão é o LabControlDB).</summary>
    [Fact]
    public async Task Recriar_recusa_Forcar_sem_Banco_explicito()
    {
        var (codigo, saida) = await RodarRecriarAsync("-Forcar");
        Assert.Equal(1, codigo);
        Assert.Contains("-Forcar exige -Banco", saida);

        (codigo, saida) = await RodarRecriarAsync("-Forcar", "-Teste");
        Assert.Equal(1, codigo);
        Assert.Contains("-Forcar exige -Banco", saida);
    }

    /// <summary>B-4: com -Banco explícito a guarda deixa passar (para no mysql.exe inexistente, sem acessar banco).</summary>
    [Fact]
    public async Task Recriar_aceita_Forcar_com_Banco_explicito()
    {
        var (codigo, saida) = await RodarRecriarAsync("-Banco", "lc_teste_guarda", "-Forcar");
        Assert.Equal(1, codigo);
        Assert.DoesNotContain("-Forcar exige -Banco", saida);
        Assert.Contains("mysql.exe", saida);
    }

    /// <summary>
    /// B-2: o instalador (Funcoes.ps1 › RestaurarBanco) lê as contagens do $esperado do recriar.ps1 e falha se não
    /// conseguir. Garante que a expressão do instalador continua achando as 5 contagens no recriar.ps1.
    /// </summary>
    [Fact]
    public void Contagens_do_recriar_sao_lidas_pela_expressao_do_instalador()
    {
        const string padrao = @"\$esperado = \[ordered\]@\{([^}]+)\}";
        var funcoes = File.ReadAllText(Path.Combine(BancoTeste.RaizDoRepositorio(), "instalador", "scripts", "Funcoes.ps1"));
        Assert.Contains("-match '" + padrao + "'", funcoes);
        Assert.DoesNotContain("TABELAS = 59", funcoes);   // sem contagens fixas

        var m = Regex.Match(File.ReadAllText(Recriar), padrao, RegexOptions.IgnoreCase);
        Assert.True(m.Success);
        var pares = m.Groups[1].Value.Split(';').Select(p => p.Split('='))
            .ToDictionary(p => p[0].Trim(), p => int.Parse(p[1].Trim()));
        foreach (var chave in new[] { "TABELAS", "VIEWS", "FUNCOES", "PROCEDURES", "TRIGGERS" })
            Assert.True(pares.TryGetValue(chave, out var n) && n > 0, $"{chave} ausente ou zero no $esperado do recriar.ps1");
    }
}
