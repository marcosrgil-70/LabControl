using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.Services.Boletins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests.Manual;

/// <summary>
/// Laboratório fictício para as imagens do manual do usuário (nenhum dado real): duas empresas usuárias com o mesmo CNPJ
/// (matriz e filial), clientes, produtos, parâmetros, propostas, pedido de venda, amostras em todas as etapas
/// (aguardando análise, com resultado, com boletim emitido e arquivada) e os usuários que entram nas telas.
/// Grava de verdade: só no banco temporário dos testes.
/// </summary>
public sealed class CenarioManual
{
    public const string Senha = "Exemplo@2026";
    public const string LoginOperador = "ANA.SOUZA";
    public const string LoginAdministrador = "ADMINISTRADOR";

    public required int Empresa { get; init; }
    public int Filial { get; private set; }
    public int Cliente { get; private set; }
    public int Profissional { get; private set; }
    public int Produto { get; private set; }
    public int Parametro { get; private set; }
    public int Proposta { get; private set; }
    public int PropostaAberta { get; private set; }
    public int UsuarioRecepcao { get; private set; }
    public int Pedido { get; private set; }
    public int AmostraPendente { get; private set; }
    public int AmostraComResultado { get; private set; }
    public int AmostraEmitida { get; private set; }
    public int Resultado { get; private set; }
    public int BoletimAberto { get; private set; }
    public int BoletimEmitido { get; private set; }
    public int UsuarioOperador { get; private set; }
    public int PerfilRecepcao { get; private set; }
    public int Prazo { get; private set; }
    public long EventoAuditoria { get; private set; }

    public static async Task<CenarioManual> CriarAsync(string pastaBoletins)
    {
        await using var adm = await BancoTeste.NovoContextoAsync(new EmpresaFixa { AdminGlobal = true });
        var empresa = await NovaEmpresaAsync(adm, "LABORATÓRIO EXEMPLO DE ANÁLISES LTDA", "LAB EXEMPLO", "12.345.678/0001-95");
        var c = new CenarioManual { Empresa = empresa };
        // segunda empresa usuária (filial, CNPJ próprio): o operador escolhe a empresa no login
        c.Filial = await NovaEmpresaAsync(adm, "LABORATÓRIO EXEMPLO DE ANÁLISES LTDA - FILIAL CAMPINAS", "LAB EXEMPLO CAMPINAS", "12.345.678/0002-76");

        var ativa = new EmpresaFixa { EmpresaId = empresa };
        await using var db = await BancoTeste.NovoContextoAsync(ativa);
        await c.CadastrosAsync(db, ativa);
        await c.UsuariosAsync(db, ativa);
        await c.MovimentoAsync(db, ativa, pastaBoletins);
        return c;
    }

    private static async Task<int> NovaEmpresaAsync(LabDbContext adm, string razao, string fantasia, string cnpj)
    {
        var cod = (await adm.Empresas.CountAsync() + 1).ToString("000");
        var emp = new Empresa { CodEmpresa = cod, RazaoSocial = razao, NomeFantasia = fantasia, Cnpj = cnpj,
            InscEstadual = "123.456.789.110", Site = "www.labexemplo.com.br", NrAcreditacao = "CRL 0000" };
        adm.Add(emp);
        await adm.SaveChangesAsync();
        var ini = await new Procedures(adm, new EmpresaFixa { AdminGlobal = true }).InicializarEmpresaAsync(emp.IdEmpresas);
        if (!ini.Sucesso) throw new InvalidOperationException(ini.Mensagem);
        return emp.IdEmpresas;
    }

    private Unidade _ufc = null!, _nmp = null!;
    private EmbalagemTipo _saco = null!;
    private AmostraTipo _alimentos = null!;
    private AnaliseTipo _micro = null!;
    private LabTipoResultado _ausencia = null!;
    private AnaliseMetodo _iso4833 = null!, _iso6579 = null!, _iso16649 = null!;
    private Prazo _uteis = null!, _corridos = null!;
    private CondicaoPagto _trinta = null!;
    private Produto _queijo = null!, _iogurte = null!, _linguica = null!;
    private LabParametroAnalise _mesofilos = null!, _salmonella = null!, _ecoli = null!;
    private Entidade _laticinios = null!, _frigorifico = null!;
    private EntidadeEndereco _endLaticinios = null!, _endFrigorifico = null!;
    private EntidadeEmail _emailLaticinios = null!, _emailFrigorifico = null!;
    private int _analista;

    private async Task CadastrosAsync(LabDbContext db, EmpresaFixa ativa)
    {
        var sp = await db.Cidades.Include(x => x.Estado).FirstAsync(x => x.Nome == "SÃO PAULO" && x.Estado.Sigla == "SP");
        var campinas = await db.Cidades.Include(x => x.Estado).FirstAsync(x => x.Nome == "CAMPINAS" && x.Estado.Sigla == "SP");
        var registro = new TipoRegProfissional { Descricao = "CRBIO" };
        var cargoRt = new CargoFuncionario { Descricao = "RESPONSÁVEL TÉCNICA" };
        var cargoAn = new CargoFuncionario { Descricao = "ANALISTA DE LABORATÓRIO" };
        db.AddRange(registro, cargoRt, cargoAn);
        await db.SaveChangesAsync();

        var ana = new Entidade { Categoria = Categoria.Fisica, Nome = "ANA PAULA SOUZA", CpfCnpj = "123.456.789-09", TipoFuncionario = true };
        var carlos = new Entidade { Categoria = Categoria.Fisica, Nome = "CARLOS EDUARDO LIMA", CpfCnpj = "987.654.321-00", TipoFuncionario = true };
        db.AddRange(ana, carlos);
        await db.SaveChangesAsync();
        db.AddRange(
            new EntidadeFuncionario { IdEntidades = ana.IdEntidades, AssinaBoletim = true, ExecutaAnalise = true, IdCargosFuncionarios = cargoRt.IdCargosFuncionarios,
                IdTiposRegProfissional = registro.IdTiposRegProfissional, NrRegistroProfissional = "12345/01-D", UfRegistro = "SP" },
            new EntidadeFuncionario { IdEntidades = carlos.IdEntidades, ExecutaAnalise = true, IdCargosFuncionarios = cargoAn.IdCargosFuncionarios });
        await db.SaveChangesAsync();
        Profissional = ana.IdEntidades;
        _analista = carlos.IdEntidades;
        ativa.ProfissionalId = ana.IdEntidades;

        _ufc = new Unidade { Descricao = "UNIDADES FORMADORAS DE COLÔNIA POR GRAMA", Sigla = "UFC/g" };
        _nmp = new Unidade { Descricao = "NÚMERO MAIS PROVÁVEL POR GRAMA", Sigla = "NMP/g" };
        _saco = new EmbalagemTipo { Descricao = "SACO PLÁSTICO ESTÉRIL" };
        _alimentos = new AmostraTipo { Codigo = 4, Descricao = "ALIMENTOS" };
        _micro = new AnaliseTipo { Codigo = 1, Descricao = "MICROBIOLÓGICA" };
        _ausencia = new LabTipoResultado { Descricao = "Ausência em 25 g" };
        _iso4833 = new AnaliseMetodo { Descricao = "ISO 4833-1:2013" };
        _iso6579 = new AnaliseMetodo { Descricao = "ISO 6579-1:2017" };
        _iso16649 = new AnaliseMetodo { Descricao = "ISO 16649-2:2001" };
        _uteis = new Prazo { Descricao = "5 DIAS ÚTEIS", QtdeDias = 5, DiasUteis = true };
        _corridos = new Prazo { Descricao = "10 DIAS", QtdeDias = 10 };
        _trinta = new CondicaoPagto { Codigo = "30", Descricao = "30 DIAS" };
        db.AddRange(_ufc, _nmp, _saco, _alimentos, _micro, _ausencia, _iso4833, _iso6579, _iso16649, _uteis, _corridos, _trinta,
            new AmostraTipo { Codigo = 5, Descricao = "ÁGUAS" }, new AnaliseTipo { Codigo = 2, Descricao = "FÍSICO-QUÍMICA" },
            new LabTipoResultado { Descricao = "Presença em 25 g" });
        await db.SaveChangesAsync();
        Prazo = _uteis.IdPrazos;

        _laticinios = new Entidade { Categoria = Categoria.Juridica, Nome = "LATICÍNIOS SERRA AZUL LTDA", NomeFantasia = "SERRA AZUL",
            CpfCnpj = "11.222.333/0001-81", RgIe = "111.222.333.444", TipoCliente = true };
        _frigorifico = new Entidade { Categoria = Categoria.Juridica, Nome = "FRIGORÍFICO BOM CORTE S.A.", NomeFantasia = "BOM CORTE",
            CpfCnpj = "44.555.666/0001-77", TipoCliente = true };
        var padaria = new Entidade { Categoria = Categoria.Juridica, Nome = "PADARIA PÃO DOURADO LTDA", NomeFantasia = "PÃO DOURADO",
            CpfCnpj = "22.333.444/0001-55", TipoCliente = true };
        var fornecedor = new Entidade { Categoria = Categoria.Juridica, Nome = "DISTRIBUIDORA DE REAGENTES ALFA LTDA", NomeFantasia = "ALFA REAGENTES",
            CpfCnpj = "33.444.555/0001-66", TipoFornecedor = true };
        db.AddRange(_laticinios, _frigorifico, padaria, fornecedor);
        await db.SaveChangesAsync();
        Cliente = _laticinios.IdEntidades;

        var tipoEnd = await db.EnderecosTipos.OrderBy(x => x.IdEnderecosTipos).FirstAsync();
        var tipoFone = await db.FonesTipos.OrderBy(x => x.IdFonesTipos).FirstAsync();
        _endLaticinios = new EntidadeEndereco { IdEntidades = _laticinios.IdEntidades, IdEnderecosTipos = tipoEnd.IdEnderecosTipos,
            Logradouro = "RUA DAS FLORES", Numero = "250", Bairro = "DISTRITO INDUSTRIAL", Cep = "13069-000", IdCidades = campinas.IdCidades };
        _endFrigorifico = new EntidadeEndereco { IdEntidades = _frigorifico.IdEntidades, IdEnderecosTipos = tipoEnd.IdEnderecosTipos,
            Logradouro = "AVENIDA DOS ANDRADAS", Numero = "1200", Bairro = "CENTRO", Cep = "01001-000", IdCidades = sp.IdCidades };
        _emailLaticinios = new EntidadeEmail { IdEntidades = _laticinios.IdEntidades, Email = "qualidade@serraazul.com.br", NomeContato = "JULIANA MARTINS",
            Departamento = "QUALIDADE", Principal = true, EnviarLaudo = true };
        _emailFrigorifico = new EntidadeEmail { IdEntidades = _frigorifico.IdEntidades, Email = "laboratorio@bomcorte.com.br", NomeContato = "ROBERTO ALVES",
            Principal = true, EnviarLaudo = true };
        db.AddRange(_endLaticinios, _endFrigorifico, _emailLaticinios, _emailFrigorifico,
            new EntidadeEndereco { IdEntidades = padaria.IdEntidades, IdEnderecosTipos = tipoEnd.IdEnderecosTipos, Logradouro = "RUA DO COMÉRCIO", Numero = "45",
                Bairro = "VILA NOVA", Cep = "01310-100", IdCidades = sp.IdCidades },
            new EntidadeFone { IdEntidades = _laticinios.IdEntidades, IdFonesTipos = tipoFone.IdFonesTipos, Ddd = "19", Fone = "3255-1000", Principal = true, Contato = "JULIANA" },
            new EntidadeFone { IdEntidades = _frigorifico.IdEntidades, IdFonesTipos = tipoFone.IdFonesTipos, Ddd = "11", Fone = "3333-4444", Principal = true });
        await db.SaveChangesAsync();

        _queijo = new Produto { Descricao = "QUEIJO MINAS FRESCAL", IdUnidades = _ufc.IdUnidades };
        _iogurte = new Produto { Descricao = "IOGURTE NATURAL INTEGRAL", IdUnidades = _ufc.IdUnidades };
        _linguica = new Produto { Descricao = "LINGUIÇA TOSCANA", IdUnidades = _ufc.IdUnidades };
        _mesofilos = new LabParametroAnalise { Descricao = "CONTAGEM DE AERÓBIOS MESÓFILOS", DescReduzida = "AERÓBIOS MESÓFILOS",
            IdAnalisesTipo = _micro.IdAnalisesTipo, IdAnalisesMetodos = _iso4833.IdAnalisesMetodos, VrUnitario = 85, Auditado = true };
        _salmonella = new LabParametroAnalise { Descricao = "PESQUISA DE SALMONELLA SPP.", DescReduzida = "SALMONELLA SPP.",
            IdAnalisesTipo = _micro.IdAnalisesTipo, IdAnalisesMetodos = _iso6579.IdAnalisesMetodos, VrUnitario = 120, Auditado = true };
        _ecoli = new LabParametroAnalise { Descricao = "CONTAGEM DE ESCHERICHIA COLI", DescReduzida = "E. COLI",
            IdAnalisesTipo = _micro.IdAnalisesTipo, IdAnalisesMetodos = _iso16649.IdAnalisesMetodos, VrUnitario = 95 };
        db.AddRange(_queijo, _iogurte, _linguica, _mesofilos, _salmonella, _ecoli);
        await db.SaveChangesAsync();
        Produto = _queijo.IdProdutos;
        Parametro = _mesofilos.IdLabParametrosAnalises;
    }

    private async Task UsuariosAsync(LabDbContext db, EmpresaFixa ativa)
    {
        await using var adm = await BancoTeste.NovoContextoAsync(new EmpresaFixa { AdminGlobal = true });
        var perfilAdm = await adm.Perfis.IgnoreQueryFilters().Where(p => p.IdEmpresas == Empresa && p.Nome == "ADMINISTRADOR").Select(p => p.IdPerfis).SingleAsync();
        var perfilAdmFilial = await adm.Perfis.IgnoreQueryFilters().Where(p => p.IdEmpresas == Filial && p.Nome == "ADMINISTRADOR").Select(p => p.IdPerfis).SingleAsync();
        PerfilRecepcao = await adm.Perfis.IgnoreQueryFilters().Where(p => p.IdEmpresas == Empresa && p.Nome == "RECEPÇÃO").Select(p => p.IdPerfis).SingleAsync();

        var operador = new Usuario { Login = LoginOperador, Nome = "ANA PAULA SOUZA", Email = "ana.souza@labexemplo.com.br", SenhaHash = ContaService.GerarHash(Senha), TrocarSenha = false };
        var recepcao = new Usuario { Login = "JOAO.PEREIRA", Nome = "JOÃO PEREIRA", SenhaHash = ContaService.GerarHash(Senha), TrocarSenha = true };
        var admin = new Usuario { Login = LoginAdministrador, Nome = "ADMINISTRADOR DO SISTEMA", SenhaHash = ContaService.GerarHash(Senha), AdminGlobal = true, TrocarSenha = false };
        adm.AddRange(operador, recepcao, admin);
        await adm.SaveChangesAsync();
        UsuarioOperador = operador.IdUsuarios;
        UsuarioRecepcao = recepcao.IdUsuarios;
        ativa.UsuarioId = operador.IdUsuarios;
        adm.AddRange(
            new UsuarioEmpresa { IdUsuarios = operador.IdUsuarios, IdEmpresas = Empresa, IdEntidadesFunc = Profissional, Padrao = true, Ativo = true },
            new UsuarioEmpresa { IdUsuarios = operador.IdUsuarios, IdEmpresas = Filial, Ativo = true },
            new UsuarioEmpresa { IdUsuarios = recepcao.IdUsuarios, IdEmpresas = Empresa, IdEntidadesFunc = _analista, Padrao = true, Ativo = true },
            new UsuarioEmpresa { IdUsuarios = admin.IdUsuarios, IdEmpresas = Empresa, Padrao = true, Ativo = true });
        await adm.SaveChangesAsync();
        adm.AddRange(
            new UsuarioPerfil { IdUsuarios = operador.IdUsuarios, IdEmpresas = Empresa, IdPerfis = perfilAdm },
            new UsuarioPerfil { IdUsuarios = operador.IdUsuarios, IdEmpresas = Filial, IdPerfis = perfilAdmFilial },
            new UsuarioPerfil { IdUsuarios = recepcao.IdUsuarios, IdEmpresas = Empresa, IdPerfis = PerfilRecepcao });
        await adm.SaveChangesAsync();
    }

    private async Task MovimentoAsync(LabDbContext db, EmpresaFixa ativa, string pastaBoletins)
    {
        var par = await db.EmpresasParametros.SingleAsync();
        var procedures = new Procedures(db, ativa);
        var idioma = await db.Idiomas.OrderBy(x => x.Descricao.StartsWith("PORTUG") ? 0 : 1).FirstAsync();
        var hoje = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURDATE() AS Value").SingleAsync();

        async Task<(LabProposta P, LabPropostaProduto Queijo)> PropostaAsync(Entidade cli, EntidadeEndereco end, EntidadeEmail eml, (Produto Prod, LabParametroAnalise[] Params, int Qtde)[] itens)
        {
            var p = new LabProposta { IdEntidades = cli.IdEntidades, IdEntidadesFunc = Profissional, IdEntEndLaudo = end.IdEntidadesEnderecos,
                IdEntContatoResultado = eml.IdEntidadesEmails, IdEntCobranca = cli.IdEntidades, IdEntEndCobranca = end.IdEntidadesEnderecos,
                IdLabPropostasStatus = par.IdLabPropostasStatusInicial!.Value, IdCondicoesPagtos = _trinta.IdCondicoesPagtos, PorcDesconto = 5,
                DtValidade = hoje.AddDays(30) };
            db.Add(p);
            await db.SaveChangesAsync();
            LabPropostaProduto? primeiro = null;
            foreach (var (prod, parametros, qtde) in itens)
            {
                var item = new LabPropostaProduto { IdLabPropostas = p.IdLabPropostas, IdProdutos = prod.IdProdutos, QtdeAmostras = qtde };
                db.Add(item);
                await db.SaveChangesAsync();
                primeiro ??= item;
                foreach (var pa in parametros)
                    db.Add(new LabPropostaProdAnalise { IdLabPropostas = p.IdLabPropostas, IdLabPropostasProdutos = item.IdLabPropostasProdutos,
                        IdLabParametrosAnalises = pa.IdLabParametrosAnalises, IdAnalisesMetodos = pa.IdAnalisesMetodos!.Value, IdIdiomas = idioma.IdIdiomas,
                        IdPrazos = pa == _salmonella ? _corridos.IdPrazos : _uteis.IdPrazos, QtdeAmostras = qtde, VrUnitario = pa.VrUnitario });
                await db.SaveChangesAsync();
            }
            return (p, primeiro!);
        }

        var (prop, itemQueijo) = await PropostaAsync(_laticinios, _endLaticinios, _emailLaticinios,
            [(_queijo, [_mesofilos, _salmonella, _ecoli], 3), (_iogurte, [_mesofilos, _ecoli], 2)]);
        var (aberta, _) = await PropostaAsync(_frigorifico, _endFrigorifico, _emailFrigorifico, [(_linguica, [_mesofilos, _salmonella], 4)]);
        PropostaAberta = aberta.IdLabPropostas;
        Proposta = prop.IdLabPropostas;
        var pedido = await procedures.GerarPedidoVendaAsync(prop.IdLabPropostas, hoje, "JULIANA MARTINS", "PC-2026-0457");
        if (!pedido.Sucesso) throw new InvalidOperationException(pedido.Mensagem);
        Pedido = pedido.Valor.IdPedido;

        async Task<LabHistAmostra> AmostraAsync(string lote)
        {
            var a = new LabHistAmostra
            {
                IdAmostrasTipo = _alimentos.IdAmostrasTipo, IdAnalisesTipo = _micro.IdAnalisesTipo, IdEntidades = _laticinios.IdEntidades,
                IdLabPropostas = prop.IdLabPropostas, IdLabPropostasProdutos = itemQueijo.IdLabPropostasProdutos, DtEntrega = hoje,
                HrEntrega = new TimeOnly(9, 30), LocalRecebimento = "RECEPÇÃO", IdEmbalagensTipos = _saco.IdEmbalagensTipos, QtdeEmbalagensEntregue = 2,
                NrLote = lote, FabricacaoDia = (byte)hoje.AddDays(-3).Day, FabricacaoMes = (byte)hoje.AddDays(-3).Month, FabricacaoAno = (short)hoje.AddDays(-3).Year,
                ValidadeMes = (byte)hoje.AddMonths(1).Month, ValidadeAno = (short)hoje.AddMonths(1).Year, NomeContato = "JULIANA MARTINS",
                TemperaturaVerificacao = 4.5m, AspectoAmostra = "SÓLIDO", Cor = "BRANCA", NomeColetor = "O PRÓPRIO CLIENTE",
                IdAmostrasStatus = par.IdAmostrasStatusInicial!.Value, IdEntidadesFuncResp = Profissional, IdEntidadesFuncDig = Profissional,
            };
            db.Add(a);
            await db.SaveChangesAsync();
            var t = await procedures.AmostraGerarTestesAsync(a.IdLabHistAmostras);
            if (!t.Sucesso) throw new InvalidOperationException(t.Mensagem);
            return a;
        }

        async Task<(LabResultadoAnalise R, List<LabResultadoParam> Ps)> ResultadoAsync(LabHistAmostra a)
        {
            var testes = await db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == a.IdLabHistAmostras).ToListAsync();
            var r = new LabResultadoAnalise { IdLabHistAmostras = a.IdLabHistAmostras, IdEntidadesFuncDig = Profissional };
            db.Add(r);
            await db.SaveChangesAsync();
            int Teste(LabParametroAnalise p) => testes.Single(t => t.IdLabParametrosAnalises == p.IdLabParametrosAnalises).IdLabHistAmostrasTestes;
            var ps = new List<LabResultadoParam>
            {
                new() { IdLabResultadosAnalises = r.IdLabResultadosAnalises, IdLabHistAmostrasTestes = Teste(_mesofilos), DtInicial = hoje, DtResultado = hoje,
                    IdEntidadesFuncAnalise = _analista, TipoResultado = true, VrResultado = "2,3", VrExpResultado = 3, IdUnidades = _ufc.IdUnidades,
                    TipoResultadoEspecif = true, VrResultadoEspecif = "5,0", VrExpEspecif = 5, IdUnidadesEspecif = _ufc.IdUnidades },
                new() { IdLabResultadosAnalises = r.IdLabResultadosAnalises, IdLabHistAmostrasTestes = Teste(_salmonella), DtInicial = hoje, DtResultado = hoje,
                    IdEntidadesFuncAnalise = _analista, TipoResultado = false, IdLabTiposResultados = _ausencia.IdLabTiposResultados,
                    TipoResultadoEspecif = false, IdLabTipoResultadosEsp = _ausencia.IdLabTiposResultados },
                new() { IdLabResultadosAnalises = r.IdLabResultadosAnalises, IdLabHistAmostrasTestes = Teste(_ecoli), DtInicial = hoje, DtResultado = hoje,
                    IdEntidadesFuncAnalise = _analista, TipoResultado = true, SimboloGrandeza = "<", VrResultado = "1,0", VrExpResultado = 1, IdUnidades = _ufc.IdUnidades },
            };
            db.AddRange(ps);
            await db.SaveChangesAsync();
            return (r, ps);
        }

        async Task<LabBoletimAnalise> BoletimAsync(LabHistAmostra a, LabResultadoAnalise r, List<LabResultadoParam> ps)
        {
            await db.Entry(ps[0]).ReloadAsync(); await db.Entry(ps[1]).ReloadAsync(); await db.Entry(ps[2]).ReloadAsync();
            var b = new LabBoletimAnalise { IdLabHistAmostras = a.IdLabHistAmostras, IdLabResultadosAnalises = r.IdLabResultadosAnalises,
                IdEntidadesFuncGer = Profissional, IdEntidadesFuncResp = Profissional, TxtObservacao = "Amostra recebida lacrada e refrigerada.",
                TxtMetodologiaAcred = "AERÓBIOS MESÓFILOS: ISO 4833-1:2013\nSALMONELLA SPP.: ISO 6579-1:2017", TxtMetodologiaUtil = "E. COLI: ISO 16649-2:2001" };
            db.Add(b);
            await db.SaveChangesAsync();
            db.AddRange(
                new LabBoletimParametro { IdLabBoletimAnalises = b.IdLabBoletimAnalises, IdLabResultadosParam = ps[0].IdLabResultadosParam, Ordem = 1,
                    DescParametro = _mesofilos.DescReduzida, ResultadoConcat = ps[0].ResultadoFormatado, Acreditado = true },
                new LabBoletimParametro { IdLabBoletimAnalises = b.IdLabBoletimAnalises, IdLabResultadosParam = ps[1].IdLabResultadosParam, Ordem = 2,
                    DescParametro = _salmonella.DescReduzida, ResultadoConcat = ps[1].ResultadoFormatado, Acreditado = true },
                new LabBoletimParametro { IdLabBoletimAnalises = b.IdLabBoletimAnalises, IdLabResultadosParam = ps[2].IdLabResultadosParam, Ordem = 3,
                    DescParametro = _ecoli.DescReduzida, ResultadoConcat = ps[2].ResultadoFormatado });
            await db.SaveChangesAsync();
            return b;
        }

        // 1) arquivada, com boletim emitido
        var emitida = await AmostraAsync("L2026-101");
        var (r1, ps1) = await ResultadoAsync(emitida);
        var b1 = await BoletimAsync(emitida, r1, ps1);
        var boletins = new BoletimService(db, ativa, procedures, new AmostraService(db, ativa, procedures), new ArmazemBoletins(pastaBoletins), NullLogger<BoletimService>.Instance);
        var emissao = await boletins.EmitirAsync(b1.IdLabBoletimAnalises);
        if (!emissao.Sucesso) throw new InvalidOperationException(emissao.Mensagem);
        db.Add(new LabMovAmostra { IdLabHistAmostras = emitida.IdLabHistAmostras, DataMov = DateTime.Now, Qtde = 1, ES = EntradaSaida.Saida,
            Justificativa = "CONSUMIDA NA ANÁLISE", IdEntidadesFunc = _analista });
        db.Add(new LabLocalAmostra { IdLabHistAmostras = emitida.IdLabHistAmostras, Status = SituacaoArquivo.Arquivada, DtHrArquivo = DateTime.Now,
            NrArmario = "A1", NrPrateleira = "3", NrCaixa = "12", Observacao = "CONTRAPROVA" });
        await db.SaveChangesAsync();

        // 2) com resultado e boletim ainda não emitido
        var comResultado = await AmostraAsync("L2026-102");
        var (r2, ps2) = await ResultadoAsync(comResultado);
        var b2 = await BoletimAsync(comResultado, r2, ps2);

        // 3) aguardando análise (aparece na fila)
        var pendente = await AmostraAsync("L2026-103");

        AmostraEmitida = emitida.IdLabHistAmostras;
        AmostraComResultado = comResultado.IdLabHistAmostras;
        AmostraPendente = pendente.IdLabHistAmostras;
        Resultado = r2.IdLabResultadosAnalises;
        BoletimAberto = b2.IdLabBoletimAnalises;
        BoletimEmitido = b1.IdLabBoletimAnalises;
        EventoAuditoria = await db.Auditoria.IgnoreQueryFilters().Where(x => x.IdEmpresas == Empresa).MaxAsync(x => x.IdAuditoria);
    }
}
