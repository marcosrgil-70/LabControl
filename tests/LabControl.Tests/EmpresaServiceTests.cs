using System.Net;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Aceite da 1.5 (docs/telas/T03-empresa-usuaria.md, P23).</summary>
public class EmpresaServiceTests(AppWeb app) : IClassFixture<AppWeb>
{
    private static EmpresaService Servico(LabDbContext db, IEmpresaContexto ctx) =>
        new(db, ctx, new Procedures(db, ctx), new CopiaCadastros(db), new ServicoAuditoria(db, ctx));

    private static async Task<(LabDbContext Db, EmpresaFixa Ctx)> AdminGlobalAsync(Cenario c)
    {
        var ctx = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, UsuarioId = c.Ativa.UsuarioId, AdminGlobal = true };
        return (await BancoTeste.NovoContextoAsync(ctx), ctx);
    }

    // banco de testes persistente: CNPJ (único) diferente a cada execução
    internal static string CnpjAleatorio()
    {
        while (true)
        {
            var baseCnpj = string.Concat(Enumerable.Range(0, 12).Select(_ => Random.Shared.Next(10)));
            foreach (var dv in Enumerable.Range(0, 100).Select(n => n.ToString("00")))
                if (Documentos.CnpjValido(baseCnpj + dv)) return baseCnpj + dv;
        }
    }

    // código livre (3 caracteres, único) no banco de testes persistente
    private static async Task<string> CodigoAsync(LabDbContext db)
    {
        const string letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        while (true)
        {
            var c = string.Concat(Enumerable.Range(0, 3).Select(_ => letras[Random.Shared.Next(letras.Length)]));
            if (!await db.Empresas.AnyAsync(e => e.CodEmpresa == c)) return c;
        }
    }

    [Fact]
    public async Task Nova_empresa_nasce_inicializada_e_aparece_para_o_criador()
    {
        await using var c = await Cenario.CriarAsync();
        var (db, ctx) = await AdminGlobalAsync(c);
        await using var _ = db;
        var cnpj = CnpjAleatorio();
        var r = await Servico(db, ctx).CriarAsync(new EmpresaVM { CodEmpresa = await CodigoAsync(db), RazaoSocial = "LABORATÓRIO NOVO", Cnpj = cnpj });
        Assert.True(r.Sucesso, r.Mensagem);
        var nova = r.Valor;

        Assert.Equal(4, await db.AmostrasStatus.IgnoreQueryFilters().CountAsync(s => s.IdEmpresas == nova));
        Assert.Equal(5, await db.Perfis.IgnoreQueryFilters().CountAsync(p => p.IdEmpresas == nova));
        Assert.NotNull((await db.EmpresasParametros.IgnoreQueryFilters().SingleAsync(p => p.IdEmpresas == nova)).IdAmostrasStatusInicial);
        Assert.Equal(Documentos.FormatarCnpj(cnpj), (await db.Empresas.SingleAsync(e => e.IdEmpresas == nova)).Cnpj);

        // criador com acesso e perfil ADMINISTRADOR: aparece na escolha de empresa e recebe as permissões
        var conta = UsuariosTeste.Servico(db, ctx);
        Assert.Contains((await conta.EmpresasDoUsuarioAsync(ctx.UsuarioId!.Value)).Empresas, e => e.IdEmpresas == nova);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE USUARIOS SET ADMIN_GLOBAL = 0 WHERE ID_USUARIOS = {ctx.UsuarioId}");
        var p = await conta.MontarIdentidadeAsync(ctx.UsuarioId.Value, nova);
        Assert.Equal(await db.Permissoes.CountAsync(), p.Valor.FindAll(LabClaims.Permissao).Count());
        Assert.True(await db.Auditoria.IgnoreQueryFilters().AnyAsync(a => a.Tabela == "EMPRESAS" && a.IdRegistro == nova.ToString() && a.Acao == "I"));
    }

    [Fact]
    public async Task Copia_remapeia_para_a_empresa_nova_e_nunca_copia_clientes()
    {
        await using var c = await Cenario.CriarAsync(); // origem: tipos, métodos, parâmetros, produto, prazo, condição…
        c.Db.Add(new CondicaoPagtoParc { IdCondicoesPagtos = await c.Db.CondicoesPagtos.Select(x => x.IdCondicoesPagtos).SingleAsync(), NrParcela = 1, DiasVencimento = 30, PorcVencimento = 100 });
        await c.Db.SaveChangesAsync();
        var (db, ctx) = await AdminGlobalAsync(c);
        await using var _ = db;

        var r = await Servico(db, ctx).CriarAsync(new EmpresaVM
        {
            CodEmpresa = await CodigoAsync(db), RazaoSocial = "CÓPIA", CopiarDe = c.Empresa.IdEmpresas,
            Grupos = ["AUXILIARES", "PARAMETROS", "PRODUTOS", "CONDICOES", "PRAZOS", "TIPOS_RESULTADO"],
        });
        Assert.True(r.Sucesso, r.Mensagem);
        var nova = r.Valor;
        IQueryable<T> Da<T>(IQueryable<T> q) where T : class => q.IgnoreQueryFilters();

        // parâmetros apontam para tipo/método DA NOVA empresa (nunca para os IDs da origem)
        var parametros = await Da(db.LabParametrosAnalises).Where(x => x.IdEmpresas == nova)
            .Select(x => new { x.Descricao, Tipo = x.AnaliseTipo.IdEmpresas, Metodo = (int?)x.AnaliseMetodo!.IdEmpresas }).ToListAsync();
        Assert.Equal(2, parametros.Count);
        Assert.All(parametros, x => Assert.Equal(nova, x.Tipo));
        var produto = await Da(db.Produtos).Where(x => x.IdEmpresas == nova).Select(x => new { x.Codigo, Unidade = x.Unidade.IdEmpresas }).SingleAsync();
        Assert.Equal((c.Produto.Codigo, nova), (produto.Codigo, produto.Unidade));
        Assert.Equal(1, await Da(db.CondicoesPagtosParc).CountAsync(x => x.CondicaoPagto.IdEmpresas == nova));
        Assert.Equal(3, await Da(db.Idiomas).CountAsync(x => x.IdEmpresas == nova)); // os do SP_INICIALIZAR_EMPRESA não duplicam
        Assert.Equal(0, await Da(db.Entidades).CountAsync(x => x.IdEmpresas == nova && !x.TipoEmpresaUsuaria)); // clientes/profissionais nunca
        Assert.Equal(1, await Da(db.Entidades).CountAsync(x => x.IdEmpresas == nova && x.TipoEmpresaUsuaria)); // só a entidade da própria empresa (P25)

        // a sequência do produto acompanha o código copiado: o próximo produto da empresa nova não repete
        var naNova = new EmpresaFixa { EmpresaId = nova, UsuarioId = ctx.UsuarioId };
        await using var dbNova = await BancoTeste.NovoContextoAsync(naNova);
        var unidade = await dbNova.Unidades.Select(u => u.IdUnidades).FirstAsync();
        var outro = new Produto { Descricao = "NOVO NA EMPRESA NOVA", IdUnidades = unidade };
        dbNova.Add(outro);
        await dbNova.SaveChangesAsync();
        Assert.Equal("000002", outro.Codigo);

        // resumo da cópia na auditoria
        var ev = await db.Auditoria.IgnoreQueryFilters().Where(a => a.Tabela == "EMPRESAS" && a.IdRegistro == nova.ToString() && a.DadosDepois!.Contains("COPIA_CADASTROS")).SingleAsync();
        Assert.Contains("LAB_PARAMETROS_ANALISES", ev.DadosDepois);
    }

    [Fact]
    public async Task Modelo_da_copia_confere_com_o_banco()
    {
        await using var c = await Cenario.CriarAsync();
        var copia = new CopiaCadastros(c.Db);
        // chaves únicas declaradas = UNIQUE do banco (exceto ANALISES_METODOS, que não tem)
        var unicos = await c.Db.Database.SqlQueryRaw<ChaveBanco>("""
            SELECT UPPER(TABLE_NAME) AS Tabela, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) AS Colunas
              FROM information_schema.STATISTICS
             WHERE TABLE_SCHEMA = DATABASE() AND NON_UNIQUE = 0 AND INDEX_NAME <> 'PRIMARY'
             GROUP BY TABLE_NAME, INDEX_NAME
            """).ToListAsync();
        foreach (var t in CopiaCadastros.Tabelas.Where(t => t.Nome != "ANALISES_METODOS"))
        {
            var doBanco = unicos.Where(u => u.Tabela == t.Nome).Select(u => string.Join(",", u.Colunas.Split(',').Where(x => x != "ID_EMPRESAS" && x != t.Pai?.Coluna))).Order();
            Assert.Equal(doBanco, t.Chaves.Select(k => string.Join(",", k)).Order());
        }
        // toda FK de tabela copiada para outra tabela por empresa está remapeada (senão copiaria ID da origem)
        var fks = await c.Db.Database.SqlQueryRaw<FkBanco>("""
            SELECT UPPER(TABLE_NAME) AS Tabela, COLUMN_NAME AS Coluna, UPPER(REFERENCED_TABLE_NAME) AS Referencia
              FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND REFERENCED_TABLE_NAME IS NOT NULL
            """).ToListAsync();
        var globais = new[] { "EMPRESAS", "PAISES", "ESTADOS", "CIDADES", "USUARIOS" };
        foreach (var t in CopiaCadastros.Tabelas)
            foreach (var fk in fks.Where(f => f.Tabela == t.Nome && !globais.Contains(f.Referencia)))
                Assert.True(t.Fks.Any(x => x.Coluna == fk.Coluna && x.Tabela == fk.Referencia) || t.Pai?.Coluna == fk.Coluna, $"{t.Nome}.{fk.Coluna} → {fk.Referencia}");
        // e o SQL de cada tabela é montado
        foreach (var t in CopiaCadastros.Tabelas) Assert.Contains("INSERT INTO", await copia.MontarSqlAsync(t));
    }

    private sealed record ChaveBanco(string Tabela, string Colunas);
    private sealed record FkBanco(string Tabela, string Coluna, string Referencia);

    [Fact]
    public async Task Usuario_comum_altera_so_a_empresa_ativa_e_nao_cria()
    {
        await using var c = await Cenario.CriarAsync();
        await using var outra = await Cenario.CriarAsync();
        var s = Servico(c.Db, c.Ativa);
        Assert.Equal("Somente o administrador global cria empresas.", (await s.CriarAsync(new EmpresaVM { CodEmpresa = "X", RazaoSocial = "X" })).Mensagem);
        Assert.Null(await s.CarregarAsync(outra.Empresa.IdEmpresas));
        Assert.Equal("Empresa não encontrada.", (await s.AlterarAsync(new EmpresaVM { Id = outra.Empresa.IdEmpresas, RazaoSocial = "INVADIDA" })).Mensagem);

        var vm = (await s.CarregarAsync(c.Empresa.IdEmpresas))!;
        vm.Cnpj = "11.111.111/1111-11";
        Assert.Equal(("CNPJ inválido.", "Cnpj"), ((await s.AlterarAsync(vm)).Mensagem, (await s.AlterarAsync(vm)).Campo));
        vm.Cnpj = null;
        vm.NomeFantasia = "LAB ALTERADO";
        vm.Ativa = false; // ignorado para quem não é ADMIN_GLOBAL
        vm.NovoLogo = Arquivo([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2]);
        Assert.True((await s.AlterarAsync(vm)).Sucesso);
        var e = await c.Db.Empresas.AsNoTracking().SingleAsync(x => x.IdEmpresas == c.Empresa.IdEmpresas);
        Assert.Equal(("LAB ALTERADO", true, "image/png"), (e.NomeFantasia, e.Ativa, e.LogoMime));
        Assert.Equal(c.Empresa.CodEmpresa, e.CodEmpresa); // código imutável

        vm.NovoLogo = Arquivo("GIF89a"u8.ToArray());
        Assert.Equal("O logo deve ser uma imagem PNG ou JPG.", (await s.AlterarAsync(vm)).Mensagem);
        vm.NovoLogo = Arquivo(new byte[EmpresaService.TamanhoMaximoLogo + 1]);
        Assert.Equal("O logo deve ter no máximo 1 MB.", (await s.AlterarAsync(vm)).Mensagem);

        // selo da acreditação (D-28): mesmas regras do logo, gravado e removido à parte
        vm.NovoLogo = null;
        vm.NovoSelo = Arquivo("GIF89a"u8.ToArray());
        Assert.Equal(("O selo da acreditação deve ser uma imagem PNG ou JPG.", "NovoSelo"), ((await s.AlterarAsync(vm)).Mensagem, (await s.AlterarAsync(vm)).Campo));
        vm.NovoSelo = Arquivo([0xFF, 0xD8, 0xFF, 0xE0, 1, 2]);
        Assert.True((await s.AlterarAsync(vm)).Sucesso);
        Assert.True((await s.CarregarAsync(c.Empresa.IdEmpresas))!.TemSelo);
        Assert.Equal("image/jpeg", (await s.SeloAsync(c.Empresa.IdEmpresas))?.Mime);
        (vm.NovoSelo, vm.RemoverSelo) = (null, true);
        Assert.True((await s.AlterarAsync(vm)).Sucesso);
        Assert.Null(await s.SeloAsync(c.Empresa.IdEmpresas));
        Assert.Equal("image/png", (await s.LogoAsync(c.Empresa.IdEmpresas))?.Mime); // o logo continua
    }

    private static IFormFile Arquivo(byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "NovoLogo", "logo.bin");

    [Fact]
    public async Task Telas_respeitam_permissao_e_admin_global()
    {
        await using var c = await Cenario.CriarAsync();
        var leitor = await UsuariosTeste.ComPermissoesAsync(c, "EMPRESAS.CONSULTAR");
        using (var web = app.Cliente())
        {
            await web.EntrarAsync(leitor.Login);
            var dados = await web.GetAsync("/Empresa/Dados");
            Assert.Equal(HttpStatusCode.OK, dados.StatusCode);
            Assert.Contains(c.Empresa.CodEmpresa, await dados.HtmlAsync());
            Assert.Equal(HttpStatusCode.Forbidden, (await web.PostarAsync("/Empresa/Dados", "/Empresa/Dados", ("RazaoSocial", "X"))).StatusCode);
            Assert.Equal("/Empresa/Dados", (await web.GetAsync("/Empresas")).Destino()); // um item só no menu: cai na empresa ativa
            Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync("/Empresas/Criar")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await web.GetAsync($"/Geografia/CidadesDaUf?idEstado={await c.Db.Estados.Select(x => x.IdEstados).FirstAsync()}")).StatusCode);
        }

        var admin = await UsuariosTeste.CriarAsync(adminGlobal: true);
        using var adm = app.Cliente();
        await adm.EntrarAsync(admin.Login);
        await adm.PostarAsync("/Conta/Empresa", "/Conta/Empresa", ("IdEmpresa", c.Empresa.IdEmpresas.ToString()));
        Assert.Equal(HttpStatusCode.OK, (await adm.GetAsync("/Empresas")).StatusCode);
        var codigo = await CodigoAsync(c.Db);
        var r = await adm.PostarAsync("/Empresas/Criar", "/Empresas/Criar", ("CodEmpresa", codigo), ("RazaoSocial", "PELA TELA"));
        Assert.Equal("/Empresas", r.Destino());
        Assert.Contains(codigo, await (await adm.GetAsync("/Empresas")).HtmlAsync());
    }
}
