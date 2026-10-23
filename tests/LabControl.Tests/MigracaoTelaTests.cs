using System.Net;

namespace LabControl.Tests;

/// <summary>T07 pela web: só ADMIN_GLOBAL; pedido inválido volta com a mensagem (a execução real está em <see cref="MigracaoTests"/>).</summary>
public class MigracaoTelaTests(AppWeb app) : IClassFixture<AppWeb>
{
    [Fact]
    public async Task So_admin_global_e_destino_com_dados_recusado()
    {
        await using var c = await Cenario.CriarAsync();
        var comum = await UsuariosTeste.ComPermissoesAsync(c, "EMPRESAS.CONSULTAR");
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(comum.Login);
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Migracao")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Migracao/Relatorio")).StatusCode);
        }

        var global = await UsuariosTeste.CriarAsync(adminGlobal: true);
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(global.Login);
            await web.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", c.Empresa.IdEmpresas.ToString()));
            var html = await (await web.GetAsync("/Migracao")).HtmlAsync();
            Assert.Contains("Executar migração", html);
            Assert.Contains(@"recriar.ps1 -Banco labcontrol_migracao -Forcar", html);

            // o banco de testes tem dados: recusado com o comando para criar um vazio; senhas não voltam para a tela
            var recusa = await (await web.PostarAsync("/Migracao", "/Migracao/Executar",
                ("CaminhoFirebird", @"C:\Project\Laboratorio\DBMAIS.FDB"), ("UsuarioFirebird", "SYSDBA"), ("SenhaFirebird", "masterkey"),
                ("BancoDestino", BancoTeste.NomeBanco), ("SenhaProvisoria", "Migracao#2026"), ("ConfirmarSenha", "Migracao#2026"))).HtmlAsync();
            Assert.Contains($"O banco {BancoTeste.NomeBanco} não está vazio", recusa);
            Assert.DoesNotContain("Migracao#2026", recusa);

            var fraca = await (await web.PostarAsync("/Migracao", "/Migracao/Executar",
                ("CaminhoFirebird", "x"), ("UsuarioFirebird", "SYSDBA"), ("BancoDestino", "labcontrol_migracao"),
                ("SenhaProvisoria", "123"), ("ConfirmarSenha", "123"))).HtmlAsync();
            Assert.Contains("Senha provisória:", fraca);
        }
    }
}
