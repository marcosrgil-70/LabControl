using System.Text.Json;
using LabControl.Data;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 0.6 (docs/processos/P21-auditoria-log.md).</summary>
public class AuditoriaTests
{
    private static Task<List<Auditoria>> LinhasAsync(LabDbContext db, string tabela, object id) =>
        db.Auditoria.AsNoTracking().Where(a => a.Tabela == tabela && a.IdRegistro == id.ToString()).OrderBy(a => a.IdAuditoria).ToListAsync();

    private static JsonElement Json(string? s) => JsonDocument.Parse(s!).RootElement;

    [Fact]
    public async Task Alteracao_em_tabela_auditada_grava_antes_e_depois_na_mesma_transacao()
    {
        await using var c = await Cenario.CriarAsync();
        var db = c.Db;
        var id = c.Proposta.IdLabPropostas;

        // inclusão (feita pelo Cenario): linha completa, com os valores gerados pelo banco
        var inc = Assert.Single(await LinhasAsync(db, "LAB_PROPOSTAS", id));
        Assert.Equal(AcoesAuditoria.Inclusao, inc.Acao);
        Assert.StartsWith("000001/", Json(inc.DadosDepois).GetProperty("NR_PROPOSTA").GetString());
        Assert.Null(inc.DadosAntes);
        Assert.Equal((c.Ativa.EmpresaId, c.Ativa.UsuarioId, "127.0.0.1"), (inc.IdEmpresas, inc.IdUsuarios, inc.Ip));

        // enum gravado com o valor do banco
        var analise = await db.LabPropostasProdanalises.FirstAsync(a => a.IdLabPropostas == id);
        var incAn = Assert.Single(await LinhasAsync(db, "LAB_PROPOSTAS_PRODANALISES", analise.IdLabPropostasProdanalises));
        Assert.Equal("BOLETIM", Json(incAn.DadosDepois).GetProperty("TIPO_DOCUMENTO").GetString());

        // alteração dentro de uma transação revertida: a auditoria some junto (mesma transação)
        var validade = c.Proposta.DtSolicitacao.AddDays(30);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            c.Proposta.DtValidade = validade;
            await db.SaveChangesAsync();
            Assert.Equal(2, (await LinhasAsync(db, "LAB_PROPOSTAS", id)).Count);
            await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        Assert.Single(await LinhasAsync(db, "LAB_PROPOSTAS", id));
        Assert.Null((await db.LabPropostas.AsNoTracking().SingleAsync(p => p.IdLabPropostas == id)).DtValidade);

        // alteração sem transação do serviço: o contexto abre uma e grava só as colunas alteradas
        var prop = await db.LabPropostas.SingleAsync(p => p.IdLabPropostas == id);
        prop.DtValidade = validade;
        prop.PorcDesconto = 10; // mesmo valor: não entra
        await db.SaveChangesAsync();
        var alt = (await LinhasAsync(db, "LAB_PROPOSTAS", id)).Last();
        Assert.Equal(AcoesAuditoria.Alteracao, alt.Acao);
        Assert.Equal(JsonValueKind.Null, Json(alt.DadosAntes).GetProperty("DT_VALIDADE").ValueKind);
        Assert.Equal(validade.ToString("yyyy-MM-dd"), Json(alt.DadosDepois).GetProperty("DT_VALIDADE").GetString());
        Assert.Equal(["DT_VALIDADE"], Json(alt.DadosDepois).EnumerateObject().Select(p => p.Name));

        // nada alterado de fato: nenhuma linha
        prop.DtValidade = validade;
        await db.SaveChangesAsync();
        Assert.Equal(2, (await LinhasAsync(db, "LAB_PROPOSTAS", id)).Count);

        // exclusão: linha completa em DADOS_ANTES
        db.Remove(await db.LabPropostasProdanalises.SingleAsync(a => a.IdLabPropostasProdanalises == analise.IdLabPropostasProdanalises));
        await db.SaveChangesAsync();
        var exc = (await LinhasAsync(db, "LAB_PROPOSTAS_PRODANALISES", analise.IdLabPropostasProdanalises)).Last();
        Assert.Equal(AcoesAuditoria.Exclusao, exc.Acao);
        Assert.Equal(100m, Json(exc.DadosAntes).GetProperty("VR_UNITARIO").GetDecimal());
        Assert.Null(exc.DadosDepois);

        // tabela não auditada
        db.Add(new Unidade { Descricao = "NÃO AUDITADA", Sigla = "NA" });
        await db.SaveChangesAsync();
        Assert.False(await db.Auditoria.AnyAsync(a => a.Tabela == "UNIDADES"));
    }

    [Fact]
    public async Task Blob_e_hash_de_senha_nunca_vao_para_o_json()
    {
        await using var c = await Cenario.CriarAsync();
        var db = c.Db;

        var usu = await db.Usuarios.SingleAsync(u => u.IdUsuarios == c.Ativa.UsuarioId);
        var incUsu = Assert.Single(await LinhasAsync(db, "USUARIOS", usu.IdUsuarios));
        Assert.Equal("***", Json(incUsu.DadosDepois).GetProperty("SENHA_HASH").GetString());

        const string hash = "AQAAAAIAAYagAAAAEJ-hash-secreto";
        usu.SenhaHash = hash;
        usu.UltimoAcesso = DateTime.Now; // ignorada (o login tem evento próprio)
        await db.SaveChangesAsync();
        var alt = (await LinhasAsync(db, "USUARIOS", usu.IdUsuarios)).Last();
        Assert.Equal(AcoesAuditoria.Alteracao, alt.Acao);
        Assert.Equal(["SENHA_HASH"], Json(alt.DadosDepois).EnumerateObject().Select(p => p.Name));
        Assert.Equal("***", Json(alt.DadosDepois).GetProperty("SENHA_HASH").GetString());
        Assert.DoesNotContain(hash, alt.DadosDepois + alt.DadosAntes);

        var emp = await db.Empresas.SingleAsync(e => e.IdEmpresas == c.Empresa.IdEmpresas);
        emp.Logo = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];
        await db.SaveChangesAsync();
        var altEmp = (await LinhasAsync(db, "EMPRESAS", emp.IdEmpresas)).Last();
        Assert.Equal("[binário]", Json(altEmp.DadosDepois).GetProperty("LOGO").GetString());
    }

    [Fact]
    public async Task Procedures_registram_revisao_emissao_e_pedido()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = c.Proposta.DtSolicitacao;

        var ped = await c.Procedures.GerarPedidoVendaAsync(c.Proposta.IdLabPropostas, hoje, "CLIENTE", "PC-1");
        var aPed = Assert.Single(await LinhasAsync(c.Db, "PEDIDOS_VENDA", ped.Valor.IdPedido));
        Assert.Equal((AcoesAuditoria.Inclusao, 1), (aPed.Acao, Json(aPed.DadosDepois).GetProperty("NR_PEDIDO").GetInt32()));

        var (_, _, bol) = await c.AmostraComBoletimAsync(hoje);
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "b.pdf", new string('f', 64))).Sucesso);
        Assert.Contains(await LinhasAsync(c.Db, "LAB_BOLETIM_ANALISES", bol.IdLabBoletimAnalises), a => a.Acao == AcoesAuditoria.Emissao);

        var rev = await c.Procedures.GerarRevisaoPropostaAsync(c.Proposta.IdLabPropostas, "CLIENTE PEDIU OUTRO PRAZO");
        var aRev = Assert.Single(await LinhasAsync(c.Db, "LAB_PROPOSTAS", rev.Valor.IdNovo));
        Assert.Equal(AcoesAuditoria.Revisao, aRev.Acao);
        Assert.Equal("CLIENTE PEDIU OUTRO PRAZO", Json(aRev.DadosDepois).GetProperty("MOTIVO").GetString());
        Assert.Equal(c.Proposta.IdLabPropostas, Json(aRev.DadosAntes).GetProperty("ID_ORIGEM").GetInt32());

        // falha de negócio não gera evento
        var antes = await c.Db.Auditoria.CountAsync();
        Assert.False((await c.Procedures.GerarPedidoVendaAsync(rev.Valor.IdNovo, hoje, null, null)).Sucesso);
        Assert.Equal(antes, await c.Db.Auditoria.CountAsync());
    }
}
