using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// 5.5 — cenários de aceite da Fase 5 que faltavam, pela aplicação (T22 → T26 → T27): a cascata completa do roteiro
/// 12 §7 itens 6–9 a partir da revisão da <b>amostra</b>, a amostra travada pelo boletim emitido (P17) e o caso dos 120
/// boletins do legado com dois boletins para o mesmo resultado (P18). Os demais estão em <see cref="ResultadosTests"/>,
/// <see cref="BoletinsTests"/>, <see cref="MigracaoTests"/> e no navegador.
/// </summary>
public class AceiteFase5Tests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "lc-aceite5-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true);
        GC.SuppressFinalize(this);
    }

    private sealed record Servicos(AmostraService Amostras, ResultadoService Resultados, BoletimService Boletins);

    private Servicos Criar(Cenario c)
    {
        var amostras = new AmostraService(c.Db, c.Ativa, c.Procedures);
        return new(amostras, new ResultadoService(c.Db, c.Ativa, c.Procedures, amostras),
            new BoletimService(c.Db, c.Ativa, c.Procedures, amostras, new ArmazemBoletins(_pasta), NullLogger<BoletimService>.Instance));
    }

    /// <summary>Amostra (status que permite resultado) → resultado pela T26 (numérico + descritivo) → boletim pela T27 com os dois, emitido.</summary>
    private static async Task<(LabHistAmostra Amostra, int Resultado, int Boletim)> EmitidoAsync(Cenario c, Servicos s)
    {
        await c.Db.EntidadesFuncionarios.Where(f => f.IdEntidades == c.Ativa.ProfissionalId).ExecuteUpdateAsync(u => u.SetProperty(f => f.AssinaBoletim, true));
        var hoje = await c.HojeAsync();
        var a = c.NovaAmostra(hoje);
        a.IdAmostrasStatus = await c.Db.AmostrasStatus.Where(st => st.PermiteResultado).Select(st => st.IdAmostrasStatus).FirstAsync();
        c.Db.Add(a);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(a.IdLabHistAmostras)).Sucesso);

        var vm = (await s.Resultados.NovoAsync(a.IdLabHistAmostras))!;
        foreach (var l in vm.Linhas)
        {
            (l.Lancar, l.DtInicial, l.DtResultado) = (true, hoje, hoje);
            if (l.Acreditado) (l.Simbolo, l.Valor, l.Expoente, l.IdUnidade) = ("<", "1,0", 2, c.Unidade.IdUnidades);
            else (l.Numerico, l.IdTipoResultado) = (false, c.Ausencia.IdLabTiposResultados);
        }
        var res = await s.Resultados.GravarAsync(vm);
        Assert.True(res.Sucesso, res.Mensagem);

        var bvm = (await s.Boletins.NovoAsync(a.IdLabHistAmostras))!;
        (bvm.Selecionados, bvm.IdResponsavel1) = (bvm.Disponiveis.Select(d => d.IdParam).ToList(), c.Ativa.ProfissionalId);
        var bol = await s.Boletins.GravarAsync(bvm);
        Assert.True(bol.Sucesso, bol.Mensagem);
        var emitir = await s.Boletins.EmitirAsync(bol.Valor);
        Assert.True(emitir.Sucesso, emitir.Mensagem);
        c.Db.ChangeTracker.Clear();
        return (a, res.Valor, bol.Valor);
    }

    [Fact]
    public async Task Cascata_pela_revisao_da_amostra_gera_resultado_e_boletim_novos_que_se_corrigem_e_emitem()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Criar(c);
        var (a, res, bol) = await EmitidoAsync(c, s);
        var pdfAntigo = (await s.Boletins.ImprimirAsync(bol)).Valor.Conteudo;

        // P17: com boletim emitido, a amostra não muda (serviço e banco)
        var amostra = (await s.Amostras.CarregarAsync(a.IdLabHistAmostras))!;
        Assert.Equal("Amostra com boletim emitido: para alterar, gere uma revisão da amostra.", amostra.Bloqueio);
        var direto = await ErrosMySql.CapturarAsync(() => c.Db.Database.ExecuteSqlRawAsync(
            "UPDATE LAB_HIST_AMOSTRAS SET NR_LOTE = 'X' WHERE ID_LAB_HIST_AMOSTRAS = {0}", a.IdLabHistAmostras));
        Assert.Equal("Amostra com boletim emitido: gere uma revisão da amostra.", direto.Mensagem);

        // P11 → P15 → P18: a revisão da amostra revisa o resultado e o boletim, apontando para os novos
        var revAmostra = await s.Amostras.GerarRevisaoAsync(a.IdLabHistAmostras, "Lote digitado errado");
        Assert.True(revAmostra.Sucesso, revAmostra.Mensagem);
        c.Db.ChangeTracker.Clear();
        var resNovo = (await s.Resultados.ResultadoDaAmostraAsync(revAmostra.Valor)).Valor!.Value;
        Assert.NotEqual(res, resNovo);
        var resultado = (await s.Resultados.CarregarAsync(resNovo))!;
        Assert.Equal((1, (string?)null), (resultado.Revisao, resultado.Bloqueio));
        Assert.Equal(["< 1,0e2 UFC/g", "Ausência em 25 g"], resultado.Linhas.Select(l => l.ResultadoFormatado!).Order());
        var bolNovo = (await s.Boletins.BoletimDaAmostraAsync(revAmostra.Valor)).Valor!.Value;
        var boletim = (await s.Boletins.CarregarAsync(bolNovo))!;
        Assert.Equal((1, (DateTime?)null, resNovo, revAmostra.Valor), (boletim.Revisao, boletim.DhEmissao, boletim.IdResultado, boletim.IdAmostra));
        Assert.Equal(2, boletim.Parametros.Count);

        // corrige o resultado novo, atualiza os textos do boletim novo e emite: R01 com o valor novo
        resultado.Linhas.Single(l => l.Acreditado).Valor = "2,5";
        Assert.True((await s.Resultados.GravarAsync(resultado)).Sucesso);
        Assert.True((await s.Boletins.AtualizarTextosAsync(bolNovo)).Sucesso);
        Assert.Contains("< 2,5e2 UFC/g", (await s.Boletins.CarregarAsync(bolNovo))!.Parametros.Select(p => p.Resultado));
        Assert.True((await s.Boletins.EmitirAsync(bolNovo)).Sucesso);
        c.Db.ChangeTracker.Clear();
        Assert.EndsWith(" R01.pdf", await c.Db.LabBoletimAnalises.Where(b => b.IdLabBoletimAnalises == bolNovo).Select(b => b.ArquivoPdf).SingleAsync());

        // o histórico não muda: o boletim antigo continua emitido, com o PDF de antes, e não se desfaz o emitido
        Assert.Equal(pdfAntigo, (await s.Boletins.ImprimirAsync(bol)).Valor.Conteudo);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.Boletins.DesfazerRevisaoAsync(bolNovo)).Mensagem);
        Assert.Equal(ResultadoService.MensagemRevisaoAntiga, (await s.Resultados.CarregarAsync(res))!.Bloqueio);
    }

    [Fact]
    public async Task Dois_boletins_para_o_mesmo_resultado_revisam_sem_erro()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Criar(c);
        var (a, res, bol) = await EmitidoAsync(c, s);

        // revisão só do boletim: o resultado é o mesmo — o formato dos 120 casos do legado
        var rev1 = await s.Boletins.GerarRevisaoAsync(bol, "Responsável trocado");
        Assert.True(rev1.Sucesso, rev1.Mensagem);
        Assert.Equal(2, await c.Db.LabBoletimAnalises.CountAsync(b => b.IdLabResultadosAnalises == res));
        Assert.True((await s.Boletins.EmitirAsync(rev1.Valor)).Sucesso);

        // o legado quebrava aqui com "multiple rows": revisar de novo o boletim e revisar o resultado com o boletim
        var rev2 = await s.Boletins.GerarRevisaoAsync(rev1.Valor, "Observação incompleta");
        Assert.True(rev2.Sucesso, rev2.Mensagem);
        Assert.True((await s.Boletins.EmitirAsync(rev2.Valor)).Sucesso);
        var revRes = await s.Resultados.GerarRevisaoAsync(res, "Valor corrigido", true);
        Assert.True(revRes.Sucesso, revRes.Mensagem);
        c.Db.ChangeTracker.Clear();
        var vigente = (await s.Boletins.BoletimDaAmostraAsync(a.IdLabHistAmostras)).Valor!.Value;
        var b = (await s.Boletins.CarregarAsync(vigente))!;
        Assert.Equal((3, revRes.Valor, (DateTime?)null), (b.Revisao, b.IdResultado, b.DhEmissao));
        Assert.Equal(1, await c.Db.LabBoletimAnalises.CountAsync(x => x.CodBoletim == b.CodBoletim && x.UltimaRevisao));
    }
}
