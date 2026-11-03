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
/// Banco de testes <b>temporário</b> (decisão do usuário, 30/10/2026: o único banco permanente é o LabControlDB, com os
/// dados do legado): cada execução do <c>dotnet test</c> cria <c>lc_teste_&lt;data-hora&gt;_&lt;id&gt;</c> por Scripts/v2/recriar.ps1
/// (~3 min) e o apaga no fim, junto com os bancos dos testes de migração (<c>&lt;nome&gt;_mig</c>, <c>&lt;nome&gt;_inst</c>).
/// Sobras de execuções interrompidas (mais de 12 h) são apagadas na próxima. Nunca usa o LabControlDB.
/// Servidor/usuário/senha vêm de LABCONTROL_TESTE_CS (variável de ambiente) ou do user-secret
/// ConnectionStrings:LabControl; o nome do banco é sempre trocado por <see cref="NomeBanco"/>.
/// Isolamento: cada teste cria a própria empresa (<see cref="CriarEmpresaAsync"/>) — o filtro multiempresa
/// separa os dados; testes que não chamam procedures com transação própria podem usar transação revertida.
/// </summary>
public static class BancoTeste
{
    /// <summary>Prefixo dos bancos temporários dos testes (só eles são apagados pela limpeza).</summary>
    public const string Prefixo = "lc_teste_";

    public static string NomeBanco { get; } = $"{Prefixo}{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}";

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

    // COD_EMPRESA é VARCHAR(3) único: 36³ combinações (o banco de testes é novo a cada execução)
    private static string CodigoAleatorio() =>
        new(Enumerable.Range(0, 3).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[RandomNumberGenerator.GetInt32(36)]).ToArray());

    private static async Task PrepararAsync()
    {
        await ApagarSobrasAsync();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => ApagarDestaExecucao();
        await RecriarAsync(NomeBanco);
    }

    // bancos lc_teste_* de execuções interrompidas há mais de 12 h (o nome traz a data e hora da criação)
    private static async Task ApagarSobrasAsync()
    {
        await using var con = new MySqlConnection(new MySqlConnectionStringBuilder(ConnectionString) { Database = "" }.ConnectionString);
        await con.OpenAsync();
        var sobras = new List<string>();
        await using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "SELECT SCHEMA_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME LIKE 'lc\\_teste\\_%'";
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var nome = r.GetString(0);
                if (nome.Length >= Prefixo.Length + 14 &&
                    DateTime.TryParseExact(nome.Substring(Prefixo.Length, 14), "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var criado) &&
                    criado < DateTime.Now.AddHours(-12))
                    sobras.Add(nome);
            }
        }
        foreach (var nome in sobras)
        {
            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS `{nome}`";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // no fim do processo de testes: o banco desta execução e os dos testes de migração (<nome>_mig, <nome>_inst)
    private static void ApagarDestaExecucao()
    {
        try
        {
            using var con = new MySqlConnection(new MySqlConnectionStringBuilder(ConnectionString) { Database = "" }.ConnectionString);
            con.Open();
            var nomes = new List<string>();
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT SCHEMA_NAME FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = @n OR SCHEMA_NAME LIKE CONCAT(@n, '\\_%')";
                cmd.Parameters.AddWithValue("@n", NomeBanco);
                using var r = cmd.ExecuteReader();
                while (r.Read()) nomes.Add(r.GetString(0));
            }
            foreach (var nome in nomes)
            {
                using var cmd = con.CreateCommand();
                cmd.CommandText = $"DROP DATABASE IF EXISTS `{nome}`";
                cmd.ExecuteNonQuery();
            }
        }
        catch (MySqlException) { } // servidor fora do ar no fim: a próxima execução apaga a sobra
    }

    /// <summary>
    /// Apaga e recria <paramref name="banco"/> pelos scripts (<c>recriar.ps1 -Forcar</c>), no servidor dos testes; com
    /// <paramref name="empresa"/>, também o <c>inicial.sql</c> (empresa 001 + ADMINISTRADOR, como o instalador deixa).
    /// </summary>
    public static async Task RecriarAsync(string banco, string? empresa = null)
    {
        var cs = new MySqlConnectionStringBuilder(ConnectionString);
        var psi = new ProcessStartInfo("powershell.exe",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(RaizDoRepositorio(), "Scripts", "v2", "recriar.ps1"),
             "-Banco", banco, "-Servidor", cs.Server, "-Porta", cs.Port.ToString(), "-Usuario", cs.UserID, "-Forcar",
             .. (empresa is null ? Array.Empty<string>() : ["-Empresa", empresa])])
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.Environment["MYSQL_PWD"] = cs.Password;
        using var p = Process.Start(psi)!;
        var saida = p.StandardOutput.ReadToEndAsync();
        var erro = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"recriar.ps1 falhou ({p.ExitCode}):\n{await saida}\n{await erro}");
    }

    public static string RaizDoRepositorio()
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
