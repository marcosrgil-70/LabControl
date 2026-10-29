using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 7.2 (T24): descarte com data, localização na raiz, desfazer descarte, lote e consulta.</summary>
public class LocalizacaoTests
{
    private static LocalizacaoService Servico(Cenario c, EmpresaFixa? ativa = null) =>
        new(c.Db, ativa ?? c.Ativa, new AmostraService(c.Db, ativa ?? c.Ativa, c.Procedures));

    private static async Task<DateTime> AgoraAsync(Cenario c) =>
        await c.Db.Database.SqlQueryRaw<DateTime>("SELECT NOW() AS Value").SingleAsync();

    private static async Task<LabHistAmostra> AmostraAsync(Cenario c)
    {
        var a = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(a);
        await c.Db.SaveChangesAsync();
        return a;
    }

    private static LocalizacaoVM Arquivo(int idAmostra, DateTime quando, string caixa = "CX-01") =>
        new() { IdAmostra = idAmostra, DtHrArquivo = quando, Armario = "A1", Prateleira = "P2", Caixa = caixa };

    [Fact]
    public async Task Descartar_exige_data_valida_e_profissional()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var amo = await AmostraAsync(c);
        var agora = await AgoraAsync(c);
        var arquivo = agora.AddHours(-2);

        var semData = Arquivo(amo.IdLabHistAmostras, arquivo);
        semData.Status = SituacaoArquivo.Descartada;
        Assert.Equal("Informe a data e a hora do descarte.", (await s.GravarAsync(semData)).Mensagem);
        semData.DtHrDescarte = agora.AddDays(1);
        Assert.Equal("A data do descarte não pode ser futura.", (await s.GravarAsync(semData)).Mensagem);
        semData.DtHrDescarte = arquivo.AddHours(-1);
        Assert.Equal("A data do descarte não pode ser anterior à do arquivo.", (await s.GravarAsync(semData)).Mensagem);
        var futura = Arquivo(amo.IdLabHistAmostras, agora.AddDays(1));
        Assert.Equal("A data do arquivo não pode ser futura.", (await s.GravarAsync(futura)).Mensagem);

        semData.DtHrDescarte = agora;
        var semProfissional = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId };
        Assert.Contains("não está vinculado a um profissional", (await Servico(c, semProfissional).GravarAsync(semData)).Mensagem);

        var ok = await s.GravarAsync(semData);
        Assert.True(ok.Sucesso, ok.Mensagem);
        var l = await c.Db.LabLocalAmostras.AsNoTracking().SingleAsync(x => x.IdLabLocalAmostras == ok.Valor.Id);
        Assert.Equal(SituacaoArquivo.Descartada, l.Status);
        Assert.Equal(c.Ativa.ProfissionalId, l.IdEntidadesFuncDescarte);
        Assert.Equal(0, l.DtHrDescarte!.Value.Second);   // grava no minuto, como a tela mostra
    }

    [Fact]
    public async Task Localizacao_da_revisao_fica_na_amostra_fisica_e_desfazer_descarte_limpa()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amo, _, _) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        var rev = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "LOTE ERRADO");
        Assert.True(rev.Sucesso, rev.Mensagem);
        var agora = await AgoraAsync(c);

        var r = await s.GravarAsync(Arquivo(rev.Valor.IdNova, agora.AddHours(-1)));
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal(amo.IdLabHistAmostras, (await c.Db.LabLocalAmostras.AsNoTracking().SingleAsync(x => x.IdLabLocalAmostras == r.Valor.Id)).IdLabHistAmostras);
        var pelaRaiz = await s.CarregarAsync(amo.IdLabHistAmostras);
        Assert.Equal(r.Valor.Id, pelaRaiz!.Id);
        Assert.Equal("CX-01", pelaRaiz.Caixa);
        Assert.False(pelaRaiz.UltimaRevisao);
        // a mesma amostra física não ganha outra localização
        Assert.StartsWith("Esta amostra já foi localizada", (await s.GravarAsync(Arquivo(amo.IdLabHistAmostras, agora))).Mensagem);

        // descartar e desfazer: volta para arquivada, sem data nem profissional, com o local mantido
        var vm = (await s.CarregarAsync(rev.Valor.IdNova))!;
        (vm.Status, vm.DtHrDescarte) = (SituacaoArquivo.Descartada, agora);
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        vm = (await s.CarregarAsync(rev.Valor.IdNova))!;
        Assert.NotNull(vm.ProfissionalDescarte);
        vm.Status = SituacaoArquivo.Arquivada;
        Assert.True((await s.GravarAsync(vm)).Sucesso);
        var l = await c.Db.LabLocalAmostras.AsNoTracking().SingleAsync(x => x.IdLabLocalAmostras == r.Valor.Id);
        Assert.Equal((SituacaoArquivo.Arquivada, (DateTime?)null, (int?)null, "CX-01"), (l.Status, l.DtHrDescarte, l.IdEntidadesFuncDescarte, l.NrCaixa));
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "LAB_LOCAL_AMOSTRAS" && a.IdRegistro == r.Valor.Id.ToString()
            && a.Acao == LabControl.Data.AcoesAuditoria.Alteracao));

        // excluir: a amostra volta a ficar sem localização
        Assert.True((await s.ExcluirAsync(r.Valor.Id)).Sucesso);
        Assert.Equal(0, (await s.CarregarAsync(amo.IdLabHistAmostras))!.Id);
    }

    [Fact]
    public async Task Descarte_por_reprovacao_muda_o_status_da_ultima_revisao()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var amo = await AmostraAsync(c);
        var agora = await AgoraAsync(c);
        var vm = Arquivo(amo.IdLabHistAmostras, agora.AddHours(-1));
        (vm.Status, vm.DtHrDescarte, vm.MarcarReprovada) = (SituacaoArquivo.Descartada, agora, true);
        Assert.True((await s.CarregarAsync(amo.IdLabHistAmostras))!.TemStatusReprovada);

        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal(LocalizacaoService.StatusReprovadaDescartada,
            await c.Db.VwLabAmostras.Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).Select(a => a.StatusAmostra).SingleAsync());

        // desfazer o descarte não devolve o status anterior: avisa (code-review 7.3)
        var desfazer = (await s.CarregarAsync(amo.IdLabHistAmostras))!;
        desfazer.Status = SituacaoArquivo.Arquivada;
        var d = await s.GravarAsync(desfazer);
        Assert.True(d.Sucesso, d.Mensagem);
        Assert.Equal($"O status da amostra continua “{LocalizacaoService.StatusReprovadaDescartada}”: se precisar, altere no recebimento.", d.Valor.Aviso);
    }

    /// <summary>Code-review 7.3: amostra com boletim emitido não muda de status pelo descarte (o banco recusaria e o descarte se perderia).</summary>
    [Fact]
    public async Task Descarte_por_reprovacao_de_amostra_com_boletim_emitido_muda_so_o_status()
    {
        // pendência 34 (decisão do usuário, 29/10/2026): o banco aceita a troca só do status; o boletim emitido não muda
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var (amo, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "boletins/x.pdf", new string('a', 64))).Sucesso);
        var agora = await AgoraAsync(c);

        var vm = Arquivo(amo.IdLabHistAmostras, agora.AddHours(-1));
        (vm.Status, vm.DtHrDescarte, vm.MarcarReprovada) = (SituacaoArquivo.Descartada, agora, true);
        var r = await s.GravarAsync(vm);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal(LocalizacaoService.StatusReprovadaDescartada,
            await c.Db.VwLabAmostras.AsNoTracking().Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).Select(a => a.StatusAmostra).SingleAsync());
        var b = await c.Db.LabBoletimAnalises.AsNoTracking().SingleAsync(x => x.IdLabBoletimAnalises == bol.IdLabBoletimAnalises);
        Assert.Equal(("boletins/x.pdf", new string('a', 64)), (b.ArquivoPdf, b.HashPdf));
        Assert.NotNull(b.DhEmissao);
        // outra alteração da amostra emitida continua exigindo revisão
        await Assert.ThrowsAnyAsync<MySqlConnector.MySqlException>(() =>
            c.Db.LabHistAmostras.Where(a => a.IdLabHistAmostras == amo.IdLabHistAmostras).ExecuteUpdateAsync(x => x.SetProperty(a => a.NrLote, "OUTRO")));
    }

    [Fact]
    public async Task Arquivar_em_lote_nao_sobrescreve_as_ja_localizadas()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Servico(c);
        var a1 = await AmostraAsync(c);
        var a2 = await AmostraAsync(c);
        var a3 = await AmostraAsync(c);
        var agora = await AgoraAsync(c);
        Assert.True((await s.GravarAsync(Arquivo(a3.IdLabHistAmostras, agora.AddHours(-1), "CX-ANTIGA"))).Sucesso);

        var lote = new LocalizacaoLoteVM { Amostras = [a1.IdLabHistAmostras, a2.IdLabHistAmostras, a3.IdLabHistAmostras, a1.IdLabHistAmostras], DtHrArquivo = agora };
        Assert.Equal("Informe o armário, a prateleira ou a caixa.", (await s.ArquivarLoteAsync(lote)).Mensagem);
        lote.Caixa = "CX-07";
        var r = await s.ArquivarLoteAsync(lote);
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.Equal(2, r.Valor.Arquivadas);
        Assert.Equal([a3.NrAmostra], r.Valor.JaLocalizadas);
        Assert.Equal("CX-ANTIGA", await c.Db.LabLocalAmostras.Where(l => l.IdLabHistAmostras == a3.IdLabHistAmostras).Select(l => l.NrCaixa).SingleAsync());
        Assert.StartsWith("Todas as amostras marcadas já têm localização", (await s.ArquivarLoteAsync(lote)).Mensagem);
        Assert.Equal("Marque ao menos uma amostra.", (await s.ArquivarLoteAsync(new LocalizacaoLoteVM { Caixa = "X" })).Mensagem);

        // consulta: sem filtro não lista; pela caixa e pela situação
        Assert.Empty((await s.ListarAsync(new LocalizacaoFiltroVM())).Itens);
        Assert.Equal([a1.IdLabHistAmostras, a2.IdLabHistAmostras], (await s.ListarAsync(new LocalizacaoFiltroVM { Caixa = "CX-07" })).Itens.Select(i => i.Id).Order());
        var b = await AmostraAsync(c);
        var sem = await s.ListarAsync(new LocalizacaoFiltroVM { Situacao = SituacaoLocalizacao.SemLocalizacao });
        Assert.Equal([b.IdLabHistAmostras], sem.Itens.Select(i => i.Id));
        Assert.Null(sem.Itens[0].Situacao);
    }
}
