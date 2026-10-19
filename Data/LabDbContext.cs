using System.Linq.Expressions;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Data;

/// <summary>
/// Contexto do banco v2. O schema pertence a Scripts/v2/*.sql: este projeto NÃO usa migrations
/// nem EnsureCreated. Entidades, configurações e DbSets (*.g.cs) são gerados por
/// tools/gerador-csharp/gerar.mjs — rode o gerador sempre que um script mudar.
///
/// Multiempresa (docs/processos/P01-multiempresa.md): toda entidade <see cref="IFiltradoPorEmpresa"/>
/// recebe o filtro da empresa ativa; o SaveChanges grava/confere a empresa nas <see cref="IPorEmpresa"/>.
/// <c>IgnoreQueryFilters()</c> só em migração e telas ADMIN_GLOBAL explícitas.
/// </summary>
public partial class LabDbContext(DbContextOptions<LabDbContext> options, IEmpresaContexto empresa) : DbContext(options)
{
    /// <summary>Empresa ativa do request; lida a cada consulta pelo filtro global (null = nenhum registro).</summary>
    public int? EmpresaAtivaId => empresa.EmpresaId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabDbContext).Assembly);

        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(IFiltradoPorEmpresa).IsAssignableFrom(t.ClrType)))
        {
            // e => (int?)e.IdEmpresas == this.EmpresaAtivaId  (o EF troca "this" pelo contexto da consulta)
            var e = Expression.Parameter(tipo.ClrType, "e");
            var filtro = Expression.Lambda(
                Expression.Equal(
                    Expression.Convert(Expression.Property(e, nameof(IPorEmpresa.IdEmpresas)), typeof(int?)),
                    Expression.Property(Expression.Constant(this), nameof(EmpresaAtivaId))),
                e);
            modelBuilder.Entity(tipo.ClrType).HasQueryFilter(filtro);
        }

        // Filhos sem ID_EMPRESAS: a empresa vem do pai
        modelBuilder.Entity<CondicaoPagtoParc>().HasQueryFilter(e => e.CondicaoPagto.IdEmpresas == EmpresaAtivaId);
        modelBuilder.Entity<PerfilPermissao>().HasQueryFilter(e => e.Perfil.IdEmpresas == EmpresaAtivaId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConferirEmpresa();
        var pendentes = CapturarAuditoria(acceptAllChangesOnSuccess);
        if (pendentes.Count == 0) return base.SaveChanges(acceptAllChangesOnSuccess);

        using var propria = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
        var n = base.SaveChanges(acceptAllChangesOnSuccess);
        Set<Auditoria>().AddRange(pendentes.Select(p => p.Gerar(empresa)));
        base.SaveChanges(true);
        propria?.Commit();
        return n;
    }

    /// <summary>
    /// Confere a empresa e grava a AUDITORIA das tabelas críticas na MESMA transação: usa a transação aberta
    /// pelo serviço ou abre uma própria. Se a auditoria falhar, a alteração também é desfeita.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ConferirEmpresa();
        var pendentes = CapturarAuditoria(acceptAllChangesOnSuccess);
        if (pendentes.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await using var propria = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var n = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        Set<Auditoria>().AddRange(pendentes.Select(p => p.Gerar(empresa)));
        await base.SaveChangesAsync(true, cancellationToken);
        if (propria is not null) await propria.CommitAsync(cancellationToken);
        return n;
    }

    private List<AuditoriaAlteracoes.Pendente> CapturarAuditoria(bool acceptAllChangesOnSuccess)
    {
        var pendentes = AuditoriaAlteracoes.Capturar(ChangeTracker);
        // o 2º SaveChanges (da auditoria) regravaria as entidades ainda não aceitas
        if (pendentes.Count > 0 && !acceptAllChangesOnSuccess)
            throw new NotSupportedException("SaveChanges(acceptAllChangesOnSuccess: false) não é suportado em tabelas auditadas.");
        return pendentes;
    }

    /// <summary>
    /// Inclusão: grava a empresa ativa (ou confere, se já informada). Alteração/exclusão: o registro precisa
    /// ser da empresa ativa e a empresa não pode ser trocada. Sem empresa ativa nada é gravado.
    /// Obs.: alterar = carregar pela consulta filtrada e copiar os campos (nunca <c>Update(entidadeDoFormulario)</c>).
    /// </summary>
    private void ConferirEmpresa()
    {
        foreach (var entrada in ChangeTracker.Entries<IPorEmpresa>())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            var nome = entrada.Metadata.ClrType.Name;
            var ativa = EmpresaAtivaId
                ?? throw new InvalidOperationException($"Gravação de {nome} sem empresa ativa.");

            var prop = entrada.Property(e => e.IdEmpresas);
            if (entrada.State == EntityState.Added)
            {
                if (entrada.Entity.IdEmpresas == 0) entrada.Entity.IdEmpresas = ativa;
                else if (entrada.Entity.IdEmpresas != ativa)
                    throw new InvalidOperationException($"{nome}: inclusão em outra empresa ({entrada.Entity.IdEmpresas}) recusada; empresa ativa = {ativa}.");
                continue;
            }
            if (prop.OriginalValue != ativa)
                throw new InvalidOperationException($"{nome}: registro de outra empresa ({prop.OriginalValue}) não pode ser alterado/excluído.");
            if (prop.CurrentValue != prop.OriginalValue)
                throw new InvalidOperationException($"{nome}: não é permitido trocar a empresa do registro.");
        }
    }
}
