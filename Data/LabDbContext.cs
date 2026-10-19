using Microsoft.EntityFrameworkCore;

namespace LabControl.Data;

/// <summary>
/// Contexto do banco v2. O schema pertence a Scripts/v2/*.sql: este projeto NÃO usa migrations
/// nem EnsureCreated. Entidades, configurações e DbSets (*.g.cs) são gerados por
/// tools/gerador-csharp/gerar.mjs — rode o gerador sempre que um script mudar.
/// </summary>
public partial class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabDbContext).Assembly);
    }
}
