using LabControl.Data.Interceptors;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LabControl.Data;

/// <summary>Configuração única da conexão MySQL (aplicação e testes).</summary>
public static class ConexaoMySql
{
    public static readonly MySqlServerVersion Versao = new(new Version(8, 0, 46));

    /// <summary>
    /// AllowUserVariables: sem ele o MySqlConnector trata <c>@LC_*</c> como parâmetro não informado.
    /// ConnectionReset: o pool limpa o estado da sessão ao reaproveitar a conexão (o interceptor redefine).
    /// </summary>
    public static string Normalizar(string connectionString) =>
        new MySqlConnectionStringBuilder(connectionString) { AllowUserVariables = true, ConnectionReset = true }.ConnectionString;

    public static DbContextOptionsBuilder Configurar(this DbContextOptionsBuilder options, string connectionString, IEmpresaContexto empresa) =>
        options.UseMySql(Normalizar(connectionString), Versao)
               .AddInterceptors(new SessaoMySqlInterceptor(empresa));
}
