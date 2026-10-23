using System.Text.RegularExpressions;
using LabControl.Models;
using LabControl.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace LabControl.Tests;

/// <summary>
/// Regra do usuário (23/10/2026, CLAUDE.md): em desenvolvimento/testes o ADMINISTRADOR entra com Admin123, sem troca
/// obrigatória — no banco do <c>recriar.ps1 -Dev</c> e na migração com a chave do <c>appsettings.Development.json</c>.
/// </summary>
public class AdministradorDesenvolvimentoTests
{
    [Fact]
    public void Semente_de_desenvolvimento_cria_ADMINISTRADOR_com_Admin123_sem_troca()
    {
        var sql = File.ReadAllText(Path.Combine(BancoTeste.RaizDoRepositorio(), "Scripts", "v2", "dev", "admin_dev.sql"));
        var m = Regex.Match(sql, @"VALUES \('(?<login>[^']+)', '[^']+', '(?<hash>[^']+)', (?<global>\d), (?<trocar>\d)\)");
        Assert.True(m.Success, "INSERT do usuário não encontrado no admin_dev.sql");
        Assert.Equal((ContaService.LoginAdministrador, "1", "0"), (m.Groups["login"].Value, m.Groups["global"].Value, m.Groups["trocar"].Value));
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<Usuario>().VerifyHashedPassword(new Usuario(), m.Groups["hash"].Value, ContaService.SenhaDesenvolvimento));
        // vinculado ao profissional especial (a proposta/amostra registram quem digitou)
        Assert.Contains($"'{ContaService.ProfissionalEspecial}'", sql);
        Assert.Contains("INSERT INTO USUARIOS_EMPRESAS", sql);
    }

    [Fact]
    public void Chave_ligada_so_no_appsettings_de_desenvolvimento()
    {
        var raiz = BancoTeste.RaizDoRepositorio();
        bool Ligada(string arquivo) => new ConfigurationBuilder().AddJsonFile(Path.Combine(raiz, arquivo), optional: false).Build()
            .GetValue<bool>(ContaService.ChaveAdministradorDesenvolvimento);
        Assert.True(Ligada("appsettings.Development.json"));
        Assert.False(Ligada("appsettings.json")); // produção: senha provisória da T07, troca no 1º acesso
    }
}
