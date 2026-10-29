using System.Text;
using LabControl.Data;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 8.4 (T06): revisão/emissão com usuário e motivo, login com falha, diferença antes/depois, empresa, CSV e histórico da amostra.</summary>
public class AuditoriaConsultaTests
{
    private static AuditoriaService Servico(Cenario c, EmpresaFixa? ctx = null) =>
        new(c.Db, ctx ?? c.Ativa, new AmostraService(c.Db, ctx ?? c.Ativa, c.Procedures));

    private static async Task<string> LoginAsync(Cenario c) =>
        await c.Db.Usuarios.Where(u => u.IdUsuarios == c.Ativa.UsuarioId).Select(u => u.Login).SingleAsync();

    [Fact]
    public async Task Revisao_e_emissao_aparecem_com_usuario_e_motivo_e_a_diferenca_e_legivel()
    {
        await using var c = await Cenario.CriarAsync();
        var (amo, _, bol) = await c.AmostraComBoletimAsync(await c.HojeAsync());
        Assert.True((await c.Procedures.EmitirBoletimAsync(bol.IdLabBoletimAnalises, "x.pdf", new string('a', 64))).Sucesso);
        var rev = await c.Procedures.GerarRevisaoAmostraAsync(amo.IdLabHistAmostras, "LOTE DIGITADO ERRADO");
        Assert.True(rev.Sucesso, rev.Mensagem);
        var s = Servico(c);
        var login = await LoginAsync(c);

        var revisoes = await s.ListarAsync(new AuditoriaFiltroVM { Acao = "R" });
        var daAmostra = Assert.Single(revisoes.Itens, i => i.Tabela == "LAB_HIST_AMOSTRAS");
        Assert.Equal((login, "Revisão", "Amostras"), (daAmostra.Usuario, daAmostra.Acao, daAmostra.Assunto));
        Assert.Contains("motivo: LOTE DIGITADO ERRADO", daAmostra.Resumo);
        Assert.Contains("ID_RESULTADO_NOVO", (await s.DetalheAsync(daAmostra.Id))!.Campos.Select(x => x.Campo));   // a cascata vai no mesmo evento
        var emissao = Assert.Single((await s.ListarAsync(new AuditoriaFiltroVM { Acao = "M" })).Itens);
        Assert.Equal(($"/Boletins/Editar/{bol.IdLabBoletimAnalises}", login), (emissao.Link, emissao.Usuario));

        // alteração: só as colunas alteradas, destacadas
        c.Proposta.PorcDesconto = 5;
        await c.Db.SaveChangesAsync();
        var alteracao = Assert.Single((await s.ListarAsync(new AuditoriaFiltroVM { Assunto = "PROPOSTAS", Acao = "A", Registro = c.Proposta.IdLabPropostas.ToString() })).Itens,
            i => i.Tabela == "LAB_PROPOSTAS");
        var detalhe = (await s.DetalheAsync(alteracao.Id))!;
        var campo = Assert.Single(detalhe.Campos, x => x.Campo == "PORC_DESCONTO");
        Assert.True(campo.Mudou);
        Assert.Equal(("10", "5"), (campo.Antes, campo.Depois));
        // inclusão: um lado só, nada destacado
        var inclusao = (await s.ListarAsync(new AuditoriaFiltroVM { Assunto = "AMOSTRAS", Acao = "I" })).Itens.First(i => i.Tabela == "LAB_HIST_AMOSTRAS");
        Assert.DoesNotContain((await s.DetalheAsync(inclusao.Id))!.Campos, x => x.Mudou);

        // CSV com o filtro
        var csv = Encoding.UTF8.GetString(await s.CsvAsync(new AuditoriaFiltroVM { Acao = "R" }));
        Assert.StartsWith("﻿Data/hora;Usuário;Ação;Assunto;Tabela;Registro;IP;Empresa;Resumo;Antes;Depois", csv);
        Assert.Contains("LOTE DIGITADO ERRADO", csv);

        // histórico da amostra: as duas revisões pelo número (com ou sem o " R01")
        var hist = await s.HistoricoAmostraAsync(amo.NrAmostra);
        Assert.Equal([amo.NrAmostra, amo.NrAmostra + " R01"], hist.Revisoes.Select(r => r.NrAmostra));
        Assert.Contains(hist.Revisoes[1].Linhas, l => l.Resumo.Contains("motivo: LOTE DIGITADO ERRADO"));
        Assert.StartsWith("Amostra 99-99999 não encontrada", (await s.HistoricoAmostraAsync("99-99999")).Mensagem);
    }

    [Fact]
    public async Task Empresa_ativa_logins_dos_seus_usuarios_e_admin_global_ve_tudo()
    {
        await using var c = await Cenario.CriarAsync();
        await using var outra = await Cenario.CriarAsync();   // outra empresa, com os seus eventos
        var daEmpresa = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(daEmpresa.IdUsuarios, c.Empresa.IdEmpresas);
        var deFora = await UsuariosTeste.CriarAsync();
        await using (var db = await BancoTeste.NovoContextoAsync(new EmpresaFixa()))
        {
            var conta = UsuariosTeste.Servico(db, new EmpresaFixa());   // login: ainda sem empresa ativa
            Assert.False((await conta.EntrarAsync(daEmpresa.Login, "errada")).Sucesso);
            Assert.False((await conta.EntrarAsync(deFora.Login, "errada")).Sucesso);
            Assert.True((await conta.EntrarAsync(daEmpresa.Login, UsuariosTeste.Senha)).Sucesso);
        }

        var s = Servico(c);
        var logins = (await s.ListarAsync(new AuditoriaFiltroVM { Acao = "L", Usuario = daEmpresa.Login })).Itens;
        Assert.Equal(2, logins.Count);
        Assert.Contains(logins, l => l.Resumo == $"falhou ({daEmpresa.Login}: senha)");
        Assert.Contains(logins, l => l.Resumo == $"entrou ({daEmpresa.Login})");
        // login de quem não tem acesso à empresa e eventos da outra empresa: não aparecem, nem pelo detalhe
        Assert.Empty((await s.ListarAsync(new AuditoriaFiltroVM { Acao = "L", Usuario = deFora.Login })).Itens);
        var todas = new AuditoriaFiltroVM { Assunto = "PROPOSTAS", TodasEmpresas = true };   // sem ADMIN_GLOBAL o "todas" é ignorado
        Assert.All((await s.ListarAsync(todas)).Itens, i => Assert.Null(i.Empresa is null ? null : i.Empresa.Contains(outra.Empresa.CodEmpresa) ? i.Empresa : null));
        Assert.False(todas.TodasEmpresas);
        var daOutra = await outra.Db.Auditoria.Where(a => a.IdEmpresas == outra.Empresa.IdEmpresas).Select(a => a.IdAuditoria).FirstAsync();
        Assert.Null(await s.DetalheAsync(daOutra));

        // ADMIN_GLOBAL com "todas": o login de qualquer usuário e a outra empresa
        var admin = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, AdminGlobal = true };
        var g = Servico(c, admin);
        Assert.Single((await g.ListarAsync(new AuditoriaFiltroVM { Acao = "L", Usuario = deFora.Login, TodasEmpresas = true })).Itens);
        Assert.NotNull(await g.DetalheAsync(daOutra));
        Assert.Contains((await g.ListarAsync(new AuditoriaFiltroVM { Assunto = "PROPOSTAS", TodasEmpresas = true, Registro = outra.Proposta.IdLabPropostas.ToString() })).Itens,
            i => i.Empresa!.StartsWith(outra.Empresa.CodEmpresa));
    }
}
