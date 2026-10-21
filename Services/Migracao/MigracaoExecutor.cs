namespace LabControl.Services.Migracao;

/// <summary>
/// Executa a migração em segundo plano (singleton): uma execução por vez; a tela T07 acompanha o
/// <see cref="Atual"/> por polling. O relatório fica em memória até a próxima execução ou o app reiniciar.
/// </summary>
public sealed class MigracaoExecutor(IServiceScopeFactory escopos, ILogger<MigracaoExecutor> log)
{
    private readonly Lock _lock = new();

    public RelatorioMigracao? Atual { get; private set; }

    public bool Executando
    {
        get { lock (_lock) return Atual is { Terminou: false }; }
    }

    /// <summary>Começa a execução; false se já há uma em andamento.</summary>
    public bool Iniciar(PedidoMigracao pedido)
    {
        RelatorioMigracao relatorio;
        lock (_lock)
        {
            if (Atual is { Terminou: false }) return false;
            Atual = relatorio = MigracaoService.NovoRelatorio(pedido.BancoDestino.Trim().ToLowerInvariant());
        }
        _ = Task.Run(async () =>
        {
            try
            {
                using var escopo = escopos.CreateScope();
                await escopo.ServiceProvider.GetRequiredService<MigracaoService>().ExecutarAsync(pedido, relatorio);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Migração interrompida");
                relatorio.Log($"ERRO: {ex.Message}");
                relatorio.Concluir();
            }
        });
        return true;
    }
}
