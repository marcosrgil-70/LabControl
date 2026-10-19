using LabControl.Data;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Colunas preenchidas pelas triggers são omitidas/relidas pelo EF (ConfiguracaoExtensions).
/// Tudo numa transação revertida no final.
/// </summary>
public class GravacaoTests
{
    [Fact]
    public async Task Colunas_preenchidas_pelo_banco_sao_relidas_apos_gravar()
    {
        var ativa = new EmpresaFixa();
        await using var db = BancoTeste.NovoContexto(ativa);
        await using var tx = await db.Database.BeginTransactionAsync();
        var hoje = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURDATE() AS Value").SingleAsync();
        var aa = (hoje.Year % 100).ToString("00");

        // empresa (global): CRIADO_EM (default do banco) e ATIVA (default 1 → inicializador true)
        var emp = new Empresa { CodEmpresa = "T99", RazaoSocial = "EMPRESA DE TESTE" };
        db.Add(emp);
        await db.SaveChangesAsync();
        Assert.True(emp.Ativa);
        Assert.NotEqual(default, emp.CriadoEm);
        await db.Database.ExecuteSqlInterpolatedAsync($"CALL SP_INICIALIZAR_EMPRESA({emp.IdEmpresas})");
        var e = emp.IdEmpresas;
        ativa.EmpresaId = e;

        // produto com código vazio → 000001 (TG_PRODUTOS_BI)
        var uni = new Unidade { IdEmpresas = e, Descricao = "UFC POR GRAMA", Sigla = "UFC/g" };
        db.Add(uni);
        await db.SaveChangesAsync();
        var prod = new Produto { IdEmpresas = e, Descricao = "PRODUTO TESTE", IdUnidades = uni.IdUnidades };
        db.Add(prod);
        await db.SaveChangesAsync();
        Assert.Equal("000001", prod.Codigo);

        // cliente (ENUM CATEGORIA gravado como 'J'), endereço e e-mail (ID_EMPRESAS herdado pela trigger)
        var cli = new Entidade { IdEmpresas = e, Categoria = Categoria.Juridica, Nome = "CLIENTE TESTE", TipoCliente = true };
        var uf = await db.Estados.SingleAsync(x => x.Sigla == "SP");
        var cid = new Cidade { IdEstados = uf.IdEstados, Nome = "CIDADE TESTE" };
        db.AddRange(cli, cid);
        await db.SaveChangesAsync();
        var categoria = await db.Database
            .SqlQuery<string>($"SELECT CATEGORIA AS Value FROM ENTIDADES WHERE ID_ENTIDADES = {cli.IdEntidades}").SingleAsync();
        Assert.Equal("J", categoria);

        var tipoEnd = await db.EnderecosTipos.FirstAsync(x => x.IdEmpresas == e);
        var end = new EntidadeEndereco { IdEmpresas = e, IdEntidades = cli.IdEntidades, IdEnderecosTipos = tipoEnd.IdEnderecosTipos, Logradouro = "RUA TESTE, 1", IdCidades = cid.IdCidades };
        var eml = new EntidadeEmail { IdEmpresas = e, IdEntidades = cli.IdEntidades, Email = "cliente@teste.com" };
        var cpg = new CondicaoPagto { IdEmpresas = e, Codigo = "01", Descricao = "À VISTA" };
        db.AddRange(end, eml, cpg);
        await db.SaveChangesAsync();
        var par = await db.EmpresasParametros.SingleAsync(x => x.IdEmpresas == e);

        // proposta: número/ano/data do banco (TG_PROPOSTAS_BI)
        var prop = new LabProposta
        {
            IdEmpresas = e, IdEntidades = cli.IdEntidades, IdEntidadesFunc = cli.IdEntidades,
            IdEntEndLaudo = end.IdEntidadesEnderecos, IdEntContatoResultado = eml.IdEntidadesEmails,
            IdEntCobranca = cli.IdEntidades, IdLabPropostasStatus = par.IdLabPropostasStatusInicial!.Value,
            IdCondicoesPagtos = cpg.IdCondicoesPagtos, PorcDesconto = 10,
        };
        db.Add(prop);
        await db.SaveChangesAsync();
        Assert.Equal($"000001/{aa}-R0", prop.NrProposta);
        Assert.Equal(1, prop.CodProposta);
        Assert.Equal(hoje.Year, prop.AnoProposta);
        Assert.Equal(hoje, prop.DtSolicitacao);
        Assert.True(prop.UltimaRevisao);

        // item: ITEM automático (TG_PROPPROD_BI)
        var item = new LabPropostaProduto { IdEmpresas = e, IdLabPropostas = prop.IdLabPropostas, IdProdutos = prod.IdProdutos, QtdeAmostras = 2 };
        db.Add(item);
        await db.SaveChangesAsync();
        Assert.Equal(1, item.Item);

        // análise: subtotal/total calculados (TG_PRODAN_BI); totais da proposta pela trigger AFTER
        var tipoAn = new AnaliseTipo { IdEmpresas = e, Codigo = 1, Descricao = "MICROBIOLÓGICA" };
        var met = new AnaliseMetodo { IdEmpresas = e, Descricao = "ISO 4833" };
        var prz = new Prazo { IdEmpresas = e, Descricao = "10 DIAS", QtdeDias = 10 };
        db.AddRange(tipoAn, met, prz);
        await db.SaveChangesAsync();
        var param = new LabParametroAnalise { IdEmpresas = e, Descricao = "CONTAGEM TOTAL", DescReduzida = "CONT. TOTAL", IdAnalisesTipo = tipoAn.IdAnalisesTipo };
        db.Add(param);
        await db.SaveChangesAsync();
        var idioma = await db.Idiomas.FirstAsync(x => x.IdEmpresas == e);
        var an = new LabPropostaProdAnalise
        {
            IdEmpresas = e, IdLabPropostas = prop.IdLabPropostas, IdLabPropostasProdutos = item.IdLabPropostasProdutos,
            IdLabParametrosAnalises = param.IdLabParametrosAnalises, IdAnalisesMetodos = met.IdAnalisesMetodos,
            IdIdiomas = idioma.IdIdiomas, IdPrazos = prz.IdPrazos, QtdeAmostras = 2, VrUnitario = 100,
            VrSubtotal = 999, VrTotal = 999, // ignorados: calculados pelo banco
        };
        db.Add(an);
        await db.SaveChangesAsync();
        Assert.Equal(TipoDocumento.Boletim, an.TipoDocumento);
        Assert.Equal(200m, an.VrSubtotal);
        Assert.Equal(200m, an.VrTotal);

        await db.Entry(prop).ReloadAsync();
        Assert.Equal(200m, prop.VrSomaVrTotAnalis);
        Assert.Equal(20m, prop.VrDesconto);
        Assert.Equal(180m, prop.VrTotalProposta);

        // UPDATE: número não é reenviado (alterar CodProposta é ignorado) e ALTERADO_EM é relido
        prop.CodProposta = 777;
        prop.DtValidade = hoje.AddDays(30);
        await db.SaveChangesAsync();
        Assert.NotNull(prop.AlteradoEm);
        var cod = await db.Database
            .SqlQuery<int>($"SELECT COD_PROPOSTA AS Value FROM LAB_PROPOSTAS WHERE ID_LAB_PROPOSTAS = {prop.IdLabPropostas}").SingleAsync();
        Assert.Equal(1, cod);

        await tx.RollbackAsync();
    }
}
