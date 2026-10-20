using System.Security.Cryptography;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services;
using LabControl.Services.Entidades;
using LabControl.Services.Geografia;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabControl.Tests;

/// <summary>
/// Cadastros com vários endereços/telefones/e-mails tipados (pedido do usuário, 20/10/2026 — P25): empresa usuária
/// como entidade (T03), clientes/fornecedores (T12), profissionais (T13), contatos e busca de CEP.
/// </summary>
public class CadastrosEntidadesTests
{
    private static async Task<int> CidadeAsync(Cenario c, string ibge = "3550308") => // São Paulo (seed 06_cidades)
        await c.Db.Cidades.Where(x => x.CodigoIbge == ibge).Select(x => x.IdCidades).SingleAsync();

    private static async Task<int> EntidadeDaEmpresaAsync(Cenario c) =>
        (await c.Db.Empresas.Where(e => e.IdEmpresas == c.Empresa.IdEmpresas).Select(e => e.IdEntidades).SingleAsync())!.Value;

    private static async Task<EnderecoVM> EnderecoAsync(Cenario c, string tipo = "NOTA FISCAL") => new()
    {
        IdEnderecosTipos = await c.Db.EnderecosTipos.Where(t => t.Descricao == tipo).Select(t => t.IdEnderecosTipos).SingleAsync(),
        IdTiposLogradouro = await c.Db.TiposLogradouro.Where(t => t.Descricao == "AVENIDA").Select(t => t.IdTiposLogradouro).SingleAsync(),
        Logradouro = "paulista", Numero = "1000", Bairro = "bela vista", Cep = "01310100", IdCidades = await CidadeAsync(c),
    };

    private static async Task<int> TipoFoneAsync(Cenario c, string tipo = "COMERCIAL") =>
        await c.Db.FonesTipos.Where(t => t.Descricao == tipo).Select(t => t.IdFonesTipos).SingleAsync();

    // ------------------------------------------------------------- empresa usuária

    [Fact]
    public async Task Empresa_nasce_com_entidade_e_guarda_varios_contatos_tipados()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ContatosService(c.Db);
        var ent = await EntidadeDaEmpresaAsync(c);
        var entidade = await c.Db.Entidades.AsNoTracking().SingleAsync(e => e.IdEntidades == ent);
        Assert.True(entidade.TipoEmpresaUsuaria);
        Assert.Equal(c.Empresa.RazaoSocial, entidade.Nome);

        // dois endereços de tipos diferentes, dois telefones, dois e-mails
        Assert.True((await s.GravarEnderecoAsync(CadastroDono.Empresa, ent, await EnderecoAsync(c))).Sucesso);
        var r = await s.GravarEnderecoAsync(CadastroDono.Empresa, ent, await EnderecoAsync(c, "ENTREGA"));
        Assert.True(r.Sucesso, r.Mensagem);
        Assert.True((await s.GravarFoneAsync(CadastroDono.Empresa, ent, new FoneVM { IdFonesTipos = await TipoFoneAsync(c), Ddd = "11", Fone = "3333-4444", Principal = true })).Sucesso);
        Assert.True((await s.GravarFoneAsync(CadastroDono.Empresa, ent, new FoneVM { IdFonesTipos = await TipoFoneAsync(c, "WHATSAPP"), Ddd = "(11)", Fone = "99999 8888" })).Sucesso);
        Assert.True((await s.GravarEmailAsync(CadastroDono.Empresa, ent, new EmailVM { Email = " Lab@Empresa.com.br ", Principal = true, EnviarLaudo = true })).Sucesso);
        Assert.True((await s.GravarEmailAsync(CadastroDono.Empresa, ent, new EmailVM { Email = "financeiro@empresa.com.br", EnviarNfe = true })).Sucesso);

        var vm = (await s.CarregarAsync(CadastroDono.Empresa, ent, "/Empresa", true))!;
        Assert.Equal(2, vm.Enderecos.Count);
        var end = vm.Enderecos.Single(e => e.Tipo == "NOTA FISCAL");
        Assert.Equal("AV. PAULISTA, 1000", end.Linha1);
        Assert.Equal("BELA VISTA - SÃO PAULO/SP - 01310-100", end.Linha2);
        Assert.Equal(["(11) 3333-4444", "(11) 99999-8888"], vm.Fones.Select(f => $"({f.Ddd}) {f.Fone}"));
        Assert.Equal("lab@empresa.com.br", vm.Emails.Single(m => m.Principal).Email);

        // a entidade da empresa não é mexida pelas rotas de clientes nem de profissionais
        Assert.Equal(ContatosService.MensagemNaoEncontrado, (await s.GravarFoneAsync(CadastroDono.Cliente, ent, new FoneVM { IdFonesTipos = await TipoFoneAsync(c), Ddd = "11", Fone = "33334444" })).Mensagem);
        Assert.Null(await s.CarregarAsync(CadastroDono.Profissional, ent, "/x", true));
    }

    [Fact]
    public async Task Alterar_a_empresa_espelha_razao_social_e_cnpj_na_entidade()
    {
        await using var c = await Cenario.CriarAsync();
        var global = new EmpresaFixa { EmpresaId = c.Empresa.IdEmpresas, UsuarioId = c.Ativa.UsuarioId, AdminGlobal = true };
        await using var db = await BancoTeste.NovoContextoAsync(global);
        var s = new EmpresaService(db, global, new Procedures(db, global), new CopiaCadastros(db), new ServicoAuditoria(db, global));
        var vm = (await s.CarregarAsync(c.Empresa.IdEmpresas))!;
        vm.RazaoSocial = "NOVA RAZÃO " + Guid.NewGuid().ToString("N")[..6];
        Assert.True((await s.AlterarAsync(vm)).Sucesso);
        Assert.Equal(vm.RazaoSocial, await db.Entidades.Where(e => e.IdEntidades == vm.IdEntidades).Select(e => e.Nome).SingleAsync());
    }

    // ------------------------------------------------------------- regras dos contatos

    [Fact]
    public async Task Endereco_valida_campos_e_um_ativo_por_tipo()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ContatosService(c.Db);
        var cli = c.Cliente.IdEntidades;

        var semCidade = await EnderecoAsync(c);
        semCidade.IdCidades = null;
        Assert.Equal(("Informe a cidade.", "IdCidades"), Erro(await s.GravarEnderecoAsync(CadastroDono.Cliente, cli, semCidade)));
        var cepRuim = await EnderecoAsync(c);
        cepRuim.Cep = "1234";
        Assert.Equal(("CEP inválido (8 dígitos).", "Cep"), Erro(await s.GravarEnderecoAsync(CadastroDono.Cliente, cli, cepRuim)));
        var longo = await EnderecoAsync(c);
        longo.Bairro = new string('X', 81);
        Assert.Equal(("Bairro: no máximo 80 caracteres.", "Bairro"), Erro(await s.GravarEnderecoAsync(CadastroDono.Cliente, cli, longo)));

        // o cenário já tem um endereço ativo do 1º tipo (NOTA FISCAL) para o cliente
        var tipoDoCenario = await c.Db.EntidadesEnderecos.Where(e => e.IdEntidades == cli).Select(e => e.EnderecoTipo.Descricao).FirstAsync();
        var segundo = await EnderecoAsync(c, tipoDoCenario);
        Assert.Equal("Já existe um endereço ativo deste tipo para este cadastro. Inative o anterior ou altere-o.",
            (await s.GravarEnderecoAsync(CadastroDono.Cliente, cli, segundo)).Mensagem);
        segundo.Inativo = true; // endereço antigo guardado como histórico
        Assert.True((await s.GravarEnderecoAsync(CadastroDono.Cliente, cli, segundo)).Sucesso);
    }

    [Fact]
    public async Task Principal_troca_sozinho_e_inativo_nunca_e_principal()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ContatosService(c.Db);
        var cli = c.Cliente.IdEntidades;
        var tipo = await TipoFoneAsync(c);

        Assert.Equal(("DDD com 2 ou 3 dígitos.", "Ddd"), Erro(await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { IdFonesTipos = tipo, Ddd = "1", Fone = "33334444" })));
        Assert.Equal(("Telefone com 8 ou 9 dígitos (sem o DDD).", "Fone"), Erro(await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { IdFonesTipos = tipo, Ddd = "11", Fone = "333" })));

        var f1 = (await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { IdFonesTipos = tipo, Ddd = "19", Fone = "33334444", Principal = true })).Valor;
        var f2 = (await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { IdFonesTipos = tipo, Ddd = "19", Fone = "999998888", Principal = true })).Valor;
        c.Db.ChangeTracker.Clear();
        var fones = await c.Db.EntidadesFones.AsNoTracking().Where(f => f.IdEntidades == cli).ToDictionaryAsync(f => f.IdEntidadesFones);
        Assert.False(fones[f1].Principal);
        Assert.True(fones[f2].Principal);

        Assert.True((await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { Id = f2, IdFonesTipos = tipo, Ddd = "19", Fone = "999998888", Principal = true, Inativo = true })).Sucesso);
        Assert.False(await c.Db.EntidadesFones.AnyAsync(f => f.IdEntidades == cli && f.Principal));

        // e-mails: formato, duplicado na entidade, principal
        Assert.Equal(("E-mail inválido.", "Email"), Erro(await s.GravarEmailAsync(CadastroDono.Cliente, cli, new EmailVM { Email = "sem-arroba" })));
        Assert.Equal("Este e-mail já está cadastrado para esta entidade.",
            (await s.GravarEmailAsync(CadastroDono.Cliente, cli, new EmailVM { Email = "CLIENTE@teste.com" })).Mensagem);
        var m1 = (await s.GravarEmailAsync(CadastroDono.Cliente, cli, new EmailVM { Email = "a@teste.com", Principal = true })).Valor;
        var m2 = (await s.GravarEmailAsync(CadastroDono.Cliente, cli, new EmailVM { Email = "b@teste.com", Principal = true })).Valor;
        c.Db.ChangeTracker.Clear();
        Assert.Equal(m2, await c.Db.EntidadesEmails.Where(m => m.IdEntidades == cli && m.Principal).Select(m => m.IdEntidadesEmails).SingleAsync());
        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public async Task Contato_em_uso_na_proposta_nao_exclui_e_outra_empresa_nao_mexe()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var s = new ContatosService(a.Db);
        var cli = a.Cliente.IdEntidades;
        var email = a.Proposta.IdEntContatoResultado;

        Assert.Equal("Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).",
            (await s.ExcluirAsync(CadastroDono.Cliente, cli, TipoContato.Email, email)).Mensagem);
        var fone = (await s.GravarFoneAsync(CadastroDono.Cliente, cli, new FoneVM { IdFonesTipos = await TipoFoneAsync(a), Ddd = "11", Fone = "33334444" })).Valor;
        Assert.True((await s.ExcluirAsync(CadastroDono.Cliente, cli, TipoContato.Fone, fone)).Sucesso);

        var deB = new ContatosService(b.Db);
        Assert.Null(await deB.CarregarAsync(CadastroDono.Cliente, cli, "/x", true));
        Assert.Equal(ContatosService.MensagemNaoEncontrado, (await deB.ExcluirAsync(CadastroDono.Cliente, cli, TipoContato.Email, email)).Mensagem);
    }

    // ------------------------------------------------------------- T12 clientes / fornecedores

    [Fact]
    public async Task Cliente_pf_pj_documento_tipos_e_lista()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ClienteService(c.Db);

        var pj = new ClienteVM { Nome = "fornecedor teste ltda", Categoria = Categoria.Juridica, CpfCnpj = "11222333000180", TipoCliente = false };
        Assert.Equal("Marque ao menos um tipo de cadastro (cliente, fornecedor, comercial ou transportador).", (await s.GravarAsync(pj)).Mensagem);
        pj.TipoFornecedor = true;
        Assert.Equal(("CNPJ inválido.", "CpfCnpj"), Erro(await s.GravarAsync(pj)));
        pj.CpfCnpj = "11222333000181";
        pj.Sexo = Sexo.Feminino; // campo de PF: descartado em PJ
        var id = (await s.GravarAsync(pj)).Valor;
        var gravado = await c.Db.Entidades.AsNoTracking().SingleAsync(e => e.IdEntidades == id);
        Assert.Equal(("FORNECEDOR TESTE LTDA", "11.222.333/0001-81", (Sexo?)null), (gravado.Nome, gravado.CpfCnpj, gravado.Sexo));

        var dup = new ClienteVM { Nome = "OUTRO", CpfCnpj = "11.222.333/0001-81", TipoCliente = true };
        Assert.Equal(("Já existe um cadastro com este CPF/CNPJ.", "CpfCnpj"), Erro(await s.GravarAsync(dup)));
        Assert.Equal(("CPF inválido.", "CpfCnpj"), Erro(await s.GravarAsync(new ClienteVM { Nome = "PF", Categoria = Categoria.Fisica, CpfCnpj = "12345678900" })));
        var pf = (await s.GravarAsync(new ClienteVM { Nome = "maria", NomeFantasia = "silva", Categoria = Categoria.Fisica, CpfCnpj = "52998224725" })).Valor;
        Assert.Equal("529.982.247-25", await c.Db.Entidades.Where(e => e.IdEntidades == pf).Select(e => e.CpfCnpj).SingleAsync());

        // lista: por parte do CNPJ sem máscara, por tipo; a empresa usuária nunca aparece
        Assert.Equal([id], (await s.ListarAsync(new ClienteFiltroVM { Texto = "222333" })).Itens.Select(i => i.Id));
        var fornecedores = await s.ListarAsync(new ClienteFiltroVM { Tipo = "fornecedor" });
        Assert.Equal([id], fornecedores.Itens.Select(i => i.Id));
        var todos = await s.ListarAsync(new ClienteFiltroVM());
        Assert.DoesNotContain(todos.Itens, i => i.Id == (c.Db.Empresas.Where(e => e.IdEmpresas == c.Empresa.IdEmpresas).Select(e => e.IdEntidades).Single()));
        Assert.Contains(todos.Itens, i => i.Id == c.Cliente.IdEntidades && i.Email == "cliente@teste.com");
    }

    [Fact]
    public async Task Cliente_com_proposta_nao_exclui_inativa_e_sem_movimento_exclui_com_os_contatos()
    {
        await using var c = await Cenario.CriarAsync();
        var s = new ClienteService(c.Db);
        Assert.Equal("Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).",
            (await s.ExcluirAsync(c.Cliente.IdEntidades)).Mensagem);
        Assert.True((await s.DefinirInativoAsync(c.Cliente.IdEntidades, true)).Sucesso);
        Assert.DoesNotContain((await s.ListarAsync(new ClienteFiltroVM())).Itens, i => i.Id == c.Cliente.IdEntidades);

        var novo = (await s.GravarAsync(new ClienteVM { Nome = "SEM MOVIMENTO", TipoCliente = true })).Valor;
        await new ContatosService(c.Db).GravarEmailAsync(CadastroDono.Cliente, novo, new EmailVM { Email = "x@sem.com" });
        Assert.True((await s.ExcluirAsync(novo)).Sucesso);
        Assert.False(await c.Db.EntidadesEmails.AnyAsync(m => m.IdEntidades == novo)); // contatos vão junto (CASCADE)
    }

    // ------------------------------------------------------------- T13 profissionais

    private static ProfissionalService Profissionais(Cenario c) =>
        new(c.Db, c.Ativa, new VinculoUsuarioProfissionalService(c.Db, c.Ativa));

    private static IFormFile Arquivo(byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "NovaAssinatura", "a.png");

    [Fact]
    public async Task Profissional_registro_assinatura_e_vinculo_com_usuario()
    {
        await using var c = await Cenario.CriarAsync();
        var s = Profissionais(c);
        var crq = await c.Db.TiposRegProfissional.Where(t => t.Descricao == "CRQ").Select(t => t.IdTiposRegProfissional).SingleAsync();

        var vm = new ProfissionalVM { Nome = "joão", Sobrenome = "analista", Cpf = "52998224725", AssinaBoletim = true };
        Assert.Equal("Quem assina boletim precisa de tipo, nº e UF do registro profissional.", (await s.GravarAsync(vm)).Mensagem);
        (vm.IdTiposRegProfissional, vm.NrRegistroProfissional, vm.UfRegistro) = (crq, "04123456", "sp");

        Assert.Equal("A assinatura deve ser uma imagem PNG.", (await s.GravarAsync(Com(vm, Arquivo([0xFF, 0xD8, 0xFF, 0x00])))).Mensagem);
        Assert.Equal("A assinatura deve ter no máximo 300 KB.", (await s.GravarAsync(Com(vm, Arquivo(new byte[300 * 1024 + 1])))).Mensagem);
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
        var usu = await UsuariosTeste.CriarAsync();
        await UsuariosTeste.VincularAsync(usu.IdUsuarios, c.Empresa.IdEmpresas);
        vm.IdUsuario = usu.IdUsuarios;
        var r = await s.GravarAsync(Com(vm, Arquivo(png)));
        Assert.True(r.Sucesso, r.Mensagem);

        var f = await c.Db.EntidadesFuncionarios.AsNoTracking().Include(x => x.Entidade).SingleAsync(x => x.IdEntidades == r.Valor);
        Assert.Equal(("JOÃO", Categoria.Fisica, true, "SP", "529.982.247-25"), (f.Entidade.Nome, f.Entidade.Categoria, f.Entidade.TipoFuncionario, f.UfRegistro, f.Entidade.CpfCnpj));
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(png)), f.AssinaturaHash);
        Assert.Equal(r.Valor, await c.Db.UsuariosEmpresas.Where(v => v.IdUsuarios == usu.IdUsuarios).Select(v => v.IdEntidadesFunc).SingleAsync());
        Assert.Contains((await s.ListarAsync(new ProfissionalFiltroVM { Texto = "joão" })).Itens, p => p.Id == r.Valor && p.TemAssinatura && p.Login == usu.Login);

        // o usuário já vinculado não aparece livre para outro profissional
        var outro = await s.NovoAsync();
        Assert.DoesNotContain(outro.Usuarios, u => u.Id == usu.IdUsuarios);
        Assert.Equal("Profissional vinculado a um usuário: retire o vínculo (ou inative o profissional) antes de excluir.",
            (await s.ExcluirAsync(r.Valor)).Mensagem);

        // inativo fica sem usuário
        var edit = (await s.CarregarAsync(r.Valor))!;
        edit.Inativo = true;
        Assert.True((await s.GravarAsync(edit)).Sucesso);
        Assert.Null(await c.Db.UsuariosEmpresas.Where(v => v.IdUsuarios == usu.IdUsuarios).Select(v => v.IdEntidadesFunc).SingleAsync());
        Assert.True((await s.ExcluirAsync(r.Valor)).Sucesso);
    }

    private static ProfissionalVM Com(ProfissionalVM vm, IFormFile arquivo)
    {
        vm.NovaAssinatura = arquivo;
        return vm;
    }

    // ------------------------------------------------------------- CEP

    private sealed class CepFixo(CepBruto? resposta, bool falha = false) : IFonteCep
    {
        public int Chamadas { get; private set; }
        public Task<CepBruto?> ConsultarAsync(string cep, CancellationToken ct)
        {
            Chamadas++;
            return falha ? throw new HttpRequestException("fora do ar") : Task.FromResult(resposta);
        }
    }

    [Fact]
    public async Task Cep_liga_a_cidade_pelo_ibge_separa_o_tipo_de_logradouro_e_usa_cache()
    {
        await using var c = await Cenario.CriarAsync();
        var fonte = new CepFixo(new CepBruto("Avenida Paulista", "de 612 a 1510 - lado par", "Bela Vista", "3550308", "SP"));
        var s = new ConsultaCepService(fonte, new MemoryCache(new MemoryCacheOptions()), c.Db, NullLogger<ConsultaCepService>.Instance);

        var r = await s.ConsultarAsync("01310-100");
        Assert.True(r.Sucesso, r.Mensagem);
        var avenida = await c.Db.TiposLogradouro.Where(t => t.Descricao == "AVENIDA").Select(t => t.IdTiposLogradouro).SingleAsync();
        Assert.Equal(("01310-100", "PAULISTA", (int?)avenida, "BELA VISTA", "SÃO PAULO", "SP"),
            (r.Valor.Cep, r.Valor.Logradouro, r.Valor.IdTiposLogradouro, r.Valor.Bairro, r.Valor.Cidade, r.Valor.Uf));
        await s.ConsultarAsync("01310100");
        Assert.Equal(1, fonte.Chamadas);

        Assert.Equal("CEP inválido (8 dígitos).", (await s.ConsultarAsync("123")).Mensagem);
        var inexistente = new ConsultaCepService(new CepFixo(null), new MemoryCache(new MemoryCacheOptions()), c.Db, NullLogger<ConsultaCepService>.Instance);
        Assert.Equal("CEP não encontrado.", (await inexistente.ConsultarAsync("99999999")).Mensagem);
        var foraDoAr = new ConsultaCepService(new CepFixo(null, falha: true), new MemoryCache(new MemoryCacheOptions()), c.Db, NullLogger<ConsultaCepService>.Instance);
        Assert.Equal("Serviço de CEP indisponível no momento. Preencha o endereço manualmente.", (await foraDoAr.ConsultarAsync("01310100")).Mensagem);
    }

    private static (string?, string?) Erro(IResultado r) => (r.Mensagem, r.Campo);
}
