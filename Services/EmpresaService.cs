using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T03 — Empresa usuária e nova empresa (docs/telas/T03-empresa-usuaria.md, P23). <c>EMPRESAS</c> é global:
/// usuário comum só lê/altera a empresa ativa; lista, inclusão, outras empresas e "Ativa" só ADMIN_GLOBAL.
/// Não existe exclusão (só inativar). EMPRESAS é auditada (o logo vai como "[binário]").
/// </summary>
public sealed class EmpresaService(LabDbContext db, IEmpresaContexto ctx, Procedures procedures, CopiaCadastros copia, IAuditoria auditoria)
{
    public const int TamanhoMaximoLogo = 1024 * 1024;
    public const string Administrador = "ADMINISTRADOR";

    public async Task<IReadOnlyList<EmpresaListaItemVM>> ListarAsync() =>
        await db.Empresas.AsNoTracking().OrderBy(e => e.CodEmpresa)
            .Select(e => new EmpresaListaItemVM(e.IdEmpresas, e.CodEmpresa, e.NomeFantasia ?? e.RazaoSocial, e.Cnpj,
                // cidade do endereço ativo de nota fiscal (senão o 1º ativo) da entidade da empresa
                // (lista só do ADMIN_GLOBAL: lê as outras empresas)
                db.EntidadesEnderecos.IgnoreQueryFilters().Where(a => a.IdEntidades == e.IdEntidades && !a.Inativo)
                    .OrderBy(a => a.EnderecoTipo.Finalidade == FinalidadeEndereco.NotaFiscal ? 0 : 1).ThenBy(a => a.IdEntidadesEnderecos).Select(a => a.Cidade.Nome + "/" + a.Cidade.Estado.Sigla).FirstOrDefault(),
                e.Ativa, db.UsuariosEmpresas.Count(v => v.IdEmpresas == e.IdEmpresas && v.Ativo)))
            .ToListAsync();

    public async Task<EmpresaVM> NovaAsync()
    {
        var vm = new EmpresaVM();
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Empresa para o formulário: a ativa, ou qualquer uma para ADMIN_GLOBAL.</summary>
    public async Task<EmpresaVM?> CarregarAsync(int id)
    {
        if (!PodeVer(id)) return null;
        var e = await db.Empresas.AsNoTracking().SingleOrDefaultAsync(x => x.IdEmpresas == id);
        if (e is null) return null;
        var vm = new EmpresaVM
        {
            Id = e.IdEmpresas, CodEmpresa = e.CodEmpresa, RazaoSocial = e.RazaoSocial, NomeFantasia = e.NomeFantasia, Cnpj = e.Cnpj,
            InscEstadual = e.InscEstadual, InscMunicipal = e.InscMunicipal, IdEntidades = e.IdEntidades, Site = e.Site, CabecalhoLaudo = e.CabecalhoLaudo, RodapeLaudo = e.RodapeLaudo, NrAcreditacao = e.NrAcreditacao, Ativa = e.Ativa,
            TemLogo = e.Logo is { Length: > 0 }, TemSelo = e.SeloAcreditacao is { Length: > 0 },
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Combos (empresas de origem, grupos de cópia), logo e entidade; usado também ao reexibir o POST com erro.</summary>
    public async Task PrepararAsync(EmpresaVM vm)
    {
        vm.AdminGlobal = ctx.AdminGlobal;
        if (vm.Id != 0)
        {
            var atual = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == vm.Id)
                .Select(e => new { TemLogo = e.Logo != null, TemSelo = e.SeloAcreditacao != null, e.IdEntidades }).SingleOrDefaultAsync();
            (vm.TemLogo, vm.TemSelo, vm.IdEntidades) = (atual?.TemLogo ?? false, atual?.TemSelo ?? false, atual?.IdEntidades);
        }
        if (vm.Id == 0 && ctx.AdminGlobal)
        {
            vm.EmpresasOrigem = await db.Empresas.AsNoTracking().Where(e => e.Ativa).OrderBy(e => e.CodEmpresa)
                .Select(e => new OpcaoVM(e.IdEmpresas, e.CodEmpresa + " — " + (e.NomeFantasia ?? e.RazaoSocial))).ToListAsync();
            vm.GruposDisponiveis = CopiaCadastros.Grupos;
        }
    }

    /// <summary>
    /// Nova empresa (ADMIN_GLOBAL), numa transação só: EMPRESAS → SP_INICIALIZAR_EMPRESA (status, parâmetros, perfis) →
    /// cópia de cadastros (opcional) → vínculo do criador com o perfil ADMINISTRADOR → evento de auditoria com o resumo.
    /// </summary>
    public async Task<Resultado<int>> CriarAsync(EmpresaVM vm)
    {
        if (!ctx.AdminGlobal) return Resultado.Erro("Somente o administrador global cria empresas.");
        if (vm.CopiarDe is not null && vm.Grupos.Count == 0) return Resultado.Erro("Marque o que deseja copiar.", nameof(vm.Grupos));
        if (vm.CopiarDe is { } de && !await db.Empresas.AnyAsync(e => e.IdEmpresas == de)) return Resultado.Erro("Empresa de origem não encontrada.", nameof(vm.CopiarDe));

        var empresa = new Empresa { CodEmpresa = vm.CodEmpresa.Trim().ToUpperInvariant() };
        if (await AplicarAsync(empresa, vm) is { Sucesso: false } invalido) return invalido.Falha;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Add(empresa);
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!gravou.Sucesso) return Resultado.Erro(gravou.Mensagem!, CampoDoErro(gravou.Mensagem));

        var ini = await procedures.InicializarEmpresaAsync(empresa.IdEmpresas);
        if (!ini.Sucesso) return Resultado.Erro(ini.Mensagem!);

        IReadOnlyDictionary<string, int>? resumo = null;
        if (vm.CopiarDe is { } origem)
            resumo = await copia.CopiarAsync(origem, empresa.IdEmpresas, vm.Grupos, ctx.UsuarioId);

        // o criador passa a ter acesso à empresa nova, com o perfil ADMINISTRADOR (aparece na escolha de empresa dele)
        if (ctx.UsuarioId is { } criador)
        {
            // ADMIN_GLOBAL: a empresa nova ainda não é a ativa — lê o perfil sem o filtro global, com a empresa no Where
            var admin = await db.Perfis.IgnoreQueryFilters()
                .Where(p => p.IdEmpresas == empresa.IdEmpresas && p.Nome == Administrador).Select(p => p.IdPerfis).SingleAsync();
            db.Add(new UsuarioEmpresa { IdUsuarios = criador, IdEmpresas = empresa.IdEmpresas, Ativo = true });
            db.Add(new UsuarioPerfil { IdUsuarios = criador, IdEmpresas = empresa.IdEmpresas, IdPerfis = admin });
            await db.SaveChangesAsync();
        }

        if (resumo is not null)
            await auditoria.RegistrarAsync(AcoesAuditoria.Inclusao, "EMPRESAS", empresa.IdEmpresas.ToString(), null,
                new { COPIA_CADASTROS = new { ID_EMPRESA_ORIGEM = vm.CopiarDe, GRUPOS = vm.Grupos, LINHAS = resumo } });
        await tx.CommitAsync();
        return Resultado.Ok(empresa.IdEmpresas);
    }

    /// <summary>Alteração: a empresa ativa (EMPRESAS.ALTERAR) ou, para ADMIN_GLOBAL, qualquer uma. O código não muda.</summary>
    public async Task<Resultado> AlterarAsync(EmpresaVM vm)
    {
        if (!PodeVer(vm.Id)) return Resultado.Erro("Empresa não encontrada.");
        var empresa = await db.Empresas.SingleOrDefaultAsync(e => e.IdEmpresas == vm.Id);
        if (empresa is null) return Resultado.Erro("Empresa não encontrada.");
        if (ctx.AdminGlobal && !vm.Ativa && empresa.IdEmpresas == ctx.EmpresaId)
            return Resultado.Erro("Não é possível inativar a empresa ativa: troque de empresa antes.", nameof(vm.Ativa));
        var r = await AplicarAsync(empresa, vm);
        if (!r.Sucesso) return r;
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        return gravou.Sucesso ? Resultado.Ok() : Resultado.Erro(gravou.Mensagem!, CampoDoErro(gravou.Mensagem));
    }

    /// <summary>Logo da empresa ativa (ou de qualquer uma, ADMIN_GLOBAL) para a tela e os PDFs.</summary>
    public async Task<(byte[] Dados, string Mime)?> LogoAsync(int id)
    {
        if (!PodeVer(id)) return null;
        var l = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == id && e.Logo != null)
            .Select(e => new { e.Logo, e.LogoMime }).SingleOrDefaultAsync();
        return l is null ? null : (l.Logo!, l.LogoMime ?? "image/png");
    }

    /// <summary>Selo da acreditação (D-28) da empresa ativa (ou de qualquer uma, ADMIN_GLOBAL) para a tela.</summary>
    public async Task<(byte[] Dados, string Mime)?> SeloAsync(int id)
    {
        if (!PodeVer(id)) return null;
        var l = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == id && e.SeloAcreditacao != null)
            .Select(e => new { e.SeloAcreditacao, e.SeloAcreditacaoMime }).SingleOrDefaultAsync();
        return l is null ? null : (l.SeloAcreditacao!, l.SeloAcreditacaoMime ?? "image/png");
    }

    /// <summary>Entidade da empresa (endereços, telefones e e-mails), criada por SP_INICIALIZAR_EMPRESA.</summary>
    public async Task<int?> EntidadeDaEmpresaAsync(int id) =>
        PodeVer(id) ? await db.Empresas.Where(e => e.IdEmpresas == id).Select(e => e.IdEntidades).SingleOrDefaultAsync() : null;

    private bool PodeVer(int id) => ctx.AdminGlobal || id == ctx.EmpresaId;

    private async Task<Resultado> AplicarAsync(Empresa e, EmpresaVM vm)
    {
        string? cnpj = null;
        if (!string.IsNullOrWhiteSpace(vm.Cnpj))
        {
            if (!Documentos.CnpjValido(vm.Cnpj)) return Resultado.Erro("CNPJ inválido.", nameof(vm.Cnpj));
            cnpj = Documentos.FormatarCnpj(vm.Cnpj);
        }
        if (vm.NovoLogo is { Length: > 0 } arquivo)
        {
            var (bytes, mime, erro) = await LerImagemAsync(arquivo, "O logo");
            if (erro is not null) return Resultado.Erro(erro, nameof(vm.NovoLogo));
            (e.Logo, e.LogoMime) = (bytes, mime);
        }
        else if (vm.RemoverLogo) (e.Logo, e.LogoMime) = (null, null);
        if (vm.NovoSelo is { Length: > 0 } selo)
        {
            var (bytes, mime, erro) = await LerImagemAsync(selo, "O selo da acreditação");
            if (erro is not null) return Resultado.Erro(erro, nameof(vm.NovoSelo));
            (e.SeloAcreditacao, e.SeloAcreditacaoMime) = (bytes, mime);
        }
        else if (vm.RemoverSelo) (e.SeloAcreditacao, e.SeloAcreditacaoMime) = (null, null);

        e.RazaoSocial = vm.RazaoSocial.Trim();
        e.NomeFantasia = Textos.Limpo(vm.NomeFantasia);
        e.Cnpj = cnpj;
        e.InscEstadual = Textos.Limpo(vm.InscEstadual)?.ToUpperInvariant();
        e.InscMunicipal = Textos.Limpo(vm.InscMunicipal);
        e.Site = Textos.Limpo(vm.Site);
        e.CabecalhoLaudo = Textos.Limpo(vm.CabecalhoLaudo);
        e.RodapeLaudo = Textos.Limpo(vm.RodapeLaudo);
        e.NrAcreditacao = Textos.Limpo(vm.NrAcreditacao);
        if (ctx.AdminGlobal) e.Ativa = vm.Ativa; // usuário comum não inativa a própria empresa
        return Resultado.Ok();
    }

    // logo e selo: até 1 MB, PNG ou JPG pelo conteúdo (assinatura), não pelo nome/Content-Type enviados
    private static async Task<(byte[]? Bytes, string? Mime, string? Erro)> LerImagemAsync(IFormFile arquivo, string qual)
    {
        if (arquivo.Length > TamanhoMaximoLogo) return (null, null, $"{qual} deve ter no máximo 1 MB.");
        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var mime = bytes is [0x89, 0x50, 0x4E, 0x47, ..] ? "image/png" : bytes is [0xFF, 0xD8, 0xFF, ..] ? "image/jpeg" : null;
        return mime is null ? (null, null, $"{qual} deve ser uma imagem PNG ou JPG.") : (bytes, mime, null);
    }

    private static string? CampoDoErro(string? mensagem) => mensagem switch
    {
        "Já existe uma empresa com este código." => nameof(EmpresaVM.CodEmpresa),
        "Já existe uma empresa com este CNPJ." => nameof(EmpresaVM.Cnpj),
        _ => null,
    };

}
