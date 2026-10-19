using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Monta, numa empresa nova, os cadastros e documentos do roteiro do doc 12 §7 (proposta com 2 análises,
/// amostra, resultado, boletim). Grava de verdade (sem transação revertida): usar só no banco de testes.
/// </summary>
public sealed class Cenario : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Db.DisposeAsync();

    public required LabDbContext Db { get; init; }
    public required EmpresaFixa Ativa { get; init; }
    public required Procedures Procedures { get; init; }
    public required Empresa Empresa { get; init; }

    public Entidade Cliente { get; private set; } = null!;
    public Produto Produto { get; private set; } = null!;
    public LabProposta Proposta { get; private set; } = null!;
    public LabPropostaProduto Item { get; private set; } = null!;
    public LabParametroAnalise ParamAcreditado { get; private set; } = null!;
    public LabParametroAnalise ParamDescritivo { get; private set; } = null!;
    public AmostraTipo TipoAmostra { get; private set; } = null!;
    public AnaliseTipo TipoAnalise { get; private set; } = null!;
    public EmbalagemTipo Embalagem { get; private set; } = null!;
    public Unidade Unidade { get; private set; } = null!;
    public LabTipoResultado Ausencia { get; private set; } = null!;
    public int StatusAmostraInicial { get; private set; }

    public static async Task<Cenario> CriarAsync()
    {
        var ativa = new EmpresaFixa();
        var db = await BancoTeste.NovoContextoAsync(ativa);
        var emp = await BancoTeste.CriarEmpresaAsync(db, ativa);
        var c = new Cenario { Db = db, Ativa = ativa, Procedures = new Procedures(db, ativa), Empresa = emp };
        await c.CadastrosAsync();
        return c;
    }

    private async Task CadastrosAsync()
    {
        var par = await Db.EmpresasParametros.SingleAsync();
        StatusAmostraInicial = par.IdAmostrasStatusInicial!.Value;

        var uf = await Db.Estados.SingleAsync(x => x.Sigla == "SP");
        var cid = new Cidade { IdEstados = uf.IdEstados, Nome = "CIDADE TESTE" };
        Cliente = new Entidade { Categoria = Categoria.Juridica, Nome = "CLIENTE TESTE", TipoCliente = true };
        Unidade = new Unidade { Descricao = "UFC POR GRAMA", Sigla = "UFC/g" };
        Embalagem = new EmbalagemTipo { Descricao = "SACO PLÁSTICO" };
        TipoAmostra = new AmostraTipo { Codigo = 4, Descricao = "ALIMENTOS" };
        TipoAnalise = new AnaliseTipo { Codigo = 1, Descricao = "MICROBIOLÓGICA" };
        Ausencia = new LabTipoResultado { Descricao = "Ausência em 25 g" };
        var met1 = new AnaliseMetodo { Descricao = "ISO 4833" };
        var met2 = new AnaliseMetodo { Descricao = "ISO 6579" };
        var prz = new Prazo { Descricao = "10 DIAS", QtdeDias = 10 };
        var cpg = new CondicaoPagto { Codigo = "01", Descricao = "À VISTA" };
        Db.AddRange(cid, Cliente, Unidade, Embalagem, TipoAmostra, TipoAnalise, Ausencia, met1, met2, prz, cpg);
        await Db.SaveChangesAsync();

        var tipoEnd = await Db.EnderecosTipos.FirstAsync();
        var end = new EntidadeEndereco { IdEntidades = Cliente.IdEntidades, IdEnderecosTipos = tipoEnd.IdEnderecosTipos, Logradouro = "RUA TESTE, 1", IdCidades = cid.IdCidades };
        var eml = new EntidadeEmail { IdEntidades = Cliente.IdEntidades, Email = "cliente@teste.com" };
        Produto = new Produto { Descricao = "QUEIJO MINAS", IdUnidades = Unidade.IdUnidades };
        ParamAcreditado = new LabParametroAnalise { Descricao = "CONTAGEM TOTAL", DescReduzida = "CONT. TOTAL", IdAnalisesTipo = TipoAnalise.IdAnalisesTipo, Auditado = true };
        ParamDescritivo = new LabParametroAnalise { Descricao = "SALMONELLA SP", DescReduzida = "SALMONELLA", IdAnalisesTipo = TipoAnalise.IdAnalisesTipo };
        Db.AddRange(end, eml, Produto, ParamAcreditado, ParamDescritivo);
        await Db.SaveChangesAsync();

        Proposta = new LabProposta
        {
            IdEntidades = Cliente.IdEntidades, IdEntidadesFunc = Ativa.ProfissionalId!.Value,
            IdEntEndLaudo = end.IdEntidadesEnderecos, IdEntContatoResultado = eml.IdEntidadesEmails,
            IdEntCobranca = Cliente.IdEntidades, IdLabPropostasStatus = par.IdLabPropostasStatusInicial!.Value,
            IdCondicoesPagtos = cpg.IdCondicoesPagtos, PorcDesconto = 10,
        };
        Db.Add(Proposta);
        await Db.SaveChangesAsync();
        Item = new LabPropostaProduto { IdLabPropostas = Proposta.IdLabPropostas, IdProdutos = Produto.IdProdutos, QtdeAmostras = 2 };
        Db.Add(Item);
        await Db.SaveChangesAsync();
        var idioma = await Db.Idiomas.FirstAsync();
        Db.AddRange(
            new LabPropostaProdAnalise { IdLabPropostas = Proposta.IdLabPropostas, IdLabPropostasProdutos = Item.IdLabPropostasProdutos,
                IdLabParametrosAnalises = ParamAcreditado.IdLabParametrosAnalises, IdAnalisesMetodos = met1.IdAnalisesMetodos,
                IdIdiomas = idioma.IdIdiomas, IdPrazos = prz.IdPrazos, QtdeAmostras = 2, VrUnitario = 100 },
            new LabPropostaProdAnalise { IdLabPropostas = Proposta.IdLabPropostas, IdLabPropostasProdutos = Item.IdLabPropostasProdutos,
                IdLabParametrosAnalises = ParamDescritivo.IdLabParametrosAnalises, IdAnalisesMetodos = met2.IdAnalisesMetodos,
                IdIdiomas = idioma.IdIdiomas, IdPrazos = prz.IdPrazos, QtdeAmostras = 2, VrUnitario = 50 });
        await Db.SaveChangesAsync();
    }

    /// <summary>Amostra nova (ainda não gravada) do item da proposta.</summary>
    public LabHistAmostra NovaAmostra(DateOnly entrega) => new()
    {
        IdAmostrasTipo = TipoAmostra.IdAmostrasTipo, IdAnalisesTipo = TipoAnalise.IdAnalisesTipo,
        IdEntidades = Cliente.IdEntidades, IdLabPropostas = Proposta.IdLabPropostas, IdLabPropostasProdutos = Item.IdLabPropostasProdutos,
        DtEntrega = entrega, HrEntrega = new TimeOnly(10, 0), LocalRecebimento = "RECEPÇÃO",
        IdEmbalagensTipos = Embalagem.IdEmbalagensTipos, QtdeEmbalagensEntregue = 3,
        IdAmostrasStatus = StatusAmostraInicial, IdEntidadesFuncResp = Ativa.ProfissionalId!.Value, IdEntidadesFuncDig = Ativa.ProfissionalId!.Value,
    };

    /// <summary>Amostra de hoje + testes + resultado (2 parâmetros) + boletim com 1 parâmetro, como no roteiro 12 §7.</summary>
    public async Task<(LabHistAmostra Amostra, LabResultadoAnalise Resultado, LabBoletimAnalise Boletim)> AmostraComBoletimAsync(DateOnly hoje)
    {
        var amo = NovaAmostra(hoje);
        Db.Add(amo);
        await Db.SaveChangesAsync();
        Assert.True((await Procedures.AmostraGerarTestesAsync(amo.IdLabHistAmostras)).Sucesso);
        var testes = await Db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == amo.IdLabHistAmostras).ToListAsync();

        var res = new LabResultadoAnalise { IdLabHistAmostras = amo.IdLabHistAmostras, IdEntidadesFuncDig = Ativa.ProfissionalId!.Value };
        Db.Add(res);
        await Db.SaveChangesAsync();
        var rp1 = new LabResultadoParam { IdLabResultadosAnalises = res.IdLabResultadosAnalises,
            IdLabHistAmostrasTestes = testes.Single(t => t.IdLabParametrosAnalises == ParamAcreditado.IdLabParametrosAnalises).IdLabHistAmostrasTestes,
            DtInicial = hoje, DtResultado = hoje, TipoResultado = true, SimboloGrandeza = "<", VrResultado = "1,0", VrExpResultado = 2, IdUnidades = Unidade.IdUnidades };
        var rp2 = new LabResultadoParam { IdLabResultadosAnalises = res.IdLabResultadosAnalises,
            IdLabHistAmostrasTestes = testes.Single(t => t.IdLabParametrosAnalises == ParamDescritivo.IdLabParametrosAnalises).IdLabHistAmostrasTestes,
            DtInicial = hoje, DtResultado = hoje, TipoResultado = false, IdLabTiposResultados = Ausencia.IdLabTiposResultados };
        Db.AddRange(rp1, rp2);
        await Db.SaveChangesAsync();

        var bol = new LabBoletimAnalise { IdLabHistAmostras = amo.IdLabHistAmostras, IdLabResultadosAnalises = res.IdLabResultadosAnalises,
            IdEntidadesFuncGer = Ativa.ProfissionalId!.Value, IdEntidadesFuncResp = Ativa.ProfissionalId!.Value };
        Db.Add(bol);
        await Db.SaveChangesAsync();
        Db.Add(new LabBoletimParametro { IdLabBoletimAnalises = bol.IdLabBoletimAnalises, IdLabResultadosParam = rp1.IdLabResultadosParam,
            Ordem = 1, DescParametro = "CONT. TOTAL", ResultadoConcat = rp1.ResultadoFormatado, Acreditado = true });
        await Db.SaveChangesAsync();
        return (amo, res, bol);
    }
}
