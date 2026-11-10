using System.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace LabControl.Tests;

/// <summary>
/// Auditoria SABOTADOR do f71f138, rodada 3 (09/11/2026): erro de concorrência (1205) vai para o log com o texto
/// técnico, uma vez por chamada de MensagemDeNegocio, sem a connection string; EhConcorrencia reconhece; a expressão
/// da migração fica com o texto técnico. Proteção contra regressão (passa hoje).
/// </summary>
public class AuditoriaRodada3LogConcorrenciaTests
{
    [Fact]
    public async Task Espera_de_trava_esgotada_vai_para_o_log_sem_segredo_e_a_migracao_ve_o_texto_tecnico()
    {
        await using var c = await Cenario.CriarAsync();
        var id = c.Produto.IdProdutos;
        Exception? erro = null;

        await using (var con = new MySqlConnection(BancoTeste.ConnectionString))
        {
            await con.OpenAsync();
            await using var tx = await con.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            await using (var cmd = new MySqlCommand("SELECT 1 FROM PRODUTOS WHERE ID_PRODUTOS = @id FOR UPDATE", con, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            await c.Db.Database.OpenConnectionAsync();
            await c.Db.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1");
            try { await c.Db.Database.ExecuteSqlRawAsync("UPDATE PRODUTOS SET DESCRICAO = 'X' WHERE ID_PRODUTOS = {0}", id); }
            catch (Exception ex) { erro = ex; }
            await tx.RollbackAsync();
        }

        Assert.NotNull(erro);
        var anterior = ErrosMySql.Log;
        var log = new Captura { Alvo = erro!.GetBaseException() };
        try
        {
            ErrosMySql.Log = log;
            Assert.True(ErrosMySql.EhConcorrencia(erro!));
            var msg1 = ErrosMySql.MensagemDeNegocio(erro!);
            var msg2 = ErrosMySql.MensagemDeNegocio(erro!);
            Assert.Equal(msg1, msg2);   // sem estado: repetir dá a mesma frase
            Assert.Equal(2, log.Linhas.Count);   // (só as desta exceção: o Log é estático e outros testes rodam em paralelo) uma linha por chamada (o fluxo normal chama uma vez)
            Assert.All(log.Linhas, l => Assert.Contains("1205", l));
            Assert.All(log.Linhas, l => Assert.DoesNotContain("Password", l, StringComparison.OrdinalIgnoreCase));

            // expressão usada na MigracaoService (estado.Erro): texto técnico, não a frase para o usuário
            var textoMigracao = (ErrosMySql.EhConcorrencia(erro!) ? null : ErrosMySql.MensagemDeNegocio(erro!)) ?? erro!.GetBaseException().Message;
            Assert.Contains("Lock wait timeout", textoMigracao);
            Assert.Equal(2, log.Linhas.Count);   // o ramo da migração não loga de novo
        }
        finally { ErrosMySql.Log = anterior; }
    }

    private sealed class Captura : ILogger
    {
        public List<(Exception? Ex, string Texto)> Todas { get; } = [];
        public Exception? Alvo { get; set; }
        public List<string> Linhas { get { lock (Todas) return Todas.Where(t => ReferenceEquals(t.Ex, Alvo)).Select(t => t.Texto).ToList(); } }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Todas) Todas.Add((exception, $"{logLevel}|{formatter(state, exception)}|{exception}"));
        }
    }
}
