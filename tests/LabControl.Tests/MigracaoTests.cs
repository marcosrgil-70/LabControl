using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.Services.Migracao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 3.5, da 4.1 e da 5.1: migração M-01…M-18 do Firebird real para um banco vazio recriado a cada execução
/// (<c>labcontrol_migracao_teste</c>). A origem é a cópia fixa da 9.1 (<c>C:\Project\backup\DBMAIS-9.1.FDB</c>, restaurada
/// do <c>DBMAIS-20261029.fbk</c>), e não o <c>DBMAIS.FDB</c>, que recebe cópias novas do laboratório: as contagens abaixo são
/// as do aceite. Sem o legado nesta máquina o teste só avisa.
/// Roda na coleção <see cref="ColecaoMigracao"/>: sozinho, depois das outras (pendência 18).
/// </summary>
[Collection(ColecaoMigracao.Nome)]
public class MigracaoTests(ITestOutputHelper saida)
{
    private const string Banco = "labcontrol_migracao_teste";
    private const string Fdb = @"C:\Project\backup\DBMAIS-9.1.FDB";
    private const string SenhaProvisoria = "Migracao#2026";

    private static MigracaoService Servico() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:LabControl"] = BancoTeste.ConnectionString }).Build());

    private static PedidoMigracao Pedido(string banco = Banco, string senha = SenhaProvisoria) => new(Fdb, "SYSDBA", "masterkey", banco, senha, "127.0.0.1");

    [Fact]
    public async Task Migra_cadastros_e_movimento_do_legado_com_conferencias_verdes_e_login_com_troca_de_senha()
    {
        if (!File.Exists(Fdb)) { saida.WriteLine($"Legado {Fdb} ausente: migração não testada."); return; }
        await BancoTeste.RecriarAsync(Banco);
        var s = Servico();
        var v = await s.ValidarAsync(Pedido());
        Assert.True(v.Sucesso, v.Mensagem);

        var relatorio = MigracaoService.NovoRelatorio(Banco);
        await s.ExecutarAsync(Pedido(), relatorio);
        var (log, anomalias, conferencias) = relatorio.Copia();
        foreach (var l in log) saida.WriteLine(l);
        foreach (var a in anomalias) saida.WriteLine($"ANOMALIA {a.Etapa}: {a.Texto}");
        Assert.True(relatorio.Sucesso, string.Join("\n", relatorio.Etapas.Select(e => $"{e.Codigo} {e.Situacao} {e.Erro}")));
        Assert.All(conferencias, c => Assert.True(c.Ok, $"{c.Etapa} {c.Item}: esperado {c.Esperado}, encontrado {c.Encontrado}"));

        // contagens do aceite (F3 3.5), direto no destino
        var ctx = new EmpresaFixa { AdminGlobal = true };
        var opcoes = new DbContextOptionsBuilder<Data.LabDbContext>();
        opcoes.Configurar(s.ConexaoDestino(Banco), ctx);
        await using var db = new Data.LabDbContext(opcoes.Options, ctx);
        ctx.EmpresaId = await db.Empresas.Select(e => e.IdEmpresas).SingleAsync();
        Assert.Equal(
            (527, 664, 695, 538, 1915, 462, 176, 15),
            (await db.Entidades.CountAsync(e => e.IdLegado != null), await db.EntidadesEmails.CountAsync(e => e.IdLegado != null),
             await db.EntidadesFones.CountAsync(e => e.IdLegado != null), await db.EntidadesEnderecos.CountAsync(e => e.IdLegado != null),
             await db.Produtos.CountAsync(), await db.LabParametrosAnalises.CountAsync(), await db.AnalisesMetodos.CountAsync(), await db.Usuarios.CountAsync()));
        Assert.Equal(1, await db.Entidades.CountAsync(e => e.TipoEmpresaUsuaria));
        Assert.Equal(52, await db.LabParametrosAnalises.CountAsync(p => p.Auditado));
        Assert.True(await db.EntidadesFuncionarios.CountAsync() >= 16);

        // parte 2 (4.1): contagens do aceite (doc 13 §3). Produtos: 5.761 − 33 de 21 propostas excluídas no legado + 41
        // criados para amostras de propostas sem produtos; análises: 19.252 − 84 das propostas excluídas
        Assert.Equal((2358, 5769, 19168, 5770, 20551),
            (await db.LabPropostas.CountAsync(), await db.LabPropostasProdutos.CountAsync(), await db.LabPropostasProdanalises.CountAsync(),
             await db.LabHistAmostras.CountAsync(), await db.LabHistAmostrasTestes.CountAsync()));
        Assert.Equal(1433, await db.PedidosVenda.CountAsync()); // 1.434 números distintos; uma proposta tinha dois
        Assert.Equal([62, 611, 511, 663, 2482, 1440],
            await db.LabHistAmostras.Where(a => a.AnoAmostra >= 2013 && a.AnoAmostra <= 2018).GroupBy(a => a.AnoAmostra)
                .OrderBy(g => g.Key).Select(g => g.Count() - (g.Key == 2014 ? 1 : 0)).ToListAsync()); // +1: a do ano 0 entrou em 2014
        // número da amostra recalculado pelo banco = o impresso pelo legado (conferência da M-14 compara todos)
        var amostra = await db.LabHistAmostras.Where(a => a.Revisao > 0).OrderBy(a => a.IdLegado).FirstAsync();
        Assert.Matches(@"^\d{2}-\d{5}-\d{2}/\d{2} R\d{2}$", amostra.NrAmostra);
        Assert.NotNull(amostra.IdAmostraRaiz);
        // a próxima amostra/proposta continua depois do legado (sequências da numeração)
        Assert.True(await db.SequenciasDocumentos.CountAsync(sq => sq.TipoDocumento == "AMOSTRA") >= 6);
        Assert.Equal(await db.PedidosVenda.MaxAsync(p => p.NrPedido),
            await db.SequenciasDocumentos.Where(sq => sq.TipoDocumento == "PEDIDO_VENDA").Select(sq => sq.UltimoNumero).SingleAsync());
        // log do legado entra marcado, sem usuário
        var logLegado = await db.LabHistAmostrasLog.FirstAsync();
        Assert.Contains("\"ORIGEM\":\"LEGADO\"", logLegado.Dados.Replace(" ", ""));
        Assert.Null(logLegado.IdUsuarios);

        // parte 3 (5.1): resultados e boletins; todos os boletins congelados como emitidos (D-11)
        // parâmetros: 19.528 − 31 repetidos no mesmo resultado; linhas: 19.615 − 146 antigas/apagadas/repetidas (anomalias da M-17)
        Assert.Equal((5345, 19497, 5312, 19469),
            (await db.LabResultadosAnalises.CountAsync(), await db.LabResultadosParam.CountAsync(),
             await db.LabBoletimAnalises.CountAsync(), await db.LabBoletimParametros.CountAsync()));
        Assert.Equal(0, await db.LabBoletimAnalises.CountAsync(b => b.DhEmissao == null));
        var emitido = await db.EmpresasParametros.Select(p => p.IdBoletinsStatusEmitido).SingleAsync();
        Assert.Equal("ENVIADO AO CLIENTE", await db.BoletinsStatus.Where(st => st.IdBoletinsStatus == emitido).Select(st => st.Descricao).SingleAsync());
        Assert.Equal(0, await db.LabBoletimAnalises.CountAsync(b => b.IdBoletinsStatus != emitido));
        // pendência 23 (5.5): pedidos com tudo recebido e todos os boletins emitidos ficam concluídos
        var situacoes = await db.PedidosVenda.GroupBy(p => p.Situacao).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.N);
        saida.WriteLine("Pedidos por situação: " + string.Join("; ", situacoes.Select(s => $"{s.Key} {s.Value}")));
        // 309 dos 449 em execução têm tudo emitido, mas receberam menos amostras que o contratado (anomalia da M-17)
        Assert.Equal((974, 449, 10), (situacoes.GetValueOrDefault(SituacaoPedido.Concluido), situacoes.GetValueOrDefault(SituacaoPedido.EmExecucao),
            situacoes.GetValueOrDefault(SituacaoPedido.Aberto)));
        Assert.Equal(1433, situacoes.Values.Sum());

        // o texto do resultado fica como foi impresso (boletim 04-00072-01/14 do legado)
        Assert.Contains("<  1", await db.LabBoletimParametros.Where(bp => bp.LabBoletimAnalise.IdLegado == 209).Select(bp => bp.ResultadoConcat).ToListAsync());
        Assert.Equal("04-00072-01/14", await db.LabBoletimAnalises.Where(b => b.IdLegado == 209).Select(b => b.CodBoletim).SingleAsync());
        // com a migração desligada (conexão nova do app), boletim migrado não se altera
        var boletim = await db.LabBoletimAnalises.Select(b => b.IdLabBoletimAnalises).FirstAsync();
        var recusa = await Assert.ThrowsAnyAsync<Exception>(() =>
            db.Database.ExecuteSqlRawAsync("UPDATE LAB_BOLETIM_ANALISES SET TXT_OBSERVACAO = 'x' WHERE ID_LAB_BOLETIM_ANALISES = {0}", boletim));
        Assert.Contains("foi emitido", ErrosMySql.MensagemDeNegocio(recusa));

        // encoding: acentos chegam como acentos (nada de "Ã£")
        var nomes = await db.Entidades.Select(e => e.Nome).ToListAsync();
        Assert.Contains(nomes, n => n.Any(ch => "ÁÉÍÓÚÂÊÔÃÕÇáéíóúâêôãõç".Contains(ch)));
        Assert.DoesNotContain(nomes, n => n.Contains('Ã') && n.Contains('£'));

        // login de um usuário migrado com a senha única → troca obrigatória
        var usuario = await db.Usuarios.FirstAsync(u => !u.AdminGlobal);
        var conta = UsuariosTeste.Servico(db, ctx);
        var entrar = await conta.EntrarAsync(usuario.Login, SenhaProvisoria);
        Assert.True(entrar.Sucesso, entrar.Mensagem);
        var identidade = await conta.MontarIdentidadeAsync(entrar.Valor, ctx.EmpresaId);
        Assert.True(identidade.Sucesso, identidade.Mensagem);
        Assert.Equal("1", identidade.Valor.FindFirst(LabClaims.TrocarSenha)?.Value);
        Assert.True((await db.Usuarios.SingleAsync(u => u.UsucodLegado == 1)).AdminGlobal);

        // o destino agora tem dados: uma 2ª migração no mesmo banco é recusada
        var denovo = await s.ValidarAsync(Pedido());
        Assert.Contains("não está vazio", denovo.Mensagem);
    }

    [Fact]
    public async Task Instalacao_nova_migra_no_proprio_banco_e_falha_desfaz_tudo()
    {
        // instalador sem backup (decisão do usuário, 29/10/2026): um banco só, com a empresa digitada e o ADMINISTRADOR /
        // Admin123; a T07 migra o legado nele mesmo, substituindo esse conteúdo inicial
        if (!File.Exists(Fdb)) { saida.WriteLine($"Legado {Fdb} ausente: migração não testada."); return; }
        const string banco = "labcontrol_instalacao_teste";
        await BancoTeste.RecriarAsync(banco, "Laboratório Instalado Ltda");
        var s = new MigracaoService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:LabControl"] = new MySqlConnector.MySqlConnectionStringBuilder(BancoTeste.ConnectionString) { Database = banco }.ConnectionString }).Build());
        Assert.True(s.SubstituiBancoEmUso(banco));
        var v = await s.ValidarAsync(Pedido(banco));
        Assert.True(v.Sucesso, v.Mensagem);

        var ctx = new EmpresaFixa { AdminGlobal = true };
        var opcoes = new DbContextOptionsBuilder<Data.LabDbContext>();
        opcoes.Configurar(s.ConexaoDestino(banco), ctx);

        // interrompida no meio (já apagou o conteúdo do instalador e gravou etapas): nada fica, o banco volta como estava
        using (var cancelar = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            try { await s.ExecutarAsync(Pedido(banco), MigracaoService.NovoRelatorio(banco), cancelar.Token); } catch (OperationCanceledException) { }
        await using (var db = new Data.LabDbContext(opcoes.Options, ctx))
        {
            Assert.Equal("LABORATÓRIO INSTALADO LTDA", await db.Empresas.Select(e => e.RazaoSocial).SingleAsync());
            Assert.Equal([ContaService.LoginAdministrador], await db.Usuarios.Select(u => u.Login).ToListAsync());
            Assert.Equal(0, await db.Produtos.CountAsync());
        }

        var relatorio = MigracaoService.NovoRelatorio(banco);
        await s.ExecutarAsync(Pedido(banco), relatorio);
        var (log, _, conferencias) = relatorio.Copia();
        foreach (var l in log) saida.WriteLine(l);
        Assert.True(relatorio.Sucesso, string.Join("\n", relatorio.Etapas.Select(e => $"{e.Codigo} {e.Situacao} {e.Erro}")));
        Assert.All(conferencias, c => Assert.True(c.Ok, $"{c.Etapa} {c.Item}: esperado {c.Esperado}, encontrado {c.Encontrado}"));

        await using (var db = new Data.LabDbContext(opcoes.Options, ctx))
        {
            // a empresa e os usuários são os do legado (a do instalador saiu); as mesmas contagens do banco vazio
            var empresa = await db.Empresas.SingleAsync();
            Assert.NotEqual("LABORATÓRIO INSTALADO LTDA", empresa.RazaoSocial);
            ctx.EmpresaId = empresa.IdEmpresas;
            Assert.Equal((15, 2358, 5770, 5312), (await db.Usuarios.CountAsync(), await db.LabPropostas.CountAsync(),
                await db.LabHistAmostras.CountAsync(), await db.LabBoletimAnalises.CountAsync()));
            Assert.Equal(1, await db.Entidades.CountAsync(e => e.TipoEmpresaUsuaria));
            // o ADMINISTRADOR migrado entra com Admin123, sem troca (como o do instalador); os outros, com a provisória
            var conta = UsuariosTeste.Servico(db, ctx);
            var entrar = await conta.EntrarAsync(ContaService.LoginAdministrador, ContaService.SenhaDesenvolvimento);
            Assert.True(entrar.Sucesso, entrar.Mensagem);
            var identidade = await conta.MontarIdentidadeAsync(entrar.Valor, ctx.EmpresaId);
            Assert.True(identidade.Sucesso, identidade.Mensagem);
            Assert.NotEqual("1", identidade.Valor.FindFirst(LabClaims.TrocarSenha)?.Value);
            Assert.Equal(1, (await db.Usuarios.SingleAsync(u => u.Login == ContaService.LoginAdministrador)).UsucodLegado);
        }

        // agora o banco tem os dados do laboratório: não é substituído de novo
        Assert.Contains("já tem cadastros ou movimentos", (await s.ValidarAsync(Pedido(banco))).Mensagem);
        await using (var con = new MySqlConnector.MySqlConnection(BancoTeste.ConnectionString))
        {
            await con.OpenAsync();
            await using var cmd = con.CreateCommand();
            cmd.CommandText = $"DROP DATABASE IF EXISTS {banco}";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task Validacao_do_pedido()
    {
        var s = Servico();
        Assert.Equal("SenhaProvisoria", (await s.ValidarAsync(Pedido(senha: "123"))).Campo);
        Assert.Equal("BancoDestino", (await s.ValidarAsync(Pedido(banco: "labcontrol"))).Campo);
        Assert.Equal("BancoDestino", (await s.ValidarAsync(Pedido(banco: "x; drop"))).Campo);
        var inexistente = await s.ValidarAsync(Pedido(banco: "labcontrol_nao_existe_" + Guid.NewGuid().ToString("N")[..6]));
        Assert.Contains("recriar.ps1", inexistente.Mensagem);
        // o banco em uso (o de testes) tem cadastros: não é substituído
        Assert.Contains("já tem cadastros ou movimentos", (await s.ValidarAsync(Pedido(banco: BancoTeste.NomeBanco))).Mensagem);
    }
}

/// <summary>
/// A migração recria um banco e grava milhares de linhas: em paralelo com o resto da suíte, as outras classes esperavam
/// lock no <c>SP_INICIALIZAR_EMPRESA</c>/<c>SEQUENCIAS_DOCUMENTOS</c> e estouravam 30 s (pendência 18). Sem paralelismo,
/// o xUnit roda esta coleção sozinha, depois das coleções paralelas.
/// </summary>
[CollectionDefinition(Nome, DisableParallelization = true)]
public sealed class ColecaoMigracao
{
    public const string Nome = "Migração (sozinha)";
}
