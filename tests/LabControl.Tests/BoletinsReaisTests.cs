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
/// Ferramenta da 5.4 (não roda na suíte): gera, com o layout atual, a 2ª via de boletins reais de um banco migrado, para
/// comparar lado a lado com os PDFs do legado. Só roda com as variáveis de ambiente:
/// <c>LABCONTROL_REAL_BANCO</c> (banco migrado), <c>LABCONTROL_REAL_BOLETINS</c> (códigos separados por ";") e
/// <c>LABCONTROL_PDF_SAIDA</c> (pasta). Com <c>LABCONTROL_REAL_FDB</c> (um .FDB do legado), antes recria o banco e migra.
/// </summary>
[Collection(ColecaoMigracao.Nome)]
public class BoletinsReaisTests(ITestOutputHelper saida)
{
    [Fact]
    public async Task Gera_a_2a_via_dos_boletins_de_um_banco_migrado()
    {
        var banco = Environment.GetEnvironmentVariable("LABCONTROL_REAL_BANCO");
        var codigos = Environment.GetEnvironmentVariable("LABCONTROL_REAL_BOLETINS")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pasta = Environment.GetEnvironmentVariable("LABCONTROL_PDF_SAIDA");
        if (banco is null || codigos is null || pasta is null) { saida.WriteLine("Variáveis LABCONTROL_REAL_* ausentes: nada a gerar."); return; }

        var migracao = new MigracaoService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:LabControl"] = BancoTeste.ConnectionString }).Build());
        if (Environment.GetEnvironmentVariable("LABCONTROL_REAL_FDB") is { Length: > 0 } fdb)
        {
            await BancoTeste.RecriarAsync(banco);
            var pedido = new PedidoMigracao(fdb, "SYSDBA", "masterkey", banco, "Migracao#2026", "127.0.0.1");
            var relatorio = MigracaoService.NovoRelatorio(banco);
            await migracao.ExecutarAsync(pedido, relatorio);
            Assert.True(relatorio.Sucesso, string.Join("\n", relatorio.Etapas.Select(e => $"{e.Codigo} {e.Situacao} {e.Erro}")));
            saida.WriteLine($"{banco} migrado de {fdb}.");
        }

        var ctx = new EmpresaFixa { AdminGlobal = true };
        var opcoes = new DbContextOptionsBuilder<LabDbContext>();
        opcoes.Configurar(migracao.ConexaoDestino(banco), ctx);
        await using var db = new LabDbContext(opcoes.Options, ctx);
        ctx.EmpresaId = await db.Empresas.Select(e => e.IdEmpresas).FirstAsync();
        var procedures = new Procedures(db, ctx);
        var servico = new BoletimService(db, ctx, procedures, new AmostraService(db, ctx, procedures), new ArmazemBoletins(pasta), NullLogger<BoletimService>.Instance);
        foreach (var codigo in codigos)
        {
            var id = await db.LabBoletimAnalises.Where(b => b.CodBoletim == codigo && b.UltimaRevisao).Select(b => (int?)b.IdLabBoletimAnalises).FirstOrDefaultAsync();
            if (id is null) { saida.WriteLine($"{codigo}: não encontrado em {banco}."); continue; }
            var d = await servico.DocumentoAsync(id.Value, ModoBoletim.SegundaViaMigrado);
            var arquivo = Path.Combine(pasta, $"BA {codigo.Replace('/', '-')}.pdf");
            await File.WriteAllBytesAsync(arquivo, BoletimPdf.Gerar(d!));
            saida.WriteLine($"{codigo}: {arquivo} ({string.Join(" + ", BoletimPdf.Partes(d!).Select(p => p.Sufixo ?? (p.Acreditada ? "acreditado" : "não acreditado")))})");
        }
    }
}
