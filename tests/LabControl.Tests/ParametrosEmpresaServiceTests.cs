using System.Net;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.6 (docs/telas/T05-parametros-empresa.md, P03).</summary>
public class ParametrosEmpresaServiceTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static ParametrosEmpresaService Servico(LabDbContext db, IEmpresaContexto ctx) => new(db, ctx, new ServicoAuditoria(db, ctx));

    private static async Task<Resultado> GravarDiasRetroativosAsync(Cenario c, int dias)
    {
        var s = Servico(c.Db, c.Ativa);
        var vm = (await s.CarregarAsync())!;
        vm.DiasRetroativosEntrega = dias;
        var r = await s.AlterarAsync(vm);
        c.Db.ChangeTracker.Clear();
        return r;
    }

    [Fact]
    public async Task Dias_retroativos_mudam_na_hora_a_regra_do_recebimento()
    {
        await using var c = await Cenario.CriarAsync();
        var entrega = (await c.HojeAsync()).AddDays(-20);

        Assert.True((await GravarDiasRetroativosAsync(c, 30)).Sucesso);
        c.Db.Add(c.NovaAmostra(entrega));
        Assert.Equal(1, await c.Db.SaveChangesAsync()); // 20 dias atrás: dentro do limite, sem liberação
        c.Db.ChangeTracker.Clear();

        Assert.True((await GravarDiasRetroativosAsync(c, 7)).Sucesso);
        c.Db.Add(c.NovaAmostra(entrega));
        var r = await ErrosMySql.CapturarAsync(() => c.Db.SaveChangesAsync());
        Assert.Equal("Data de entrega muito antiga (verifique o ano digitado). Requer confirmação e permissão.", r.Mensagem);
        c.Db.ChangeTracker.Clear();

        Assert.Equal("Informe de 0 a 365 dias.", (await GravarDiasRetroativosAsync(c, 366)).Mensagem);
        Assert.True(await c.Db.Auditoria.AnyAsync(a => a.Tabela == "EMPRESAS_PARAMETROS" && a.Acao == "A" && a.DadosDepois!.Contains("DIAS_RETROATIVOS_ENTREGA")));
    }

    [Fact]
    public async Task Combos_e_gravacao_so_com_status_da_empresa_ativa()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var s = Servico(a.Db, a.Ativa);
        var vm = (await s.CarregarAsync())!;

        var daEmpresa = await a.Db.AmostrasStatus.Select(x => x.IdAmostrasStatus).ToListAsync();
        var outra = await b.Db.AmostrasStatus.Select(x => x.IdAmostrasStatus).ToListAsync();
        Assert.All(vm.StatusAmostras, o => Assert.Contains(o.Id, daEmpresa));
        Assert.DoesNotContain(vm.StatusAmostras, o => outra.Contains(o.Id));
        Assert.NotEmpty(vm.StatusPropostas);
        Assert.NotEmpty(vm.StatusBoletins);

        vm.IdAmostrasStatusInicial = outra[0];
        var r = await s.AlterarAsync(vm);
        Assert.Equal(("Status não encontrado.", nameof(vm.IdAmostrasStatusInicial)), (r.Mensagem, r.Campo));

        vm = (await s.CarregarAsync())!;
        vm.IdBoletinsStatusEmitido = vm.IdBoletinsStatusInicial;
        Assert.Equal(nameof(vm.IdBoletinsStatusEmitido), (await s.AlterarAsync(vm)).Campo);
    }

    [Fact]
    public async Task Ajuste_da_numeracao_recusa_valor_menor_e_fica_na_auditoria()
    {
        await using var c = await Cenario.CriarAsync();
        var hoje = await c.HojeAsync();
        var amo = c.NovaAmostra(hoje);
        c.Db.Add(amo);
        await c.Db.SaveChangesAsync();
        var ano = amo.AnoAmostra;
        var usado = amo.CodAmostra;
        c.Db.ChangeTracker.Clear();
        AjusteNumeracaoVM Ajuste(int valor) => new() { Tipo = "AMOSTRA", Ano = ano, NovoValor = valor, Motivo = "Ajuste de teste da numeração" };

        // usuário comum (mesmo com ALTERAR) não ajusta
        Assert.False((await Servico(c.Db, c.Ativa).AjustarNumeracaoAsync(Ajuste(usado + 10))).Sucesso);

        var global = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, UsuarioId = c.Ativa.UsuarioId, AdminGlobal = true };
        await using var db = await BancoTeste.NovoContextoAsync(global);
        var s = Servico(db, global);
        var menor = await s.AjustarNumeracaoAsync(Ajuste(usado - 1));
        Assert.Equal($"O último número não pode ser menor que o maior número já usado ({usado}).", menor.Mensagem);
        Assert.Equal("Descreva o motivo (de 10 a 500 caracteres).", (await s.AjustarNumeracaoAsync(new() { Tipo = "AMOSTRA", Ano = ano, NovoValor = usado + 10, Motivo = "curto" })).Mensagem);
        Assert.Equal("Tipo de documento inválido.", (await s.AjustarNumeracaoAsync(new() { Tipo = "BOLETIM", Ano = ano, NovoValor = 1, Motivo = "Ajuste de teste da numeração" })).Mensagem);

        var ok = await s.AjustarNumeracaoAsync(Ajuste(usado + 10));
        Assert.True(ok.Sucesso, ok.Mensagem);
        var linha = (await s.CarregarAsync())!.Numeracao.Single(n => n.Tipo == "AMOSTRA" && n.Ano == ano);
        Assert.Equal(usado + 10, linha.UltimoNumero);
        var aud = await db.Auditoria.SingleAsync(x => x.Tabela == "SEQUENCIAS_DOCUMENTOS" && x.IdRegistro == $"{c.Empresa.IdEmpresas}-AMOSTRA-{ano}");
        Assert.Equal("A", aud.Acao);
        Assert.Equal(usado, System.Text.Json.JsonDocument.Parse(aud.DadosAntes!).RootElement.GetProperty("ULTIMO_NUMERO").GetInt32());
        Assert.Equal("Ajuste de teste da numeração", System.Text.Json.JsonDocument.Parse(aud.DadosDepois!).RootElement.GetProperty("MOTIVO").GetString());

        // a próxima amostra continua do valor ajustado
        var proxima = c.NovaAmostra(hoje);
        c.Db.Add(proxima);
        await c.Db.SaveChangesAsync();
        Assert.Equal(usado + 11, proxima.CodAmostra);
    }

    [Fact]
    public async Task Tela_respeita_permissoes_e_ajuste_so_para_admin_global()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "PARAMETROS_EMPRESA.CONSULTAR");
        var editor = await UsuariosTeste.ComPermissoesAsync(c, "PARAMETROS_EMPRESA.CONSULTAR", "PARAMETROS_EMPRESA.ALTERAR");
        var p = await c.Db.EmpresasParametros.AsNoTracking().SingleAsync();
        (string, string)[] Campos(int dias) =>
        [
            ("IdLabPropostasStatusInicial", p.IdLabPropostasStatusInicial.ToString()!), ("IdLabPropostasStatusEnvcli", p.IdLabPropostasStatusEnvcli.ToString()!),
            ("IdAmostrasStatusInicial", p.IdAmostrasStatusInicial.ToString()!), ("IdBoletinsStatusInicial", p.IdBoletinsStatusInicial.ToString()!),
            ("IdBoletinsStatusEmitido", p.IdBoletinsStatusEmitido.ToString()!), ("DiasRetroativosEntrega", dias.ToString()),
            ("DiasValidadeProposta", "30"), ("DiasPesquisaPadrao", "365"),
        ];

        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            var html = await (await web.GetAsync("/Empresa/Parametros")).HtmlAsync();
            Assert.Contains("Numeração", html);
            Assert.DoesNotContain("id=\"modalAjuste\"", html); // só ADMIN_GLOBAL
            Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync("/Empresa/Parametros", "/Empresa/Parametros", Campos(10))).StatusCode);
        }
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(editor.Login);
            Assert.Equal("/Empresa/Parametros", (await web.PostarAsync("/Empresa/Parametros", "/Empresa/Parametros", Campos(12))).Destino());
            Assert.Equal(12, (await c.Db.EmpresasParametros.AsNoTracking().SingleAsync()).DiasRetroativosEntrega);
            var ajuste = await web.PostarAsync("/Empresa/Parametros", "/Empresa/AjustarNumeracao",
                ("Ajuste.Tipo", "PRODUTO"), ("Ajuste.Ano", "0"), ("Ajuste.NovoValor", "999"), ("Ajuste.Motivo", "Tentativa sem ser administrador global"));
            Assert.Equal(HttpStatusCode.Forbidden, ajuste.StatusCode);
        }

        var global = await UsuariosTeste.CriarAsync(adminGlobal: true);
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(global.Login);
            await web.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", c.Empresa.IdEmpresas.ToString()));
            Assert.Contains("id=\"modalAjuste\"", await (await web.GetAsync("/Empresa/Parametros")).HtmlAsync());
            var ajuste = await web.PostarAsync("/Empresa/Parametros", "/Empresa/AjustarNumeracao",
                ("Ajuste.Tipo", "PEDIDO_VENDA"), ("Ajuste.Ano", "0"), ("Ajuste.NovoValor", "999"), ("Ajuste.Motivo", "curto"));
            Assert.Equal("/Empresa/Parametros#numeracao", ajuste.Destino()); // erro volta pela mensagem do TempData
        }
    }
}
