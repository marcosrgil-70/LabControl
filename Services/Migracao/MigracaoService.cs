using System.Diagnostics;
using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Services.Migracao.Etapas;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace LabControl.Services.Migracao;

/// <summary>Pedido de execução (T07): origem Firebird, banco destino e a senha provisória única dos usuários.</summary>
public sealed record PedidoMigracao(string CaminhoFirebird, string UsuarioFirebird, string SenhaFirebird, string BancoDestino, string SenhaProvisoria, string? Ip);

/// <summary>
/// Migração Firebird → MySQL v2 (M-01…M-15; docs/processos/P24-migracao.md). O destino é um banco
/// <b>vazio</b> recém-criado pelos scripts (decisão do usuário: banco separado, ex. <c>labcontrol_migracao</c>);
/// conexão própria com <c>@LC_MIGRACAO = 1</c>; uma transação por etapa (falha desfaz só a etapa e para a execução);
/// repetir = recriar o destino.
/// </summary>
public sealed partial class MigracaoService(IConfiguration config)
{
    /// <summary>Etapas em ordem (decisão do usuário, 4.1: uma execução só, M-01…M-15, num banco recém-criado).</summary>
    public static readonly IReadOnlyList<IEtapaMigracao> Etapas =
    [
        new M01Empresa(), new M02Geografia(), new M03Auxiliares(), new M04ParametrosEmpresa(), new M05Entidades(),
        new M06Contatos(), new M07Enderecos(), new M08Profissionais(), new M09Usuarios(), new M10ProdutosParametros(),
        new M11Propostas(), new M12ProdutosAnalisesProposta(), new M13PedidosVenda(), new M14Amostras(), new M15TestesEstoqueArquivo(),
    ];

    private static readonly string[] BancosProibidos = ["labcontrol", "mysql", "sys", "information_schema", "performance_schema"];

    /// <summary>Connection string do app com outro banco (mesmo servidor e usuário).</summary>
    public string ConexaoDestino(string banco) =>
        new MySqlConnectionStringBuilder(config.GetConnectionString("LabControl") ?? throw new InvalidOperationException("ConnectionStrings:LabControl não configurada.")) { Database = banco }.ConnectionString;

    /// <summary>Comando que cria o destino vazio (mostrado na tela quando ele não existe ou já tem dados).</summary>
    public static string ComandoRecriar(string banco) => $@".\Scripts\v2\recriar.ps1 -Banco {banco} -Forcar";

    /// <summary>Confere o pedido antes de começar: senha provisória, nome do banco, destino vazio e Firebird acessível.</summary>
    public async Task<Resultado> ValidarAsync(PedidoMigracao p, CancellationToken ct = default)
    {
        if (ContaService.ValidarNovaSenha(p.SenhaProvisoria) is { } senhaFraca) return Resultado.Erro($"Senha provisória: {senhaFraca}", "SenhaProvisoria");
        var banco = p.BancoDestino?.Trim().ToLowerInvariant() ?? "";
        if (!RegexBanco().IsMatch(banco) || BancosProibidos.Contains(banco))
            return Resultado.Erro("Banco destino inválido (letras minúsculas, números e _; nunca o labcontrol da v1).", "BancoDestino");

        try
        {
            await using var con = new MySqlConnection(ConexaoDestino(banco));
            await con.OpenAsync(ct);
            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT (SELECT COUNT(*) FROM EMPRESAS) + (SELECT COUNT(*) FROM USUARIOS)";
            if (Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)) > 0)
                return Resultado.Erro($"O banco {banco} não está vazio. A migração exige um banco recém-criado: rode {ComandoRecriar(banco)}", "BancoDestino");
        }
        catch (MySqlException ex) when (ex.ErrorCode is MySqlErrorCode.UnknownDatabase or MySqlErrorCode.NoSuchTable)
        {
            return Resultado.Erro($"O banco {banco} não existe ou não foi criado pelos scripts: rode {ComandoRecriar(banco)}", "BancoDestino");
        }

        try { await using var fb = await LeitorFirebird.AbrirAsync(p.CaminhoFirebird, p.UsuarioFirebird, p.SenhaFirebird, ct); }
        catch (Exception ex) when (ex is FirebirdSql.Data.FirebirdClient.FbException or IOException or ArgumentException)
        {
            return Resultado.Erro($"Não foi possível abrir o Firebird do legado: {ex.Message}", "SenhaFirebird");
        }
        return Resultado.Ok();
    }

    public static RelatorioMigracao NovoRelatorio(string banco) =>
        new(banco, Etapas.Select(e => new EstadoEtapa(e.Codigo, e.Nome)));

    /// <summary>Executa M-01…M-15 em ordem, preenchendo o relatório; para na 1ª etapa com erro.</summary>
    public async Task ExecutarAsync(PedidoMigracao p, RelatorioMigracao relatorio, CancellationToken ct = default)
    {
        var empresa = new EmpresaDaMigracao { Ip = p.Ip };
        var construtor = new DbContextOptionsBuilder<LabDbContext>();
        construtor.Configurar(ConexaoDestino(p.BancoDestino.Trim().ToLowerInvariant()), empresa);
        var opcoes = construtor.Options;
        try
        {
            await using var fb = await LeitorFirebird.AbrirAsync(p.CaminhoFirebird, p.UsuarioFirebird, p.SenhaFirebird, ct);
            await using var db = new LabDbContext(opcoes, empresa);
            await db.Database.OpenConnectionAsync(ct); // a conexão fica aberta: @LC_MIGRACAO vale até o fim
            var c = new ContextoMigracao(fb, db, empresa, relatorio, p.SenhaProvisoria);
            await c.LigarModoMigracaoAsync(ct);
            relatorio.Log($"Origem: {p.CaminhoFirebird} → destino: {p.BancoDestino}.");

            foreach (var (etapa, estado) in Etapas.Zip(relatorio.Etapas))
            {
                c.Etapa = etapa.Codigo;
                estado.Situacao = SituacaoEtapa.Executando;
                var relogio = Stopwatch.StartNew();
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    estado.Gravados = await etapa.ExecutarAsync(c, ct);
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    estado.Situacao = SituacaoEtapa.Ok;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await tx.RollbackAsync(CancellationToken.None);
                    estado.Situacao = SituacaoEtapa.Erro;
                    estado.Erro = ErrosMySql.MensagemDeNegocio(ex) ?? ex.GetBaseException().Message;
                    relatorio.Log($"[{etapa.Codigo}] ERRO: {estado.Erro}");
                    break;
                }
                finally
                {
                    estado.Tempo = relogio.Elapsed;
                    db.ChangeTracker.Clear(); // a etapa seguinte trabalha pelos dicionários, não pelo que ficou rastreado
                }
                relatorio.Log($"[{etapa.Codigo}] {etapa.Nome}: {estado.Gravados} registro(s) em {estado.Tempo.TotalSeconds:0.0} s.");
            }
            await db.Database.ExecuteSqlRawAsync("SET @LC_MIGRACAO = 0", CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            relatorio.Log($"ERRO: {ex.GetBaseException().Message}");
            foreach (var e in relatorio.Etapas.Where(e => e.Situacao is SituacaoEtapa.Pendente or SituacaoEtapa.Executando))
                (e.Situacao, e.Erro) = (SituacaoEtapa.Erro, e.Situacao == SituacaoEtapa.Executando ? ex.GetBaseException().Message : null);
        }
        finally
        {
            relatorio.Concluir();
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{1,63}$")]
    private static partial Regex RegexBanco();
}
