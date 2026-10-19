using System.Reflection;
using LabControl.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LabControl.Tests;

/// <summary>O modelo EF (gerado de Scripts/v2) confere com o banco criado pelos mesmos scripts.</summary>
public class ModeloTests
{
    private sealed record ColunaBanco(string Tabela, string Coluna, string Tipo, string TipoCompleto, bool Nula);

    private static async Task<List<ColunaBanco>> ColunasDoBancoAsync(LabDbContext db) =>
        await db.Database.SqlQueryRaw<ColunaBanco>(
                "SELECT UPPER(TABLE_NAME) AS Tabela, COLUMN_NAME AS Coluna, DATA_TYPE AS Tipo, " +
                "COLUMN_TYPE AS TipoCompleto, IS_NULLABLE = 'YES' AS Nula " +
                "FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() " +
                "AND TABLE_NAME NOT LIKE '\\_TESTE%'") // controle do banco de testes (BancoTeste)
            .ToListAsync();

    // tipo CLR (sem Nullable<>) esperado para cada DATA_TYPE do MySQL
    private static Type? TipoEsperado(ColunaBanco c) => c.Tipo switch
    {
        "tinyint" => c.TipoCompleto.StartsWith("tinyint(1)") ? typeof(bool) : typeof(byte),
        "smallint" => typeof(short),
        "int" => typeof(int),
        "bigint" => typeof(long),
        "decimal" => typeof(decimal),
        "varchar" or "char" or "text" or "mediumtext" or "longtext" or "json" => typeof(string),
        "enum" => null, // enum C# com conversor: conferido à parte
        "date" => typeof(DateOnly),
        "time" => typeof(TimeOnly),
        "datetime" => typeof(DateTime),
        "mediumblob" or "longblob" or "blob" => typeof(byte[]),
        _ => throw new InvalidOperationException($"DATA_TYPE sem mapeamento no teste: {c.Tipo}"),
    };

    [Fact]
    public async Task Cada_tabela_e_view_do_banco_tem_entidade_com_as_mesmas_colunas_e_tipos()
    {
        await using var db = await BancoTeste.NovoContextoAsync();
        var colunas = await ColunasDoBancoAsync(db);
        var erros = new List<string>();

        var entidades = db.Model.GetEntityTypes().ToList();
        var porObjeto = entidades.ToDictionary(e => (e.GetTableName() ?? e.GetViewName())!.ToUpperInvariant());

        foreach (var grupo in colunas.GroupBy(c => c.Tabela))
        {
            if (!porObjeto.TryGetValue(grupo.Key, out var entidade)) { erros.Add($"{grupo.Key}: sem entidade"); continue; }
            var objeto = StoreObjectIdentifier.Create(entidade, entidade.GetTableName() is null ? StoreObjectType.View : StoreObjectType.Table)!.Value;
            var props = entidade.GetProperties().ToDictionary(p => p.GetColumnName(objeto)!, StringComparer.OrdinalIgnoreCase);

            foreach (var c in grupo)
            {
                if (!props.TryGetValue(c.Coluna, out var p)) { erros.Add($"{c.Tabela}.{c.Coluna}: sem propriedade"); continue; }
                var clr = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                var esperado = TipoEsperado(c);
                if (esperado is null)
                {
                    if (!clr.IsEnum) erros.Add($"{c.Tabela}.{c.Coluna}: ENUM mapeado como {clr.Name}");
                }
                else if (clr != esperado) erros.Add($"{c.Tabela}.{c.Coluna}: {c.TipoCompleto} mapeado como {clr.Name} (esperado {esperado.Name})");

                if (c.Nula && !p.IsNullable) erros.Add($"{c.Tabela}.{c.Coluna}: aceita NULL no banco, mas a propriedade não");
                if (!c.Nula && p.IsNullable && !p.IsKey()) erros.Add($"{c.Tabela}.{c.Coluna}: NOT NULL no banco, mas a propriedade é anulável");
            }
            foreach (var p in props.Keys.Except(grupo.Select(c => c.Coluna), StringComparer.OrdinalIgnoreCase))
                erros.Add($"{grupo.Key}.{p}: propriedade sem coluna no banco");
        }
        foreach (var o in porObjeto.Keys.Except(colunas.Select(c => c.Tabela)))
            erros.Add($"{o}: entidade sem tabela/view no banco");

        Assert.True(erros.Count == 0, string.Join(Environment.NewLine, erros));
        Assert.Equal(69, entidades.Count);
    }

    [Fact]
    public async Task Cada_DbSet_executa_consulta_no_banco()
    {
        await using var db = await BancoTeste.NovoContextoAsync();
        var ler = typeof(ModeloTests).GetMethod(nameof(LerUmAsync), BindingFlags.NonPublic | BindingFlags.Static)!;
        var erros = new List<string>();
        foreach (var e in db.Model.GetEntityTypes())
        {
            try { await (Task)ler.MakeGenericMethod(e.ClrType).Invoke(null, [db])!; }
            catch (Exception ex) { erros.Add($"{e.ClrType.Name}: {ex.GetBaseException().Message}"); }
        }
        Assert.True(erros.Count == 0, string.Join(Environment.NewLine, erros));
    }

    private static Task LerUmAsync<T>(LabDbContext db) where T : class => db.Set<T>().AsNoTracking().Take(1).ToListAsync();
}
