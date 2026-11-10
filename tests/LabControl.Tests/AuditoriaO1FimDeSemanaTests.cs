using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Auditoria SABOTADOR da O-1 (09/11/2026): prazo de 0 dias úteis com entrega no sábado ou domingo vai para a
/// segunda-feira — pelos dois caminhos do banco (SP_AMOSTRA_GERAR_TESTES e TG_TESTES_BI); 0 dias corridos continua
/// na própria data. Proteção contra regressão (passa hoje).
/// </summary>
public class AuditoriaO1FimDeSemanaTests
{
    private static async Task<Prazo> PrazoAsync(Cenario c, int dias, bool uteis)
    {
        var p = new Prazo { Descricao = $"O1 {dias} {(uteis ? "U" : "C")} {Guid.NewGuid():N}"[..30], QtdeDias = dias, DiasUteis = uteis };
        c.Db.Add(p);
        await c.Db.SaveChangesAsync();
        return p;
    }

    // o sábado (ou domingo) mais recente até hoje: no máximo 6 dias atrás, dentro do limite retroativo padrão (7)
    private static DateOnly Ultimo(DateOnly hoje, DayOfWeek dia) => hoje.AddDays(-(((int)hoje.DayOfWeek - (int)dia + 7) % 7));

    private static DateOnly Segunda(DateOnly d) => d.AddDays(d.DayOfWeek == DayOfWeek.Saturday ? 2 : 1);

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public async Task Zero_dia_util_no_fim_de_semana_vai_para_segunda_pela_procedure_e_pela_trigger(DayOfWeek dia)
    {
        await using var c = await Cenario.CriarAsync();
        var entrega = Ultimo(await c.HojeAsync(), dia);
        var uteis = await PrazoAsync(c, 0, true);
        var corridos = await PrazoAsync(c, 0, false);
        await c.Db.LabPropostasProdanalises.Where(x => x.IdLabPropostasProdutos == c.Item.IdLabPropostasProdutos)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.IdPrazos, uteis.IdPrazos));
        var amo = c.NovaAmostra(entrega);
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso);

        var testes = await c.Db.LabHistAmostrasTestes.AsNoTracking().Where(t => t.IdLabHistAmostras == amo.IdLabHistAmostras).ToListAsync();
        Assert.NotEmpty(testes);
        Assert.All(testes, t => Assert.Equal(Segunda(entrega), t.DtPrevista));

        // trigger: inserção sem DT_PREVISTA, com 0 úteis (segunda) e com 0 corridos (a própria data)
        var modelo = testes[0];
        LabHistAmostraTeste Novo(int prazo) => new()
        {
            IdLabHistAmostras = amo.IdLabHistAmostras, IdLabParametrosAnalises = modelo.IdLabParametrosAnalises,
            IdAnalisesTipo = modelo.IdAnalisesTipo, IdAnalisesMetodos = modelo.IdAnalisesMetodos, IdIdiomas = modelo.IdIdiomas,
            IdPrazos = prazo,
        };
        var tu = Novo(uteis.IdPrazos);
        var tc = Novo(corridos.IdPrazos);
        c.Db.AddRange(tu, tc);
        await c.Db.SaveChangesAsync();
        var lidas = await c.Db.LabHistAmostrasTestes.AsNoTracking()
            .Where(t => t.IdLabHistAmostrasTestes == tu.IdLabHistAmostrasTestes || t.IdLabHistAmostrasTestes == tc.IdLabHistAmostrasTestes)
            .ToDictionaryAsync(t => t.IdLabHistAmostrasTestes, t => t.DtPrevista);
        Assert.Equal(Segunda(entrega), lidas[tu.IdLabHistAmostrasTestes]);
        Assert.Equal(entrega, lidas[tc.IdLabHistAmostrasTestes]);
    }
}
