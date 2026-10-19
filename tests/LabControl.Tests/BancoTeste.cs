using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LabControl.Tests;

/// <summary>
/// Conexão com um banco v2 criado por Scripts/v2/recriar.ps1.
/// Ordem: variável de ambiente LABCONTROL_TESTE_CS; senão user-secrets (ConnectionStrings:LabControl).
/// Os testes que gravam usam transação revertida, mas não devem rodar contra um banco com dados reais.
/// (Tarefa 0.7: trocar por fixture que cria e apaga labcontrol_teste_&lt;id&gt;.)
/// </summary>
public static class BancoTeste
{
    public static string ConnectionString { get; } = Ler();

    private static string Ler()
    {
        var cfg = new ConfigurationBuilder()
            .AddUserSecrets(typeof(BancoTeste).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        return cfg["LABCONTROL_TESTE_CS"] ?? cfg.GetConnectionString("LabControl")
            ?? throw new InvalidOperationException("Defina LABCONTROL_TESTE_CS ou o user-secret ConnectionStrings:LabControl.");
    }

    /// <summary>Contexto com a empresa ativa de <paramref name="empresa"/> (sem empresa se omitido).</summary>
    public static LabDbContext NovoContexto(EmpresaFixa? empresa = null) =>
        new(new DbContextOptionsBuilder<LabDbContext>()
            .UseMySql(ConnectionString, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options, empresa ?? new EmpresaFixa());
}

/// <summary>Empresa ativa controlada pelo teste (pode ser trocada no meio do teste).</summary>
public sealed class EmpresaFixa : IEmpresaContexto
{
    public int? EmpresaId { get; set; }
    public int? UsuarioId { get; set; }
    public int? ProfissionalId { get; set; }
    public bool AdminGlobal { get; set; }
}
