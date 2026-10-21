using LabControl.Data;
using LabControl.Infra;
using LabControl.Services.Migracao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 3.5: migração parte 1 (M-01…M-10) do Firebird real (<c>C:\Project\Laboratorio\DBMAIS.FDB</c>) para um
/// banco vazio recriado a cada execução (<c>labcontrol_migracao_teste</c>). Sem o legado nesta máquina o teste só avisa.
/// </summary>
public class MigracaoParte1Tests(ITestOutputHelper saida)
{
    private const string Banco = "labcontrol_migracao_teste";
    private const string Fdb = @"C:\Project\Laboratorio\DBMAIS.FDB";
    private const string SenhaProvisoria = "Migracao#2026";

    private static MigracaoService Servico() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:LabControl"] = BancoTeste.ConnectionString }).Build());

    private static PedidoMigracao Pedido(string banco = Banco, string senha = SenhaProvisoria) => new(Fdb, "SYSDBA", "masterkey", banco, senha, "127.0.0.1");

    [Fact]
    public async Task Parte1_migra_os_cadastros_do_legado_com_conferencias_verdes_e_login_com_troca_de_senha()
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
    public async Task Validacao_do_pedido()
    {
        var s = Servico();
        Assert.Equal("SenhaProvisoria", (await s.ValidarAsync(Pedido(senha: "123"))).Campo);
        Assert.Equal("BancoDestino", (await s.ValidarAsync(Pedido(banco: "labcontrol"))).Campo);
        Assert.Equal("BancoDestino", (await s.ValidarAsync(Pedido(banco: "x; drop"))).Campo);
        var inexistente = await s.ValidarAsync(Pedido(banco: "labcontrol_nao_existe_" + Guid.NewGuid().ToString("N")[..6]));
        Assert.Contains("recriar.ps1", inexistente.Mensagem);
        // banco com dados (o de testes) é recusado
        Assert.Contains("não está vazio", (await s.ValidarAsync(Pedido(banco: BancoTeste.NomeBanco))).Mensagem);
    }
}
