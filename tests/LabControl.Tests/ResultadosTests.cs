using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 5.2 (T26 resultados; P14/P15, RN-07): texto formatado pelo banco e exibido como no boletim, numérico ×
/// descritivo, teste de outra amostra e duplicado recusados, valor e datas, amostra que não pode receber resultado,
/// "Incluir abre o existente", boletim emitido exige revisão (que leva todos os valores e revisa o boletim), desfazer,
/// exclusões.
/// </summary>
public class ResultadosTests
{
    private static ResultadoService Servico(Cenario c) => new(c.Db, c.Ativa, c.Procedures, new AmostraService(c.Db, c.Ativa, c.Procedures));

    /// <summary>Amostra de hoje, com os 2 testes do cenário e status que permite resultado.</summary>
    private static async Task<LabHistAmostra> AmostraAsync(Cenario c, DateOnly entrega)
    {
        var a = c.NovaAmostra(entrega);
        a.IdAmostrasStatus = await c.Db.AmostrasStatus.Where(s => s.PermiteResultado).Select(s => s.IdAmostrasStatus).FirstAsync();
        c.Db.Add(a);
        await c.Db.SaveChangesAsync();
        Assert.True((await c.Procedures.AmostraGerarTestesAsync(a.IdLabHistAmostras)).Sucesso);
        return a;
    }

    /// <summary>Formulário novo com o teste acreditado numérico (&lt; 1,0 × 10² UFC/g) e o outro descritivo (Ausência em 25 g).</summary>
    private static async Task<ResultadoVM> LancadoAsync(Cenario c, ResultadoService s, LabHistAmostra a, DateOnly hoje)
    {
        var vm = (await s.NovoAsync(a.IdLabHistAmostras))!;
        var num = vm.Linhas.Single(l => l.Parametro == c.ParamAcreditado.Descricao);
        (num.Lancar, num.DtInicial, num.DtResultado, num.Numerico, num.Simbolo, num.Valor, num.Expoente, num.IdUnidade) =
            (true, hoje, hoje, true, "<", "1,0", 2, c.Unidade.IdUnidades);
        (num.EspecSimbolo, num.EspecValor, num.EspecExpoente, num.EspecIdUnidade) = ("≤", "1,0", 3, c.Unidade.IdUnidades);
        var desc = vm.Linhas.Single(l => l.Parametro == c.ParamDescritivo.Descricao);
        // descritivo com valor numérico digitado antes de trocar o tipo: o banco limpa
        (desc.Lancar, desc.DtInicial, desc.DtResultado, desc.Numerico, desc.Valor, desc.IdTipoResultado, desc.EspecNumerica, desc.EspecIdTipoResultado) =
            (true, hoje, hoje, false, "5", c.Ausencia.IdLabTiposResultados, false, c.Ausencia.IdLabTiposResultados);
        return vm;
    }

    [Fact]
    public async Task Lanca_numerico_e_descritivo_com_texto_do_banco_e_exibicao_do_boletim()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var a = await AmostraAsync(c, hoje);

        Assert.Null((await s.ResultadoDaAmostraAsync(a.IdLabHistAmostras)).Valor); // ainda sem resultado: formulário novo
        var vm = await LancadoAsync(c, s, a, hoje);
        Assert.Equal(2, vm.Linhas.Count);
        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);

        var linhas = await c.Db.LabResultadosParam.AsNoTracking().Where(p => p.IdLabResultadosAnalises == r.Valor).ToListAsync();
        var num = linhas.Single(p => p.TipoResultado);
        Assert.Equal(("< 1,0e2 UFC/g", "≤ 1,0e3 UFC/g"), (num.ResultadoFormatado, num.EspecifFormatada));
        Assert.Equal("< 1,0 × 10² UFC/g", ResultadoTexto.Impresso(num.ResultadoFormatado));
        var desc = linhas.Single(p => !p.TipoResultado);
        Assert.Equal(("Ausência em 25 g", (string?)null), (desc.ResultadoFormatado, desc.VrResultado));
        Assert.All(linhas, p => Assert.Equal(c.Ativa.ProfissionalId, p.IdEntidadesFuncDig));

        // "Incluir abre o existente" e a tela relê a prévia do banco
        Assert.Equal(r.Valor, (await s.ResultadoDaAmostraAsync(a.IdLabHistAmostras)).Valor);
        var aberto = (await s.CarregarAsync(r.Valor))!;
        Assert.Equal("< 1,0e2 UFC/g", aberto.Linhas.Single(l => l.Numerico).ResultadoFormatado);
        Assert.Null(aberto.Bloqueio);
        var denovo = await s.GravarAsync(await LancadoAsync(c, s, a, hoje));
        Assert.Contains("já tem resultado", denovo.Mensagem);

        // alteração: o valor muda e o texto é refeito pelo banco
        aberto.Linhas.Single(l => l.Numerico).Valor = "3,5";
        aberto.Linhas.Single(l => l.Numerico).Expoente = 0;
        Assert.True((await s.GravarAsync(aberto)).Sucesso);
        Assert.Equal("< 3,5 UFC/g", await c.Db.LabResultadosParam.Where(p => p.IdLabResultadosAnalises == r.Valor && p.TipoResultado)
            .Select(p => p.ResultadoFormatado).SingleAsync());
    }

    [Fact]
    public async Task Recusa_valor_datas_teste_de_outra_amostra_e_amostra_sem_status_de_resultado()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var a = await AmostraAsync(c, hoje.AddDays(-2));

        async Task<Resultado<int>> Com(Action<ResultadoLinhaVM> mudar)
        {
            var vm = await LancadoAsync(c, s, a, hoje);
            mudar(vm.Linhas.Single(l => l.Numerico && l.Lancar));
            return await s.GravarAsync(vm);
        }
        foreach (var valor in new[] { "3,5,1", "abc", "3.5" })
            Assert.Contains("não é um número", (await Com(l => l.Valor = valor)).Mensagem);
        Assert.Contains("informe o valor", (await Com(l => l.Valor = " ")).Mensagem);
        Assert.Contains("símbolo", (await Com(l => l.Simbolo = "=")).Mensagem);
        Assert.Contains("antes da entrega", (await Com(l => l.DtInicial = hoje.AddDays(-3))).Mensagem);
        Assert.Contains("antes da data de início", (await Com(l => (l.DtInicial, l.DtResultado) = (hoje, hoje.AddDays(-1)))).Mensagem);
        Assert.Contains("depois de hoje", (await Com(l => l.DtResultado = hoje.AddDays(1))).Mensagem);
        var campo = await Com(l => l.Valor = "x");
        Assert.Matches(@"^Linhas\[\d\]\.Valor$", campo.Campo);
        // descritivo sem tipo
        var semTipo = await LancadoAsync(c, s, a, hoje);
        semTipo.Linhas.Single(l => !l.Numerico).IdTipoResultado = null;
        Assert.Contains("tipo de resultado", (await s.GravarAsync(semTipo)).Mensagem);

        // teste de outra amostra: recusado pelo serviço e, direto no banco, pela trigger
        var outra = await AmostraAsync(c, hoje);
        var vm = await LancadoAsync(c, s, a, hoje);
        var testeAlheio = await c.Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == outra.IdLabHistAmostras).Select(t => t.IdLabHistAmostrasTestes).FirstAsync();
        vm.Linhas[0].IdTeste = testeAlheio;
        Assert.Equal("O parâmetro não pertence à amostra do resultado.", (await s.GravarAsync(vm)).Mensagem);
        var ok = await s.GravarAsync(await LancadoAsync(c, s, a, hoje));
        Assert.True(ok.Sucesso, ok.Mensagem);
        var banco = await ErrosMySql.CapturarAsync(async () =>
        {
            c.Db.Add(new LabResultadoParam { IdLabResultadosAnalises = ok.Valor, IdLabHistAmostrasTestes = testeAlheio, DtInicial = hoje, DtResultado = hoje, VrResultado = "1" });
            await c.Db.SaveChangesAsync();
        });
        Assert.Equal("O parâmetro não pertence à amostra do resultado.", banco.Mensagem);
        c.Db.ChangeTracker.Clear();
        // o mesmo teste duas vezes no resultado: mensagem amigável da UNIQUE
        var testeDaAmostra = await c.Db.LabResultadosParam.Where(p => p.IdLabResultadosAnalises == ok.Valor).Select(p => p.IdLabHistAmostrasTestes).FirstAsync();
        var duplicado = await ErrosMySql.CapturarAsync(async () =>
        {
            c.Db.Add(new LabResultadoParam { IdLabResultadosAnalises = ok.Valor, IdLabHistAmostrasTestes = testeDaAmostra, DtInicial = hoje, DtResultado = hoje, VrResultado = "1" });
            await c.Db.SaveChangesAsync();
        });
        Assert.Equal("Este parâmetro já tem resultado nesta revisão.", duplicado.Mensagem);
        c.Db.ChangeTracker.Clear();

        // amostra cujo status não permite resultado
        var bloqueada = c.NovaAmostra(hoje);
        bloqueada.IdAmostrasStatus = await c.Db.AmostrasStatus.Where(st => !st.PermiteResultado).Select(st => st.IdAmostrasStatus).FirstAsync();
        c.Db.Add(bloqueada);
        await c.Db.SaveChangesAsync();
        Assert.Contains("não permite lançar resultado", (await s.ResultadoDaAmostraAsync(bloqueada.IdLabHistAmostras)).Mensagem);
    }

    [Fact]
    public async Task Boletim_emitido_exige_revisao_que_leva_os_valores_e_revisa_o_boletim_e_desfazer()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var (_, res, bol) = await c.AmostraComBoletimAsync(hoje);
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "b.pdf", new string('c', 64))).Sucesso);
        c.Db.ChangeTracker.Clear();

        var vm = (await s.CarregarAsync(res.IdLabResultadosAnalises))!;
        Assert.Equal(ResultadoService.MensagemBoletimEmitido, vm.Bloqueio);
        Assert.True(vm.Linhas.Single(l => l.Numerico).NoBoletim);
        vm.Linhas.Single(l => l.Numerico).Valor = "2,0";
        Assert.Equal(ResultadoService.MensagemBoletimEmitido, (await s.GravarAsync(vm)).Mensagem);
        // linha em boletim não se exclui; desfazer sem ser revisão também não
        Assert.Contains("boletim", (await s.ExcluirLinhaAsync(res.IdLabResultadosAnalises, vm.Linhas.Single(l => l.Numerico).IdParam)).Mensagem);
        Assert.Contains("não é uma revisão", (await s.DesfazerRevisaoAsync(res.IdLabResultadosAnalises)).Mensagem);
        Assert.Contains("motivo", (await s.GerarRevisaoAsync(res.IdLabResultadosAnalises, " ", true)).Mensagem);

        // revisão: leva todos os valores; o boletim novo aponta para o resultado novo
        var rev = await s.GerarRevisaoAsync(res.IdLabResultadosAnalises, "Valor digitado errado", true);
        Assert.True(rev.Sucesso, rev.Mensagem);
        c.Db.ChangeTracker.Clear();
        var nova = (await s.CarregarAsync(rev.Valor))!;
        Assert.Equal((1, null, true), (nova.Revisao, nova.Bloqueio, nova.TemBoletim));
        Assert.Equal("< 1,0e2 UFC/g | Ausência em 25 g", string.Join(" | ", nova.Linhas.Select(l => l.ResultadoFormatado).Order()));
        Assert.Equal(ResultadoService.MensagemRevisaoAntiga, (await s.CarregarAsync(res.IdLabResultadosAnalises))!.Bloqueio);
        Assert.Equal(rev.Valor, await c.Db.LabBoletimAnalises.Where(b => b.UltimaRevisao && b.CodBoletim == bol.CodBoletim).Select(b => b.IdLabResultadosAnalises).SingleAsync());
        nova.Linhas.Single(l => l.Numerico).Valor = "2,0";
        Assert.True((await s.GravarAsync(nova)).Sucesso);

        // desfazer com boletim: recusado; sem boletim a anterior volta a ser a vigente
        Assert.False((await s.DesfazerRevisaoAsync(rev.Valor)).Sucesso);
        var semBoletim = await s.GerarRevisaoAsync(rev.Valor, "Outra correção", false);
        Assert.True(semBoletim.Sucesso, semBoletim.Mensagem);
        var desfeito = await s.DesfazerRevisaoAsync(semBoletim.Valor);
        Assert.True(desfeito.Sucesso, desfeito.Mensagem);
        Assert.Equal(rev.Valor, desfeito.Valor);
        Assert.True(await c.Db.LabResultadosAnalises.Where(r => r.IdLabResultadosAnalises == rev.Valor).Select(r => r.UltimaRevisao).SingleAsync());
        Assert.False(await c.Db.LabResultadosAnalises.AnyAsync(r => r.IdLabResultadosAnalises == semBoletim.Valor));
    }

    [Fact]
    public async Task Exclui_parametro_e_todos_da_revisao_zero_sem_boletim()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var hoje = await c.HojeAsync();
        var a = await AmostraAsync(c, hoje);
        var r = await s.GravarAsync(await LancadoAsync(c, s, a, hoje));
        Assert.True(r.Sucesso, r.Mensagem);

        var vm = (await s.CarregarAsync(r.Valor))!;
        Assert.True((await s.ExcluirLinhaAsync(r.Valor, vm.Linhas.Single(l => !l.Numerico).IdParam)).Sucesso);
        vm = (await s.CarregarAsync(r.Valor))!;
        Assert.Equal(1, vm.Linhas.Count(l => l.IdParam == 0)); // voltou a ficar pendente (e na fila)
        Assert.True(await c.Db.VwLabTestesPendentes.AnyAsync(t => t.IdLabHistAmostras == a.IdLabHistAmostras));

        Assert.True((await s.ExcluirTodosAsync(r.Valor)).Sucesso);
        Assert.False(await c.Db.LabResultadosAnalises.AnyAsync(x => x.IdLabResultadosAnalises == r.Valor));
        Assert.Equal(2, await c.Db.VwLabTestesPendentes.CountAsync(t => t.IdLabHistAmostras == a.IdLabHistAmostras));

        // com boletim (mesmo não emitido) não se exclui tudo
        var (_, res, _) = await c.AmostraComBoletimAsync(hoje);
        Assert.Contains("boletim", (await s.ExcluirTodosAsync(res.IdLabResultadosAnalises)).Mensagem);
    }

    [Theory]
    [InlineData("< 1,0e2 UFC/g", "< 1,0 × 10² UFC/g")]
    [InlineData("3,5e-3 mg/L", "3,5 × 10⁻³ mg/L")]
    [InlineData("3,5 mg/L", "3,5 mg/L")]
    [InlineData("Ausência em 25 g", "Ausência em 25 g")]
    [InlineData("≥ 12e10", "≥ 12 × 10¹⁰")]
    public void Texto_do_banco_vira_o_impresso_no_boletim(string banco, string impresso) =>
        Assert.Equal(impresso, ResultadoTexto.Impresso(banco));
}
