using System.Text.Json;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LabControl.Data;

/// <summary>Códigos de <c>AUDITORIA.ACAO</c> (docs/processos/P21-auditoria-log.md).</summary>
public static class AcoesAuditoria
{
    public const string Inclusao = "I";
    public const string Alteracao = "A";
    public const string Exclusao = "E";
    public const string Revisao = "R";
    public const string Emissao = "M";
    public const string DesfazerRevisao = "D";
    public const string Login = "L";
}

/// <summary>
/// Antes/depois em JSON das tabelas críticas (lista do P21), gravado pelo <see cref="LabDbContext"/> na mesma
/// transação do SaveChanges. Inclusão: linha completa (com os valores gerados pelo banco); exclusão: linha completa;
/// alteração: só as colunas alteradas. Blobs viram "[binário]" e senha "***" — nunca o conteúdo.
/// </summary>
public static class AuditoriaAlteracoes
{
    /// <summary>Tabelas com auditoria obrigatória (P21).</summary>
    public static readonly IReadOnlySet<string> Tabelas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "LAB_PROPOSTAS", "LAB_PROPOSTAS_PRODUTOS", "LAB_PROPOSTAS_PRODANALISES", "LAB_PROPOSTAS_TEXTOS",
        "PEDIDOS_VENDA", "PEDIDOS_VENDA_ITENS",
        "LAB_HIST_AMOSTRAS", "LAB_HIST_AMOSTRAS_TESTES",
        "LAB_RESULTADOS_ANALISES", "LAB_RESULTADOS_PARAM",
        "LAB_BOLETIM_ANALISES", "LAB_BOLETIM_PARAMETROS",
        "LAB_MOV_AMOSTRAS", "LAB_LOCAL_AMOSTRAS",
        "SEQUENCIAS_DOCUMENTOS", "EMPRESAS", "EMPRESAS_PARAMETROS",
        "USUARIOS", "USUARIOS_EMPRESAS", "USUARIOS_PERFIS", "PERFIS", "PERFIS_PERMISSOES",
        "ENTIDADES_FUNCIONARIOS",
    };

    private static readonly HashSet<string> Sigilosas = new(StringComparer.OrdinalIgnoreCase) { "SENHA_HASH" };

    // ULTIMO_ACESSO muda a cada login (o evento de login já é auditado com ACAO = L);
    // ALTERADO_POR repete AUDITORIA.ID_USUARIOS (preenchido pelo LabDbContext em toda alteração real)
    private static readonly HashSet<string> Ignoradas = new(StringComparer.OrdinalIgnoreCase) { "ULTIMO_ACESSO", "ALTERADO_POR" };

    /// <summary>Alteração capturada antes do SaveChanges; a linha de AUDITORIA é montada depois (IDs gerados).</summary>
    public sealed class Pendente(EntityEntry entrada, string acao, Dictionary<string, object?>? antes, IReadOnlyList<IProperty> alteradas)
    {
        public Auditoria Gerar(IEmpresaContexto ctx)
        {
            var tabela = entrada.Metadata.GetTableName()!;
            var chave = entrada.Metadata.FindPrimaryKey()!.Properties.Select(p => entrada.Property(p.Name).CurrentValue);
            var empresa = entrada.Metadata.FindProperty(nameof(IPorEmpresa.IdEmpresas)) is { } pe
                ? (int?)Convert.ToInt32(entrada.Property(pe.Name).CurrentValue)
                : ctx.EmpresaId;
            Dictionary<string, object?>? depois = acao switch
            {
                AcoesAuditoria.Inclusao => Valores(entrada, entrada.Metadata.GetProperties(), atuais: true),
                AcoesAuditoria.Alteracao => Valores(entrada, alteradas, atuais: true),
                _ => null,
            };
            return new Auditoria
            {
                IdEmpresas = empresa, IdUsuarios = ctx.UsuarioId, Ip = ctx.Ip,
                Tabela = tabela, IdRegistro = string.Join("-", chave), Acao = acao,
                DadosAntes = antes is null ? null : JsonSerializer.Serialize(antes),
                DadosDepois = depois is null ? null : JsonSerializer.Serialize(depois),
            };
        }
    }

    /// <summary>
    /// Grava as linhas por INSERT direto, sem passar pelo ChangeTracker: não leva junto alterações pendentes
    /// do serviço e não depende de AcceptAllChanges (o rastreador só é aceito depois do commit).
    /// </summary>
    public static void Inserir(DatabaseFacade db, IEnumerable<Auditoria> linhas)
    {
        foreach (var (sql, args) in Lotes(linhas)) db.ExecuteSqlRaw(sql, args!);
    }

    public static async Task InserirAsync(DatabaseFacade db, IEnumerable<Auditoria> linhas, CancellationToken cancellationToken = default)
    {
        foreach (var (sql, args) in Lotes(linhas)) await db.ExecuteSqlRawAsync(sql, args!, cancellationToken); // nulos viram NULL, como na interpolação
    }

    // um INSERT com várias linhas por lote (8 parâmetros por linha: 500 linhas = 4.000, longe do limite do MySQL)
    private const int LinhasPorLote = 500;

    private static IEnumerable<(string Sql, object?[] Args)> Lotes(IEnumerable<Auditoria> linhas)
    {
        foreach (var lote in linhas.Chunk(LinhasPorLote))
        {
            var args = lote.SelectMany(a => new object?[] { a.IdEmpresas, a.IdUsuarios, a.Tabela, a.IdRegistro, a.Acao, a.DadosAntes, a.DadosDepois, a.Ip }).ToArray();
            var valores = string.Join(", ", lote.Select((_, i) => "(" + string.Join(", ", Enumerable.Range(i * 8, 8).Select(p => $"{{{p}}}")) + ")"));
            yield return ("INSERT INTO AUDITORIA (ID_EMPRESAS, ID_USUARIOS, TABELA, ID_REGISTRO, ACAO, DADOS_ANTES, DADOS_DEPOIS, IP) VALUES " + valores, args);
        }
    }

    /// <summary>Entradas auditáveis do ChangeTracker (chamar antes do SaveChanges).</summary>
    public static List<Pendente> Capturar(ChangeTracker tracker)
    {
        var lista = new List<Pendente>();
        foreach (var e in tracker.Entries())
        {
            if (e.Metadata.GetTableName() is not { } t || !Tabelas.Contains(t)) continue;
            switch (e.State)
            {
                case EntityState.Added:
                    lista.Add(new Pendente(e, AcoesAuditoria.Inclusao, null, []));
                    break;
                case EntityState.Deleted:
                    lista.Add(new Pendente(e, AcoesAuditoria.Exclusao, Valores(e, e.Metadata.GetProperties(), atuais: false), []));
                    break;
                case EntityState.Modified:
                    var alteradas = e.Properties
                        .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue) && !Ignoradas.Contains(Coluna(p.Metadata)))
                        .Select(p => p.Metadata).ToList();
                    if (alteradas.Count > 0)
                        lista.Add(new Pendente(e, AcoesAuditoria.Alteracao, Valores(e, alteradas, atuais: false), alteradas));
                    break;
            }
        }
        return lista;
    }

    private static string Coluna(IProperty p) =>
        p.GetColumnName(StoreObjectIdentifier.Table(p.DeclaringType.GetTableName()!, p.DeclaringType.GetSchema()))!;

    private static Dictionary<string, object?> Valores(EntityEntry e, IEnumerable<IProperty> props, bool atuais)
    {
        var d = new Dictionary<string, object?>();
        foreach (var p in props)
        {
            var col = Coluna(p);
            if (Ignoradas.Contains(col)) continue;
            var v = atuais ? e.Property(p.Name).CurrentValue : e.Property(p.Name).OriginalValue;
            d[col] = Sigilosas.Contains(col) ? "***"
                : v is byte[] ? "[binário]"
                : p.GetTypeMapping().Converter is { } conv && v is not null ? conv.ConvertToProvider(v) // enum → valor do banco
                : v;
        }
        return d;
    }
}
