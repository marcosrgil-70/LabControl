using System.Security.Cryptography;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Entidades;

/// <summary>
/// T13 — Profissionais (funcionários) (docs/telas/T13-profissionais.md, P25): <c>ENTIDADES</c> pessoa física com
/// <c>TIPO_FUNCIONARIO</c> + <c>ENTIDADES_FUNCIONARIOS</c> (cargo, registro, flags, assinatura) + vínculo com o usuário
/// pelo <see cref="VinculoUsuarioProfissionalService"/> (mesma regra da T02). Contatos pelo <see cref="ContatosService"/>.
/// </summary>
public sealed class ProfissionalService(LabDbContext db, IEmpresaContexto ctx, VinculoUsuarioProfissionalService vinculo)
{
    public const int TamanhoMaximoAssinatura = 300 * 1024;
    public const string MensagemNaoEncontrado = "Profissional não encontrado (pode ter sido excluído).";

    private int Ativa => ctx.EmpresaId ?? throw new InvalidOperationException("Sem empresa ativa.");

    public async Task<ProfissionalListaVM> ListarAsync(ProfissionalFiltroVM f)
    {
        var q = db.VwProfissionais.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            q = q.Where(p => p.Nome.Contains(t) || (p.Sobrenome != null && p.Sobrenome.Contains(t)) || (p.Login != null && p.Login.Contains(t))
                             || (p.NrRegistroProfissional != null && p.NrRegistroProfissional.Contains(t)));
        }
        var ocultos = f.Inativos ? 0 : await q.CountAsync(p => p.Inativo);
        if (!f.Inativos) q = q.Where(p => !p.Inativo);
        var total = await q.CountAsync();
        f.Pagina = Math.Clamp(f.Pagina, 1, Math.Max(1, (total + ProfissionalFiltroVM.TamanhoPagina - 1) / ProfissionalFiltroVM.TamanhoPagina));
        var itens = await q.OrderBy(p => p.Nome).ThenBy(p => p.Sobrenome)
            .Skip((f.Pagina - 1) * ProfissionalFiltroVM.TamanhoPagina).Take(ProfissionalFiltroVM.TamanhoPagina)
            .Select(p => new ProfissionalListaItemVM(p.IdEntidades, p.NomeCompleto ?? p.Nome, p.Cargo, p.RegistroCompleto,
                p.AssinaBoletim, p.ExecutaAnalise, p.TemAssinatura == 1, p.Login, p.Inativo))
            .ToListAsync();
        return new ProfissionalListaVM { Filtro = f, Total = total, Itens = itens, InativosOcultos = ocultos };
    }

    public async Task<ProfissionalVM> NovoAsync()
    {
        var vm = new ProfissionalVM();
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<ProfissionalVM?> CarregarAsync(int id)
    {
        var p = await db.EntidadesFuncionarios.AsNoTracking().Where(f => f.IdEntidades == id && f.Entidade.TipoFuncionario)
            .Select(f => new
            {
                f.Entidade.Nome, f.Entidade.NomeFantasia, f.Entidade.CpfCnpj, f.Entidade.RgIe, f.Entidade.Inativo,
                f.IdCargosFuncionarios, f.IdTiposRegProfissional, f.NrRegistroProfissional, f.UfRegistro, f.AssinaBoletim,
                f.ExecutaAnalise, TemAssinatura = f.Assinatura != null,
            }).SingleOrDefaultAsync();
        if (p is null) return null;
        var vm = new ProfissionalVM
        {
            Id = id, Nome = p.Nome, Sobrenome = p.NomeFantasia, Cpf = p.CpfCnpj, Rg = p.RgIe, Inativo = p.Inativo,
            IdCargosFuncionarios = p.IdCargosFuncionarios, IdTiposRegProfissional = p.IdTiposRegProfissional,
            NrRegistroProfissional = p.NrRegistroProfissional, UfRegistro = p.UfRegistro, AssinaBoletim = p.AssinaBoletim,
            ExecutaAnalise = p.ExecutaAnalise, TemAssinatura = p.TemAssinatura,
            IdUsuario = await UsuarioVinculadoAsync(id),
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Combos: cargos e tipos de registro (ativos + o escolhido), UFs e usuários livres (+ o vinculado).</summary>
    public async Task PrepararAsync(ProfissionalVM vm)
    {
        var cargo = vm.IdCargosFuncionarios ?? 0;
        var registro = vm.IdTiposRegProfissional ?? 0;
        vm.Cargos = await db.CargosFuncionarios.AsNoTracking().Where(c => c.Ativo || c.IdCargosFuncionarios == cargo)
            .OrderBy(c => c.Descricao).Select(c => new OpcaoVM(c.IdCargosFuncionarios, c.Descricao)).ToListAsync();
        vm.TiposRegistro = await db.TiposRegProfissional.AsNoTracking().Where(t => t.Ativo || t.IdTiposRegProfissional == registro)
            .OrderBy(t => t.Descricao).Select(t => new OpcaoVM(t.IdTiposRegProfissional, t.Descricao)).ToListAsync();
        vm.Ufs = await db.Estados.AsNoTracking().OrderBy(e => e.Sigla).Select(e => e.Sigla).ToListAsync();
        var empresa = Ativa;
        vm.Usuarios = await db.UsuariosEmpresas.AsNoTracking()
            .Where(v => v.IdEmpresas == empresa && v.Ativo && v.Usuario.Ativo && (v.IdEntidadesFunc == null || v.IdEntidadesFunc == vm.Id))
            .OrderBy(v => v.Usuario.Login).Select(v => new OpcaoVM(v.IdUsuarios, v.Usuario.Login + " — " + v.Usuario.Nome)).ToListAsync();
        if (vm.Id != 0)
            vm.TemAssinatura = await db.EntidadesFuncionarios.AnyAsync(f => f.IdEntidades == vm.Id && f.Assinatura != null);
    }

    /// <summary>Inclui ou altera (entidade + dados do profissional + vínculo com o usuário) numa transação.</summary>
    public async Task<Resultado<int>> GravarAsync(ProfissionalVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nome)) return Resultado.Erro("Informe o nome.", nameof(vm.Nome));
        string? cpf = null;
        if (!string.IsNullOrWhiteSpace(vm.Cpf))
        {
            if (!Documentos.CpfValido(vm.Cpf)) return Resultado.Erro("CPF inválido.", nameof(vm.Cpf));
            cpf = Documentos.FormatarCpf(vm.Cpf);
        }
        if (vm.IdCargosFuncionarios is { } cargo && !await db.CargosFuncionarios.AnyAsync(c => c.IdCargosFuncionarios == cargo))
            return Resultado.Erro("Cargo não encontrado.", nameof(vm.IdCargosFuncionarios));
        if (vm.IdTiposRegProfissional is { } tr && !await db.TiposRegProfissional.AnyAsync(t => t.IdTiposRegProfissional == tr))
            return Resultado.Erro("Tipo de registro não encontrado.", nameof(vm.IdTiposRegProfissional));
        var uf = string.IsNullOrWhiteSpace(vm.UfRegistro) ? null : vm.UfRegistro.Trim().ToUpperInvariant();
        if (uf is not null && !await db.Estados.AnyAsync(e => e.Sigla == uf)) return Resultado.Erro("UF inválida.", nameof(vm.UfRegistro));
        var nrRegistro = string.IsNullOrWhiteSpace(vm.NrRegistroProfissional) ? null : vm.NrRegistroProfissional.Trim().ToUpperInvariant();
        // RN-03.15: quem assina boletim precisa de tipo, nº e UF do registro (impressos no boletim)
        if (vm.AssinaBoletim && (vm.IdTiposRegProfissional is null || nrRegistro is null || uf is null))
            return Resultado.Erro("Quem assina boletim precisa de tipo, nº e UF do registro profissional.", nameof(vm.IdTiposRegProfissional));

        (byte[] Dados, string Hash)? assinatura = null;
        if (vm.NovaAssinatura is { Length: > 0 } arquivo)
        {
            if (arquivo.Length > TamanhoMaximoAssinatura) return Resultado.Erro("A assinatura deve ter no máximo 300 KB.", nameof(vm.NovaAssinatura));
            using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms);
            var bytes = ms.ToArray();
            if (bytes is not [0x89, 0x50, 0x4E, 0x47, ..]) return Resultado.Erro("A assinatura deve ser uma imagem PNG.", nameof(vm.NovaAssinatura));
            assinatura = (bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        Entidade? e;
        EntidadeFuncionario? f;
        if (vm.Id == 0)
        {
            e = new Entidade { Categoria = Categoria.Fisica, TipoFuncionario = true };
            f = new EntidadeFuncionario { Entidade = e };
            db.AddRange(e, f);
        }
        else
        {
            f = await db.EntidadesFuncionarios.Include(x => x.Entidade).SingleOrDefaultAsync(x => x.IdEntidades == vm.Id && x.Entidade.TipoFuncionario);
            if (f is null) return Resultado.Erro(MensagemNaoEncontrado);
            e = f.Entidade;
        }
        e.Nome = vm.Nome.Trim().ToUpperInvariant();
        e.NomeFantasia = string.IsNullOrWhiteSpace(vm.Sobrenome) ? null : vm.Sobrenome.Trim().ToUpperInvariant();
        e.CpfCnpj = cpf;
        e.RgIe = string.IsNullOrWhiteSpace(vm.Rg) ? null : vm.Rg.Trim().ToUpperInvariant();
        e.Inativo = vm.Inativo;
        f.IdCargosFuncionarios = vm.IdCargosFuncionarios;
        f.IdTiposRegProfissional = vm.IdTiposRegProfissional;
        f.NrRegistroProfissional = nrRegistro;
        f.UfRegistro = uf;
        f.AssinaBoletim = vm.AssinaBoletim;
        f.ExecutaAnalise = vm.ExecutaAnalise;
        if (assinatura is { } a) (f.Assinatura, f.AssinaturaMime, f.AssinaturaHash) = (a.Dados, "image/png", a.Hash);
        else if (vm.RemoverAssinatura) (f.Assinatura, f.AssinaturaMime, f.AssinaturaHash) = (null, null, null);

        var gravou = await ErrosMySql.CapturarAsync(() => (Task)db.SaveChangesAsync());
        if (!gravou.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(gravou.Mensagem!, gravou.Mensagem!.Contains("CPF/CNPJ") ? nameof(vm.Cpf) : null);
        }

        // vínculo com o usuário (T02 ↔ T13): inativo fica sem usuário (o usuário perde o "profissional ativo")
        var desejado = vm.Inativo ? null : vm.IdUsuario;
        var atual = await UsuarioVinculadoAsync(e.IdEntidades);
        if (atual != desejado)
        {
            if (atual is { } antigo && await vinculo.VincularAsync(antigo, Ativa, null) is { Sucesso: false } r1)
                return Resultado.Erro(r1.Mensagem!, nameof(vm.IdUsuario));
            if (desejado is { } novo && (await vinculo.VincularAsync(novo, Ativa, e.IdEntidades)) is { Sucesso: false } r2)
                return Resultado.Erro(r2.Mensagem!, nameof(vm.IdUsuario));
        }
        await tx.CommitAsync();
        return Resultado.Ok(e.IdEntidades);
    }

    /// <summary>Exclui só sem movimento e sem usuário vinculado; senão a mensagem sugere inativar.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var e = await db.Entidades.AsNoTracking().Where(x => x.IdEntidades == id && x.TipoFuncionario)
            .Select(x => new { Outros = x.TipoCliente || x.TipoFornecedor || x.TipoVendedor || x.TipoTransportador }).SingleOrDefaultAsync();
        if (e is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (e.Outros) return Resultado.Erro("Este cadastro também é cliente/fornecedor: inative-o em vez de excluir.");
        if (await UsuarioVinculadoAsync(id) is not null)
            return Resultado.Erro("Profissional vinculado a um usuário: retire o vínculo (ou inative o profissional) antes de excluir.");
        var r = await ErrosMySql.CapturarAsync(() => db.Entidades.Where(x => x.IdEntidades == id).ExecuteDeleteAsync());
        return r.Sucesso ? Resultado.Ok() : Resultado.Erro(r.Mensagem!);
    }

    /// <summary>Imagem da assinatura (só da empresa ativa).</summary>
    public async Task<byte[]?> AssinaturaAsync(int id) =>
        await db.EntidadesFuncionarios.AsNoTracking().Where(f => f.IdEntidades == id).Select(f => f.Assinatura).SingleOrDefaultAsync();

    private async Task<int?> UsuarioVinculadoAsync(int idProfissional)
    {
        var empresa = Ativa;
        return await db.UsuariosEmpresas.Where(v => v.IdEmpresas == empresa && v.IdEntidadesFunc == idProfissional)
            .Select(v => (int?)v.IdUsuarios).FirstOrDefaultAsync();
    }
}
