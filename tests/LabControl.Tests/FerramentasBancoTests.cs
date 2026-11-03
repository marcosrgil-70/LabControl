using LabControl.Data;
using LabControl.Infra;
using LabControl.Services;
using LabControl.Services.Boletins;
using LabControl.Services.Migracao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Ferramentas do banco de trabalho (não rodam na suíte: só com as variáveis de ambiente abaixo).
/// O único banco permanente é o <c>LabControlDB</c>, com os dados do legado, usado para programar e testar à mão
/// (decisão do usuário, 30/10/2026); os testes automáticos usam bancos temporários (<see cref="BancoTeste"/>).
/// </summary>
[Collection(ColecaoMigracao.Nome)]
public class FerramentasBancoTests(ITestOutputHelper saida)
{
    /// <summary>
    /// Recria o banco como o instalador deixa (empresa + ADMINISTRADOR / Admin123) e migra o legado nele mesmo pela T07
    /// (o ADMINISTRADOR migrado fica com Admin123, sem troca). Chamado por <c>Scripts/v2/carregar-legado.ps1</c>:
    /// <c>LABCONTROL_CARREGAR_FDB</c> (o .FDB), <c>LABCONTROL_CARREGAR_BANCO</c> (padrão LabControlDB) e, opcionais,
    /// <c>LABCONTROL_CARREGAR_SELO</c> (imagem do selo da acreditação), <c>LABCONTROL_CARREGAR_ACREDITACAO</c> e
    /// <c>LABCONTROL_CARREGAR_RODAPE</c> (linhas separadas por "|") — o legado guardava o selo e o rodapé dentro do relatório.
    /// </summary>
    [Fact]
    public async Task Carrega_o_legado_no_banco_de_trabalho()
    {
        if (Environment.GetEnvironmentVariable("LABCONTROL_CARREGAR_FDB") is not { Length: > 0 } fdb) { saida.WriteLine("LABCONTROL_CARREGAR_FDB ausente: nada a fazer."); return; }
        var banco = Environment.GetEnvironmentVariable("LABCONTROL_CARREGAR_BANCO") is { Length: > 0 } b ? b : "LabControlDB";
        Assert.True(File.Exists(fdb), $"{fdb} não encontrado.");
        Assert.False(banco.StartsWith(BancoTeste.Prefixo, StringComparison.OrdinalIgnoreCase), "use um banco de trabalho, não um temporário dos testes.");

        await BancoTeste.RecriarAsync(banco, "LABORATÓRIO (A SUBSTITUIR PELO LEGADO)");
        var migracao = Migracao(banco);
        Assert.True(migracao.SubstituiBancoEmUso(banco));
        var pedido = new PedidoMigracao(fdb, "SYSDBA", "masterkey", banco, "Mudar@2026", "127.0.0.1");
        var v = await migracao.ValidarAsync(pedido);
        Assert.True(v.Sucesso, v.Mensagem);
        var relatorio = MigracaoService.NovoRelatorio(banco);
        await migracao.ExecutarAsync(pedido, relatorio);
        var (log, anomalias, conferencias) = relatorio.Copia();
        foreach (var l in log) saida.WriteLine(l);
        saida.WriteLine($"{anomalias.Count} anomalia(s); {conferencias.Count(c => c.Ok)}/{conferencias.Count} conferência(s) verdes.");
        Assert.True(relatorio.Sucesso, string.Join("\n", relatorio.Etapas.Select(e => $"{e.Codigo} {e.Situacao} {e.Erro}")));

        await using var db = Contexto(migracao, banco, out _);
        var selo = Environment.GetEnvironmentVariable("LABCONTROL_CARREGAR_SELO") is { Length: > 0 } arq ? await File.ReadAllBytesAsync(arq) : null;
        var nr = Environment.GetEnvironmentVariable("LABCONTROL_CARREGAR_ACREDITACAO") is { Length: > 0 } n ? n : null;
        var rodape = Environment.GetEnvironmentVariable("LABCONTROL_CARREGAR_RODAPE") is { Length: > 0 } r ? r.Replace('|', '\n') : null;
        var mime = selo is [0x42, 0x4D, ..] ? "image/bmp" : selo is [0xFF, 0xD8, 0xFF, ..] ? "image/jpeg" : "image/png";
        await db.Empresas.ExecuteUpdateAsync(u => u
            .SetProperty(e => e.SeloAcreditacao, e => selo ?? e.SeloAcreditacao)
            .SetProperty(e => e.SeloAcreditacaoMime, e => selo != null ? mime : e.SeloAcreditacaoMime)
            .SetProperty(e => e.NrAcreditacao, e => nr ?? e.NrAcreditacao)
            .SetProperty(e => e.RodapeLaudo, e => rodape ?? e.RodapeLaudo));
        saida.WriteLine($"{banco} pronto: {await db.LabHistAmostras.IgnoreQueryFilters().CountAsync()} amostras, {await db.LabBoletimAnalises.IgnoreQueryFilters().CountAsync()} boletins; login ADMINISTRADOR / Admin123.");
    }

    /// <summary>
    /// 5.4: gera, com o layout atual, o PDF de boletins reais emitidos de um banco migrado (o mesmo da 2ª via, idêntico à
    /// 1ª), para comparar lado a lado com os PDFs do legado: <c>LABCONTROL_REAL_BANCO</c>, <c>LABCONTROL_REAL_BOLETINS</c>
    /// (códigos separados por ";") e <c>LABCONTROL_PDF_SAIDA</c> (pasta).
    /// </summary>
    [Fact]
    public async Task Gera_o_pdf_de_boletins_reais()
    {
        var banco = Environment.GetEnvironmentVariable("LABCONTROL_REAL_BANCO");
        var codigos = Environment.GetEnvironmentVariable("LABCONTROL_REAL_BOLETINS")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pasta = Environment.GetEnvironmentVariable("LABCONTROL_PDF_SAIDA");
        if (banco is null || codigos is null || pasta is null) { saida.WriteLine("Variáveis LABCONTROL_REAL_* ausentes: nada a gerar."); return; }

        var migracao = Migracao(banco);
        await using var db = Contexto(migracao, banco, out var ctx);
        ctx.EmpresaId = await db.Empresas.Select(e => e.IdEmpresas).FirstAsync();
        var procedures = new Procedures(db, ctx);
        var servico = new BoletimService(db, ctx, procedures, new AmostraService(db, ctx, procedures), new ArmazemBoletins(pasta), NullLogger<BoletimService>.Instance);
        foreach (var codigo in codigos)
        {
            var id = await db.LabBoletimAnalises.Where(x => x.CodBoletim == codigo && x.UltimaRevisao).Select(x => (int?)x.IdLabBoletimAnalises).FirstOrDefaultAsync();
            if (id is null) { saida.WriteLine($"{codigo}: não encontrado em {banco}."); continue; }
            var d = await servico.DocumentoAsync(id.Value, ModoBoletim.Emitido);
            var arquivo = Path.Combine(pasta, $"BA {codigo.Replace('/', '-')}.pdf");
            await File.WriteAllBytesAsync(arquivo, BoletimPdf.Gerar(d!));
            saida.WriteLine($"{codigo}: {arquivo} ({string.Join(" + ", BoletimPdf.Partes(d!).Select(p => p.Sufixo ?? (p.Acreditada ? "acreditado" : "não acreditado")))})");
        }
    }

    // a T07 trata o destino como "banco em uso" quando ele é o da connection string (migração no próprio banco instalado)
    private static MigracaoService Migracao(string banco) => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:LabControl"] = new MySqlConnector.MySqlConnectionStringBuilder(BancoTeste.ConnectionString) { Database = banco }.ConnectionString }).Build());

    private static LabDbContext Contexto(MigracaoService migracao, string banco, out EmpresaFixa ctx)
    {
        ctx = new EmpresaFixa { AdminGlobal = true };
        var opcoes = new DbContextOptionsBuilder<LabDbContext>();
        opcoes.Configurar(migracao.ConexaoDestino(banco), ctx);
        return new LabDbContext(opcoes.Options, ctx);
    }
}
