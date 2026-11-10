using System.Data.Common;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Auditoria SABOTADOR do commit f71f138 (09/11/2026): prazo em dias úteis (FN_DATA_PREVISTA, D-29), 2ª via do boletim
/// idêntica à 1ª e banco único LabControlDB com testes em bancos temporários. Proteção contra regressão dos ataques
/// que resistiram.
/// </summary>
public class AuditoriaF71f138Tests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "lc-auditoria-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true);
        GC.SuppressFinalize(this);
    }

    private BoletimService Boletins(Cenario c) =>
        new(c.Db, c.Ativa, c.Procedures, new AmostraService(c.Db, c.Ativa, c.Procedures), new ArmazemBoletins(_pasta), NullLogger<BoletimService>.Instance);

    private static bool Util(DateOnly d) => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static async Task<DbConnection> ConexaoAsync(Cenario c)
    {
        var con = c.Db.Database.GetDbConnection();
        if (con.State != System.Data.ConnectionState.Open) await con.OpenAsync();
        return con;
    }

    private static async Task<DateOnly?> PrevistaAsync(Cenario c, string? data, int? prazo)
    {
        var con = await ConexaoAsync(c);
        await using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT FN_DATA_PREVISTA(@d, @p)";
        var pd = cmd.CreateParameter(); pd.ParameterName = "@d"; pd.Value = (object?)data ?? DBNull.Value; cmd.Parameters.Add(pd);
        var pp = cmd.CreateParameter(); pp.ParameterName = "@p"; pp.Value = (object?)prazo ?? DBNull.Value; cmd.Parameters.Add(pp);
        var r = await cmd.ExecuteScalarAsync();
        return r is null or DBNull ? null : DateOnly.FromDateTime(Convert.ToDateTime(r));
    }

    private static async Task<Prazo> PrazoAsync(Cenario c, int dias, bool uteis)
    {
        var p = new Prazo { Descricao = $"AUD {dias} {(uteis ? "UTEIS" : "CORRIDOS")} {Guid.NewGuid():N}"[..40], QtdeDias = dias, DiasUteis = uteis };
        c.Db.Add(p);
        await c.Db.SaveChangesAsync();
        return p;
    }

    /// <summary>
    /// Propriedade independente do algoritmo: com dias úteis, para toda data de partida (3 semanas, inclusive sábado e
    /// domingo) e de 1 a 12 dias, a data prevista é dia útil e há exatamente N dias úteis em (entrega, prevista].
    /// Com 0 dias: a própria data. Dias corridos: entrega + N.
    /// </summary>
    [Fact]
    public async Task Dias_uteis_caem_sempre_em_dia_util_com_exatamente_N_uteis_e_corridos_somam_N()
    {
        await using var c = await Cenario.CriarAsync();
        var inicio = new DateOnly(2026, 12, 21); // segunda; atravessa Natal e a virada do ano
        for (var n = 0; n <= 12; n++)
        {
            var uteis = await PrazoAsync(c, n, true);
            var corridos = await PrazoAsync(c, n, false);
            for (var i = 0; i < 21; i++)
            {
                var d = inicio.AddDays(i);
                var p = (await PrevistaAsync(c, d.ToString("yyyy-MM-dd"), uteis.IdPrazos))!.Value;
                if (n == 0) Assert.Equal(Util(d) ? d : d.AddDays(d.DayOfWeek == DayOfWeek.Saturday ? 2 : 1), p); // fim de semana → segunda (09/11/2026)
                else
                {
                    Assert.True(Util(p), $"{d} + {n} úteis = {p} ({p.DayOfWeek})");
                    var contagem = Enumerable.Range(1, p.DayNumber - d.DayNumber).Count(k => Util(d.AddDays(k)));
                    Assert.True(contagem == n, $"{d} + {n} úteis = {p}: {contagem} dias úteis no intervalo");
                }
                Assert.Equal(d.AddDays(n), await PrevistaAsync(c, d.ToString("yyyy-MM-dd"), corridos.IdPrazos));
            }
        }
    }

    [Fact]
    public async Task Casos_de_calendario_virada_de_ano_bissexto_prazo_longo_nulos_e_prazo_inexistente()
    {
        await using var c = await Cenario.CriarAsync();
        var dois = await PrazoAsync(c, 2, true);
        var tres = await PrazoAsync(c, 3, true);
        var longo = await PrazoAsync(c, 3650, true); // máximo aceito pela tela (AuxiliarDefinicao: 0 a 3650)

        Assert.Equal(new DateOnly(2027, 1, 4), await PrevistaAsync(c, "2026-12-31", dois.IdPrazos));   // quinta + 2 = segunda
        Assert.Equal(new DateOnly(2028, 3, 1), await PrevistaAsync(c, "2028-02-25", tres.IdPrazos));   // sexta + 3, passa pelo 29/02
        Assert.Equal(new DateOnly(2026, 11, 9).AddDays(730 * 7), await PrevistaAsync(c, "2026-11-09", longo.IdPrazos)); // 3650 úteis = 730 semanas
        Assert.Null(await PrevistaAsync(c, null, dois.IdPrazos));
        Assert.Equal(new DateOnly(2026, 11, 9), await PrevistaAsync(c, "2026-11-09", null));
        Assert.Equal(new DateOnly(2026, 11, 9), await PrevistaAsync(c, "2026-11-09", int.MaxValue));
    }

    /// <summary>
    /// B-3 da auditoria: prazo negativo (só por SQL direto — a tela aceita 0 a 3650) conta como 0 dias nos dois modos.
    /// Antes, em dias corridos a data prevista ficava anterior à entrega e em dias úteis voltava a própria data.
    /// </summary>
    [Fact]
    public async Task Prazo_negativo_conta_como_zero_dias_uteis_ou_corridos()
    {
        await using var c = await Cenario.CriarAsync();
        var uteis = await PrazoAsync(c, -3, true);
        var corridos = await PrazoAsync(c, -3, false);
        Assert.Equal(new DateOnly(2026, 11, 11), await PrevistaAsync(c, "2026-11-11", uteis.IdPrazos));
        Assert.Equal(new DateOnly(2026, 11, 11), await PrevistaAsync(c, "2026-11-11", corridos.IdPrazos));
    }

    /// <summary>Os dois caminhos do banco: SP_AMOSTRA_GERAR_TESTES e a TG_TESTES_BI (inserção sem DT_PREVISTA).</summary>
    [Fact]
    public async Task Gerar_testes_e_trigger_usam_dias_uteis_do_prazo_da_analise()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var uteis = await PrazoAsync(c, 5, true);
        await c.Db.LabPropostasProdanalises.Where(x => x.IdLabPropostasProdutos == c.Item.IdLabPropostasProdutos)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.IdPrazos, uteis.IdPrazos));
        var amo = c.NovaAmostra(hoje);
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso);

        var esperado = hoje;
        for (var n = 5; n > 0;) { esperado = esperado.AddDays(1); if (Util(esperado)) n--; }
        var testes = await c.Db.LabHistAmostrasTestes.AsNoTracking().Where(t => t.IdLabHistAmostras == amo.IdLabHistAmostras).ToListAsync();
        Assert.Equal(2, testes.Count);
        Assert.All(testes, t => Assert.Equal(esperado, t.DtPrevista));

        var modelo = testes[0];
        var novo = new LabHistAmostraTeste
        {
            IdLabHistAmostras = amo.IdLabHistAmostras, IdLabParametrosAnalises = modelo.IdLabParametrosAnalises,
            IdAnalisesTipo = modelo.IdAnalisesTipo, IdAnalisesMetodos = modelo.IdAnalisesMetodos, IdIdiomas = modelo.IdIdiomas,
            IdPrazos = uteis.IdPrazos,
        };
        c.Db.Add(novo);
        await c.Db.SaveChangesAsync();
        Assert.Equal(esperado, await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostrasTestes == novo.IdLabHistAmostrasTestes).Select(t => t.DtPrevista).SingleAsync());
    }

    /// <summary>
    /// Boletim de outra empresa pelo id: prévia, impressão (2ª via do migrado) e documento não aparecem; a prévia continua
    /// distinguível (nome "Previa …"); a 2ª via do migrado sai com o nome da 1ª.
    /// </summary>
    [Fact]
    public async Task Segunda_via_e_previa_respeitam_a_empresa_e_a_previa_continua_distinguivel()
    {
        await using var a = await Cenario.CriarAsync();
        var (_, _, bol) = await a.AmostraComBoletimAsync(await a.HojeAsync());
        await a.Db.LabBoletimAnalises.Where(b => b.IdLabBoletimAnalises == bol.IdLabBoletimAnalises)
            .ExecuteUpdateAsync(u => u.SetProperty(b => b.DhEmissao, b => b.DhGeracao)); // migrado: emitido sem PDF
        var sa = Boletins(a);
        var via = await sa.ImprimirAsync(bol.IdLabBoletimAnalises);
        Assert.True(via.Sucesso, via.Mensagem);
        Assert.Equal(BoletimService.NomeArquivo(bol.CodBoletim, bol.Revisao), via.Valor.Nome);
        var previa = await sa.PreviaAsync(bol.IdLabBoletimAnalises);
        Assert.True(previa.Sucesso, previa.Mensagem);
        Assert.StartsWith("Previa ", previa.Valor.Nome);
        Assert.NotEqual(via.Valor.Nome, previa.Valor.Nome);

        await using var b = await Cenario.CriarAsync();
        var sb = Boletins(b);
        Assert.Equal(BoletimService.MensagemNaoEncontrado, (await sb.ImprimirAsync(bol.IdLabBoletimAnalises)).Mensagem);
        Assert.Equal(BoletimService.MensagemNaoEncontrado, (await sb.PreviaAsync(bol.IdLabBoletimAnalises)).Mensagem);
        Assert.Null(await sb.DocumentoAsync(bol.IdLabBoletimAnalises, LabControl.Services.Boletins.ModoBoletim.Emitido));
        Assert.False(Directory.Exists(_pasta));
    }

    /// <summary>O banco dos testes nunca é o LabControlDB e a limpeza só casa bancos temporários.</summary>
    [Fact]
    public void Banco_dos_testes_e_temporario_e_nunca_o_LabControlDB()
    {
        var cs = new MySqlConnector.MySqlConnectionStringBuilder(BancoTeste.ConnectionString);
        Assert.StartsWith(BancoTeste.Prefixo, cs.Database);
        Assert.NotEqual("labcontroldb", cs.Database.ToLowerInvariant());
        Assert.Matches("^lc_teste_[0-9]{14}_[0-9a-f]{4}$", BancoTeste.NomeBanco);
    }
}
