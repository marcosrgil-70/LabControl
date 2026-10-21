using System.Diagnostics;
using System.Security.Cryptography;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace LabControl.Tests;

/// <summary>
/// Banco de testes <c>labcontrol_teste</c> (nunca o labcontrol_v2): criado por Scripts/v2/recriar.ps1 na
/// primeira execução e recriado sozinho quando Scripts/v2/*.sql muda (hash gravado em _TESTE_VERSAO).
/// Servidor/usuário/senha vêm de LABCONTROL_TESTE_CS (variável de ambiente) ou do user-secret
/// ConnectionStrings:LabControl; o nome do banco é sempre trocado por <see cref="NomeBanco"/>.
/// Isolamento: cada teste cria a própria empresa (<see cref="CriarEmpresaAsync"/>) — o filtro multiempresa
/// separa os dados; testes que não chamam procedures com transação própria podem usar transação revertida.
/// </summary>
public static class BancoTeste
{
    public const string NomeBanco = "labcontrol_teste";

    public static string ConnectionString { get; } = Montar();

    private static readonly Lazy<Task> Preparado = new(PrepararAsync);

    private static string Montar()
    {
        var cfg = new ConfigurationBuilder()
            .AddUserSecrets(typeof(BancoTeste).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        var cs = cfg["LABCONTROL_TESTE_CS"] ?? cfg.GetConnectionString("LabControl")
            ?? throw new InvalidOperationException("Defina LABCONTROL_TESTE_CS ou o user-secret ConnectionStrings:LabControl.");
        return new MySqlConnectionStringBuilder(cs) { Database = NomeBanco }.ConnectionString;
    }

    /// <summary>Contexto do banco de testes com a empresa ativa de <paramref name="empresa"/> (sem empresa se omitido).</summary>
    public static async Task<LabDbContext> NovoContextoAsync(EmpresaFixa? empresa = null)
    {
        await Preparado.Value;
        var options = new DbContextOptionsBuilder<LabDbContext>();
        empresa ??= new EmpresaFixa();
        options.Configurar(ConnectionString, empresa);
        return new LabDbContext(options.Options, empresa);
    }

    /// <summary>
    /// Empresa nova (código aleatório) inicializada por SP_INICIALIZAR_EMPRESA, um profissional vinculado
    /// e um usuário; <paramref name="ativa"/> passa a apontar para ela.
    /// </summary>
    public static async Task<Empresa> CriarEmpresaAsync(LabDbContext db, EmpresaFixa ativa)
    {
        ativa.EmpresaId = null;
        Empresa emp;
        while (true)
        {
            emp = new Empresa { CodEmpresa = CodigoAleatorio(), RazaoSocial = "EMPRESA DE TESTE" };
            db.Add(emp);
            try { await db.SaveChangesAsync(); break; }
            catch (DbUpdateException ex) when (ErrosMySql.Encontrar(ex)?.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                db.ChangeTracker.Clear(); // código já usado por outra execução
            }
        }
        ativa.AdminGlobal = true; // criar/inicializar empresa é operação de ADMIN_GLOBAL
        var ini = await new Procedures(db, ativa).InicializarEmpresaAsync(emp.IdEmpresas);
        ativa.AdminGlobal = false;
        if (!ini.Sucesso) throw new InvalidOperationException(ini.Mensagem);
        ativa.EmpresaId = emp.IdEmpresas;

        var func = new Entidade { Categoria = Categoria.Fisica, Nome = "PROFISSIONAL TESTE", TipoFuncionario = true };
        db.Add(func);
        await db.SaveChangesAsync();
        db.Add(new EntidadeFuncionario { IdEntidades = func.IdEntidades, AssinaBoletim = true });
        var usu = new Usuario { Login = "t" + Guid.NewGuid().ToString("N")[..20], Nome = "USUÁRIO TESTE", SenhaHash = "-" };
        db.Add(usu);
        await db.SaveChangesAsync();
        ativa.ProfissionalId = func.IdEntidades;
        ativa.UsuarioId = usu.IdUsuarios;
        return emp;
    }

    // COD_EMPRESA é VARCHAR(3) único: 36³ combinações (o banco de testes é recriado quando os scripts mudam)
    private static string CodigoAleatorio() =>
        new(Enumerable.Range(0, 3).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[RandomNumberGenerator.GetInt32(36)]).ToArray());

    private static async Task PrepararAsync()
    {
        var raiz = RaizDoRepositorio();
        var scripts = Path.Combine(raiz, "Scripts", "v2");
        var hash = Convert.ToHexString(SHA256.HashData(
            Directory.GetFiles(scripts, "0*.sql").Order().SelectMany(File.ReadAllBytes).ToArray()));

        if (await HashAtualAsync() == hash) return;

        await RecriarAsync(NomeBanco);

        await using var con = new MySqlConnection(ConnectionString);
        await con.OpenAsync();
        await using var cmd = con.CreateCommand();
        cmd.CommandText = "CREATE TABLE _TESTE_VERSAO (HASH CHAR(64) NOT NULL); INSERT INTO _TESTE_VERSAO VALUES (@h);";
        cmd.Parameters.AddWithValue("@h", hash);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Apaga e recria <paramref name="banco"/> pelos scripts (<c>recriar.ps1 -Forcar</c>), no servidor dos testes.</summary>
    public static async Task RecriarAsync(string banco)
    {
        var cs = new MySqlConnectionStringBuilder(ConnectionString);
        var psi = new ProcessStartInfo("powershell.exe",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(RaizDoRepositorio(), "Scripts", "v2", "recriar.ps1"),
             "-Banco", banco, "-Servidor", cs.Server, "-Porta", cs.Port.ToString(), "-Usuario", cs.UserID, "-Forcar"])
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.Environment["MYSQL_PWD"] = cs.Password;
        using var p = Process.Start(psi)!;
        var saida = p.StandardOutput.ReadToEndAsync();
        var erro = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"recriar.ps1 falhou ({p.ExitCode}):\n{await saida}\n{await erro}");
    }

    private static async Task<string?> HashAtualAsync()
    {
        try
        {
            await using var con = new MySqlConnection(ConnectionString);
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT HASH FROM _TESTE_VERSAO";
            return (string?)await cmd.ExecuteScalarAsync();
        }
        catch (MySqlException) { return null; } // banco ou tabela ainda não existe
    }

    private static string RaizDoRepositorio()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "Scripts", "v2", "recriar.ps1"))) return d.FullName;
        throw new InvalidOperationException("Raiz do repositório (Scripts/v2/recriar.ps1) não encontrada.");
    }
}

/// <summary>Empresa ativa controlada pelo teste (pode ser trocada no meio do teste).</summary>
public sealed class EmpresaFixa : IEmpresaContexto
{
    public int? EmpresaId { get; set; }
    public int? UsuarioId { get; set; }
    public int? ProfissionalId { get; set; }
    public bool AdminGlobal { get; set; }
    public string? Ip { get; set; } = "127.0.0.1";
    public HashSet<string> Permissoes { get; } = [];
    public bool TemPermissao(string codigo) => AdminGlobal || Permissoes.Contains(codigo);
}
