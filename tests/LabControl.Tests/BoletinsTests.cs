using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 5.3 (T27 boletim; P16/P17/P18, RN-08): só os parâmetros marcados, na ordem; metodologias automáticas;
/// emissão com PDF + SHA-256 e tudo congelado depois; reimpressão idêntica e alerta de integridade; arquivo apagado se
/// a emissão falha; revisão não emitida com o resultado vigente e os mesmos parâmetros; desfazer; 2ª via do migrado.
/// </summary>
public class BoletinsTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "lc-boletins-" + Guid.NewGuid().ToString("N")[..8]);

    public void Dispose()
    {
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true);
        GC.SuppressFinalize(this);
    }

    private BoletimService Servico(Cenario c) =>
        new(c.Db, c.Ativa, c.Procedures, new AmostraService(c.Db, c.Ativa, c.Procedures), new ArmazemBoletins(_pasta), NullLogger<BoletimService>.Instance);

    private static ResultadoService Resultados(Cenario c) => new(c.Db, c.Ativa, c.Procedures, new AmostraService(c.Db, c.Ativa, c.Procedures));

    /// <summary>O profissional do usuário assina boletim (Responsável 1 dos testes).</summary>
    private static Task AssinaAsync(Cenario c) =>
        c.Db.EntidadesFuncionarios.Where(f => f.IdEntidades == c.Ativa.ProfissionalId).ExecuteUpdateAsync(s => s.SetProperty(f => f.AssinaBoletim, true));

    /// <summary>Boletim novo gravado pela tela com os parâmetros marcados.</summary>
    private static async Task<int> CriarAsync(Cenario c, BoletimService s, int idAmostra, params int[] marcados)
    {
        var vm = (await s.NovoAsync(idAmostra))!;
        (vm.Selecionados, vm.IdResponsavel1) = ([.. marcados], c.Ativa.ProfissionalId);
        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        return r.Valor;
    }

    [Fact]
    public async Task Monta_so_os_marcados_na_ordem_com_metodologias_e_abre_o_existente()
    {
        await using var c = await Cenario.CriarAsync();
        await AssinaAsync(c);
        var s = Servico(c);
        var (amo, _, num, desc) = await c.AmostraComResultadoAsync(await c.HojeAsync());

        Assert.Null((await s.BoletimDaAmostraAsync(amo.IdLabHistAmostras)).Valor);
        var vm = (await s.NovoAsync(amo.IdLabHistAmostras))!;
        Assert.Equal("CONT. TOTAL: ISO 4833", vm.MetodologiaAcreditada);
        Assert.Equal("SALMONELLA: ISO 6579", vm.MetodologiaNaoAcreditada);
        Assert.Equal(["CONT. TOTAL", "SALMONELLA"], vm.Disponiveis.Select(d => d.Parametro)); // acreditados primeiro
        Assert.Equal("Nenhum Parâmetro foi selecionado.", (await s.GravarAsync(vm)).Mensagem);

        // só o descritivo marcado: entra só ele, com o texto congelado; o código é o número base da amostra
        var id = await CriarAsync(c, s, amo.IdLabHistAmostras, desc.IdLabResultadosParam);
        var aberto = (await s.CarregarAsync(id))!;
        Assert.Equal(amo.NrAmostra, aberto.CodBoletim);
        Assert.Equal(("SALMONELLA", (string?)null, "Ausência em 25 g"), (aberto.Parametros.Single().Parametro, aberto.Parametros.Single().Resultado, aberto.Parametros.Single().Descritivo));
        Assert.Equal(["CONT. TOTAL"], aberto.Disponiveis.Select(d => d.Parametro));
        Assert.Equal(id, (await s.BoletimDaAmostraAsync(amo.IdLabHistAmostras)).Valor); // "Incluir abre o existente"
        Assert.Contains("já tem boletim", (await s.GravarAsync((await s.NovoAsync(amo.IdLabHistAmostras))!)).Mensagem);

        // "Concluir seleção" acrescenta no fim; ▲ leva para o topo
        Assert.True((await s.IncluirParametrosAsync(id, [num.IdLabResultadosParam])).Sucesso);
        aberto = (await s.CarregarAsync(id))!;
        Assert.Equal(["SALMONELLA", "CONT. TOTAL"], aberto.Parametros.Select(p => p.Parametro));
        Assert.Equal("< 1,0e2 UFC/g", aberto.Parametros[1].Resultado);
        Assert.True((await s.MoverAsync(id, aberto.Parametros[1].Id, -1)).Sucesso);
        Assert.Equal(["CONT. TOTAL", "SALMONELLA"], (await s.CarregarAsync(id))!.Parametros.Select(p => p.Parametro));

        // parâmetro de outro resultado: recusado
        var (_, _, outro, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        Assert.Equal("O parâmetro não pertence ao resultado do boletim.", (await s.IncluirParametrosAsync(id, [outro.IdLabResultadosParam])).Mensagem);
    }

    [Fact]
    public async Task Emite_com_pdf_e_hash_congela_tudo_e_reimprime_o_arquivo_gravado()
    {
        await using var c = await Cenario.CriarAsync();
        await AssinaAsync(c);
        var s = Servico(c);
        var (amo, res, num, desc) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        var id = await CriarAsync(c, s, amo.IdLabHistAmostras, num.IdLabResultadosParam, desc.IdLabResultadosParam);

        // pré-requisitos da emissão
        var vm = (await s.CarregarAsync(id))!;
        vm.IdResponsavel1 = null;
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        Assert.Equal("Informe o(s) responsável(is) técnico(s) do boletim.", (await s.EmitirAsync(id)).Mensagem);
        vm.IdResponsavel1 = c.Ativa.ProfissionalId;
        vm.Observacao = "Amostra recebida lacrada.";
        Assert.True((await s.GravarAsync(vm)).Sucesso);

        var previa = await s.PreviaAsync(id);
        Assert.True(previa.Sucesso, previa.Mensagem);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(previa.Valor.Conteudo, 0, 4));
        Assert.Empty(Directory.Exists(_pasta) ? Directory.GetFiles(_pasta, "*", SearchOption.AllDirectories) : []); // prévia não grava

        var emitir = await s.EmitirAsync(id);
        Assert.True(emitir.Sucesso, emitir.Mensagem);
        c.Db.ChangeTracker.Clear();
        var b = await c.Db.LabBoletimAnalises.AsNoTracking().SingleAsync(x => x.IdLabBoletimAnalises == id);
        Assert.NotNull(b.DhEmissao);
        Assert.Equal(c.Ativa.ProfissionalId, b.IdEntidadesFuncEmi);
        Assert.Equal(await c.Db.EmpresasParametros.Select(p => p.IdBoletinsStatusEmitido).SingleAsync(), b.IdBoletinsStatus);
        var arquivo = Path.Combine(_pasta, b.ArquivoPdf!);
        Assert.Equal(ArmazemBoletins.CaminhoRelativo(c.Empresa.CodEmpresa, (await c.HojeAsync()).Year, b.CodBoletim, 0), b.ArquivoPdf);
        Assert.Equal(amo.NrAmostra, b.CodBoletim);
        var gravado = await File.ReadAllBytesAsync(arquivo);
        Assert.Equal(ArmazemBoletins.Hash(gravado), b.HashPdf);
        Assert.Equal("Boletim já emitido.", (await c.Procedures.EmitirBoletimAsync(id, "x.pdf", new string('0', 64))).Mensagem);

        // depois de emitido: só o status muda; parâmetros, exclusão, emissão e o resultado vinculado recusados
        var emitido = (await s.CarregarAsync(id))!;
        Assert.Equal(BoletimService.MensagemEmitido, emitido.Bloqueio);
        emitido.Observacao = "mudou";
        Assert.True((await s.GravarAsync(emitido)).Sucesso);
        Assert.Equal("Amostra recebida lacrada.", await c.Db.LabBoletimAnalises.Where(x => x.IdLabBoletimAnalises == id).Select(x => x.TxtObservacao).SingleAsync());
        Assert.Equal(BoletimService.MensagemEmitido, (await s.IncluirParametrosAsync(id, [num.IdLabResultadosParam])).Mensagem);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.ExcluirParametroAsync(id, emitido.Parametros[0].Id)).Mensagem);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.MoverAsync(id, emitido.Parametros[0].Id, 1)).Mensagem);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.AtualizarTextosAsync(id)).Mensagem);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.ExcluirAsync(id)).Mensagem);
        var r = (await Resultados(c).CarregarAsync(res.IdLabResultadosAnalises))!;
        Assert.Equal(ResultadoService.MensagemBoletimEmitido, (await Resultados(c).GravarAsync(r)).Mensagem);
        var direto = await ErrosMySql.CapturarAsync(() => c.Db.Database.ExecuteSqlRawAsync(
            "UPDATE LAB_BOLETIM_ANALISES SET TXT_LEGENDA = 'x' WHERE ID_LAB_BOLETIM_ANALISES = {0}", id));
        Assert.Equal(BoletimService.MensagemEmitido, direto.Mensagem);

        // reimpressão = o arquivo gravado, byte a byte; adulterado ou sumido → alerta de integridade
        var impresso = await s.ImprimirAsync(id);
        Assert.True(impresso.Sucesso, impresso.Mensagem);
        Assert.Equal(gravado, impresso.Valor.Conteudo);
        await File.WriteAllBytesAsync(arquivo, [.. gravado, 0x20]);
        Assert.Contains("ALERTA DE INTEGRIDADE", (await s.ImprimirAsync(id)).Mensagem);
        File.Delete(arquivo);
        Assert.Contains("não foi encontrado", (await s.ImprimirAsync(id)).Mensagem);
    }

    [Fact]
    public async Task Falha_na_emissao_apaga_o_pdf_gerado()
    {
        await using var c = await Cenario.CriarAsync();
        await AssinaAsync(c);
        var s = Servico(c);
        var (amo, _, num, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        var id = await CriarAsync(c, s, amo.IdLabHistAmostras, num.IdLabResultadosParam);

        // emissor que não existe: o PDF é gravado antes e a procedure falha (erro técnico, que sobe) — o arquivo não fica
        var real = c.Ativa.ProfissionalId;
        c.Ativa.ProfissionalId = int.MaxValue;
        await Assert.ThrowsAnyAsync<MySqlConnector.MySqlException>(() => s.EmitirAsync(id));
        c.Ativa.ProfissionalId = real;
        Assert.Empty(Directory.GetFiles(_pasta, "*.pdf", SearchOption.AllDirectories));
        Assert.Null(await c.Db.LabBoletimAnalises.Where(x => x.IdLabBoletimAnalises == id).Select(x => x.DhEmissao).SingleAsync());
    }

    [Fact]
    public async Task Revisao_nasce_nao_emitida_com_o_resultado_vigente_e_os_mesmos_parametros_e_desfazer()
    {
        await using var c = await Cenario.CriarAsync();
        await AssinaAsync(c);
        var s = Servico(c);
        var (amo, res, num, _) = await c.AmostraComResultadoAsync(await c.HojeAsync());
        var id = await CriarAsync(c, s, amo.IdLabHistAmostras, num.IdLabResultadosParam);
        Assert.True((await s.EmitirAsync(id)).Sucesso);

        // o resultado é revisado sem o boletim; a revisão do boletim aponta para ele
        var revRes = await Resultados(c).GerarRevisaoAsync(res.IdLabResultadosAnalises, "Corrigir valor", false);
        Assert.True(revRes.Sucesso, revRes.Mensagem);
        Assert.Contains("motivo", (await s.GerarRevisaoAsync(id, "")).Mensagem);
        var rev = await s.GerarRevisaoAsync(id, "Valor corrigido no resultado");
        Assert.True(rev.Sucesso, rev.Mensagem);
        c.Db.ChangeTracker.Clear();
        var nova = (await s.CarregarAsync(rev.Valor))!;
        Assert.Equal((1, (DateTime?)null, (string?)null, revRes.Valor), (nova.Revisao, nova.DhEmissao, nova.Bloqueio, nova.IdResultado));
        Assert.Equal(["CONT. TOTAL"], nova.Parametros.Select(p => p.Parametro));
        Assert.False((await s.CarregarAsync(id))!.UltimaRevisao); // a anterior (emitida) continua travada
        Assert.Equal(rev.Valor, (await s.BoletimDaAmostraAsync(amo.IdLabHistAmostras)).Valor);

        // desfazer a revisão não emitida reativa a anterior; a emitida não se desfaz
        var desfeito = await s.DesfazerRevisaoAsync(rev.Valor);
        Assert.True(desfeito.Sucesso, desfeito.Mensagem);
        Assert.Equal(id, desfeito.Valor);
        Assert.True(await c.Db.LabBoletimAnalises.Where(x => x.IdLabBoletimAnalises == id).Select(x => x.UltimaRevisao).SingleAsync());
        var outra = await s.GerarRevisaoAsync(id, "De novo");
        Assert.True((await s.EmitirAsync(outra.Valor)).Sucesso);
        Assert.Equal(BoletimService.MensagemEmitido, (await s.DesfazerRevisaoAsync(outra.Valor)).Mensagem);
    }

    [Fact]
    public async Task Boletim_migrado_sem_arquivo_imprime_segunda_via_sem_gravar()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (_, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        Assert.Contains("ainda não emitido", (await s.ImprimirAsync(bol.IdLabBoletimAnalises)).Mensagem);
        // como a M-17 deixa o histórico: emitido, sem PDF
        await c.Db.LabBoletimAnalises.Where(b => b.IdLabBoletimAnalises == bol.IdLabBoletimAnalises)
            .ExecuteUpdateAsync(u => u.SetProperty(b => b.DhEmissao, b => b.DhGeracao));

        var via = await s.ImprimirAsync(bol.IdLabBoletimAnalises);
        Assert.True(via.Sucesso, via.Mensagem);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(via.Valor.Conteudo, 0, 4));
        Assert.StartsWith("2a via", via.Valor.Nome);
        Assert.False(Directory.Exists(_pasta));
    }

    /// <summary>
    /// /code-review 5.5: pasta configurada com a barra no fim funciona; duas emissões ao mesmo tempo gravam em provisórios
    /// diferentes, e a que falha apaga só o seu — o arquivo confirmado continua.
    /// </summary>
    [Fact]
    public async Task Armazem_aceita_barra_no_fim_e_provisorios_nao_se_atropelam()
    {
        var armazem = new ArmazemBoletins(_pasta + Path.DirectorySeparatorChar);
        const string rel = "001/2026/04-00001-01-26 R00.pdf";
        var (a, hashA) = await armazem.PrepararAsync(rel, [1, 2, 3]);
        var (b, _) = await armazem.PrepararAsync(rel, [9, 9]);
        Assert.NotEqual(a, b);
        armazem.Confirmar(a, rel);   // a emissão aceita pelo banco
        armazem.Apagar(b);           // a outra falhou ("Boletim já emitido")
        var lido = await armazem.LerAsync(rel);
        Assert.Equal(hashA, ArmazemBoletins.Hash(lido!));
        Assert.Single(Directory.GetFiles(_pasta, "*", SearchOption.AllDirectories));
        Assert.Throws<InvalidOperationException>(() => armazem.Apagar("../fora.pdf"));
    }

    [Theory]
    [InlineData("001", 2026, "04-00012-01/26", 0, "001/2026/04-00012-01-26 R00.pdf")]
    [InlineData("002", 2027, "02-01220-01/18", 3, "002/2027/02-01220-01-18 R03.pdf")]
    public void Caminho_do_pdf_por_empresa_e_ano(string empresa, int ano, string codigo, int revisao, string esperado) =>
        Assert.Equal(esperado, ArmazemBoletins.CaminhoRelativo(empresa, ano, codigo, revisao));
}
