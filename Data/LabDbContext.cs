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
            // e => this.EmpresaAtivaId != null && (int?)e.IdEmpresas == this.EmpresaAtivaId
            // (o EF troca "this" pelo contexto da consulta). O "!= null" evita que, sem empresa ativa, a
            // comparação vire "ID_EMPRESAS IS NULL" e mostre as linhas sem empresa (AUDITORIA, views).
            var e = Expression.Parameter(tipo.ClrType, "e");
            var ativa = Expression.Property(Expression.Constant(this), nameof(EmpresaAtivaId));
            var filtro = Expression.Lambda(
                Expression.AndAlso(
                    Expression.NotEqual(ativa, Expression.Constant(null, typeof(int?))),
                    Expression.Equal(
                        Expression.Convert(Expression.Property(e, nameof(IPorEmpresa.IdEmpresas)), typeof(int?)),
                        ativa)),
                e);
            modelBuilder.Entity(tipo.ClrType).HasQueryFilter(filtro);
        }

        // Filhos sem ID_EMPRESAS: a empresa vem do pai
        modelBuilder.Entity<CondicaoPagtoParc>().HasQueryFilter(e => e.CondicaoPagto.IdEmpresas == EmpresaAtivaId);
        modelBuilder.Entity<PerfilPermissao>().HasQueryFilter(e => e.Perfil.IdEmpresas == EmpresaAtivaId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreencherAutoria();
        ConferirEmpresa();
        var pendentes = AuditoriaAlteracoes.Capturar(ChangeTracker);
        if (pendentes.Count == 0) return base.SaveChanges(acceptAllChangesOnSuccess);

        using var propria = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
        var n = base.SaveChanges(acceptAllChangesOnSuccess: false);
        AuditoriaAlteracoes.Inserir(Database, pendentes.Select(p => p.Gerar(empresa)));
        propria?.Commit();
        if (acceptAllChangesOnSuccess) ChangeTracker.AcceptAllChanges();
        return n;
    }

    /// <summary>
    /// Confere a empresa e grava a AUDITORIA das tabelas críticas na MESMA transação: usa a transação aberta
    /// pelo serviço ou abre uma própria. Com transação própria, se a auditoria ou o commit falharem, a alteração
    /// também é desfeita e o rastreador continua com as alterações pendentes (só é aceito depois do commit). Dentro
    /// da transação do serviço o rastreador é aceito ao fim do SaveChanges, como no EF: se o serviço fizer rollback
    /// depois, o contexto não deve ser reaproveitado (os serviços devolvem o erro e o request termina).
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreencherAutoria();
        ConferirEmpresa();
        var pendentes = AuditoriaAlteracoes.Capturar(ChangeTracker);
        if (pendentes.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        await using var propria = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(cancellationToken) : null;
        var n = await base.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
        await AuditoriaAlteracoes.InserirAsync(Database, pendentes.Select(p => p.Gerar(empresa)), cancellationToken);
        if (propria is not null) await propria.CommitAsync(cancellationToken);
        if (acceptAllChangesOnSuccess) ChangeTracker.AcceptAllChanges();
        return n;
    }

    /// <summary>
    /// CRIADO_POR na inclusão e ALTERADO_POR na alteração (tabelas que têm as colunas), com o usuário do request.
    /// Alteração sem mudança real não ganha ALTERADO_POR (senão viraria um UPDATE e uma linha de auditoria à toa).
    /// </summary>
    private void PreencherAutoria()
    {
        if (empresa.UsuarioId is not { } usuario) return;
        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.State == EntityState.Added && entrada.Metadata.FindProperty("CriadoPor") is { } criado
                && entrada.Property(criado.Name).CurrentValue is null)
                entrada.Property(criado.Name).CurrentValue = usuario;
            else if (entrada.State == EntityState.Modified && entrada.Metadata.FindProperty("AlteradoPor") is { } alterado
                     && entrada.Properties.Any(p => p.IsModified && p.Metadata.Name != alterado.Name && !Equals(p.OriginalValue, p.CurrentValue)))
                entrada.Property(alterado.Name).CurrentValue = usuario;
        }
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

        // Vínculos do usuário com empresas/perfis: sem filtro na leitura (o login lista as empresas do usuário),
        // mas a gravação fica restrita à empresa ativa, exceto para ADMIN_GLOBAL.
        foreach (var entrada in ChangeTracker.Entries().Where(x => x.Entity is UsuarioEmpresa or UsuarioPerfil))
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted) || empresa.AdminGlobal) continue;
            var nome = entrada.Metadata.ClrType.Name;
            var ativa = EmpresaAtivaId
                ?? throw new InvalidOperationException($"Gravação de {nome} sem empresa ativa.");
            var prop = entrada.Property(nameof(UsuarioEmpresa.IdEmpresas));
            if ((int)prop.CurrentValue! != ativa || (entrada.State != EntityState.Added && (int)prop.OriginalValue! != ativa))
                throw new InvalidOperationException($"{nome}: vínculo com outra empresa só pode ser gravado por ADMIN_GLOBAL.");
        }
    }
}
