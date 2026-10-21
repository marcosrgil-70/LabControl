using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T04 — Perfis e permissões (docs/telas/T04-perfis-permissoes.md). Perfis são da empresa ativa (filtro global);
/// o catálogo <c>PERMISSOES</c> é fixo (05_seed.sql). PERFIS e PERFIS_PERMISSOES são auditados pelo SaveChanges.
/// O perfil ADMINISTRADOR (SP_INICIALIZAR_EMPRESA) tem sempre todas as permissões: não é alterado nem excluído.
/// </summary>
public sealed class PerfilService(LabDbContext db, IEmpresaContexto ctx)
{
    public const string Administrador = "ADMINISTRADOR";
    public const string MensagemAdministrador = "O perfil ADMINISTRADOR tem sempre todas as permissões: não pode ser alterado nem excluído.";

    public async Task<IReadOnlyList<PerfilListaItemVM>> ListarAsync() =>
        await db.Perfis.AsNoTracking().OrderBy(p => p.Nome)
            .Select(p => new PerfilListaItemVM(p.IdPerfis, p.Nome, p.Descricao, p.Ativo,
                db.PerfisPermissoes.Count(pp => pp.IdPerfis == p.IdPerfis),
                db.UsuariosPerfis.Count(up => up.IdPerfis == p.IdPerfis),
                p.Nome == Administrador))
            .ToListAsync();

    /// <summary>Catálogo como matriz: telas agrupadas por módulo, na ordem de <c>PERMISSOES.ORDEM</c>.</summary>
    public async Task<IReadOnlyList<TelaMatriz>> MatrizAsync()
    {
        var todas = await db.Permissoes.AsNoTracking().OrderBy(p => p.Ordem).ToListAsync();
        return todas.GroupBy(p => (p.Modulo, p.Tela))
            .OrderBy(g => g.Min(p => p.Ordem))
            .Select(g => new TelaMatriz(g.Key.Modulo, g.Key.Tela, NomeDaTela(g.First().Descricao),
                g.Where(p => PerfilVM.AcoesPadrao.Contains(p.Acao)).ToDictionary(p => p.Acao, Celula),
                g.Where(p => !PerfilVM.AcoesPadrao.Contains(p.Acao)).Select(Celula).ToList()))
            .ToList();
    }

    public async Task<PerfilVM> NovoAsync() => new() { Matriz = await MatrizAsync() };

    public async Task<PerfilVM?> CarregarAsync(int id)
    {
        var p = await db.Perfis.AsNoTracking().SingleOrDefaultAsync(x => x.IdPerfis == id);
        if (p is null) return null;
        return new PerfilVM
        {
            Id = p.IdPerfis, Nome = p.Nome, Descricao = p.Descricao, Ativo = p.Ativo, Administrador = p.Nome == Administrador,
            Permissoes = await db.PerfisPermissoes.Where(pp => pp.IdPerfis == id).Select(pp => pp.IdPermissoes).ToListAsync(),
            Matriz = await MatrizAsync(),
        };
    }

    public async Task<Resultado<int>> IncluirAsync(PerfilVM vm)
    {
        var nome = Normalizar(vm.Nome);
        if (nome == Administrador) return Resultado.Erro("O nome ADMINISTRADOR é reservado.", nameof(vm.Nome));
        var perfil = new Perfil { Nome = nome, Descricao = Textos.Limpo(vm.Descricao), Ativo = vm.Ativo };
        await using var tx = await db.Database.BeginTransactionAsync();
        db.Add(perfil);
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!, nameof(vm.Nome));
        await GravarPermissoesAsync(perfil.IdPerfis, vm.Permissoes);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Resultado.Ok(perfil.IdPerfis);
    }

    public async Task<Resultado> AlterarAsync(PerfilVM vm)
    {
        var perfil = await db.Perfis.SingleOrDefaultAsync(p => p.IdPerfis == vm.Id);
        if (perfil is null) return Resultado.Erro("Perfil não encontrado.");
        if (perfil.Nome == Administrador) return Resultado.Erro(MensagemAdministrador);
        var nome = Normalizar(vm.Nome);
        if (nome == Administrador) return Resultado.Erro("O nome ADMINISTRADOR é reservado.", nameof(vm.Nome));
        perfil.Nome = nome;
        perfil.Descricao = Textos.Limpo(vm.Descricao);
        perfil.Ativo = vm.Ativo;
        await using var tx = await db.Database.BeginTransactionAsync();
        await GravarPermissoesAsync(perfil.IdPerfis, vm.Permissoes);
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!, nameof(vm.Nome));
        await tx.CommitAsync();
        return Resultado.Ok();
    }

    /// <summary>Proibido para o ADMINISTRADOR e para perfis com usuários (remover dos usuários antes ou inativar).</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var perfil = await db.Perfis.SingleOrDefaultAsync(p => p.IdPerfis == id);
        if (perfil is null) return Resultado.Erro("Perfil não encontrado.");
        if (perfil.Nome == Administrador) return Resultado.Erro(MensagemAdministrador);
        if (await db.UsuariosPerfis.AnyAsync(up => up.IdPerfis == id))
            return Resultado.Erro("Este perfil está atribuído a usuários: remova-o dos usuários antes ou inative o perfil.");
        await using var tx = await db.Database.BeginTransactionAsync();
        db.RemoveRange(await db.PerfisPermissoes.Where(pp => pp.IdPerfis == id).ToListAsync()); // auditado (E) um a um
        db.Remove(perfil);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Resultado.Ok();
    }

    /// <summary>Perfis que podem ser copiados: os da empresa ativa e, para ADMIN_GLOBAL, os das outras empresas ativas.</summary>
    public async Task<IReadOnlyList<PerfilOrigemVM>> OrigensAsync()
    {
        var q = ctx.AdminGlobal
            // ADMIN_GLOBAL copiando de outra empresa: sem o filtro global, só empresas ativas
            ? db.Perfis.IgnoreQueryFilters().Where(p => p.Empresa.Ativa)
            : db.Perfis;
        return await q.AsNoTracking().OrderBy(p => p.IdEmpresas != ctx.EmpresaId).ThenBy(p => p.Empresa.CodEmpresa).ThenBy(p => p.Nome)
            .Select(p => new PerfilOrigemVM(p.IdPerfis, p.Nome, p.Empresa.CodEmpresa + " — " + (p.Empresa.NomeFantasia ?? p.Empresa.RazaoSocial),
                                            p.IdEmpresas == ctx.EmpresaId))
            .ToListAsync();
    }

    /// <summary>Novo perfil na empresa ativa com as permissões do perfil de origem (da mesma empresa ou, ADMIN_GLOBAL, de outra).</summary>
    public async Task<Resultado<int>> CopiarAsync(int idOrigem, string nome)
    {
        if (!(await OrigensAsync()).Any(o => o.Id == idOrigem)) return Resultado.Erro("Perfil de origem não encontrado.", nameof(CopiarPerfilVM.IdOrigem));
        var origem = await db.Perfis.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.IdPerfis == idOrigem);
        var permissoes = await db.PerfisPermissoes.IgnoreQueryFilters().Where(pp => pp.IdPerfis == idOrigem).Select(pp => pp.IdPermissoes).ToListAsync();
        var r = await IncluirAsync(new PerfilVM { Nome = nome, Descricao = origem.Descricao, Ativo = true, Permissoes = permissoes });
        return r.Sucesso ? r : Resultado.Erro(r.Mensagem!, r.Campo is null ? null : nameof(CopiarPerfilVM.Nome));
    }

    /// <summary>
    /// Sincroniza PERFIS_PERMISSOES (inclui/remove só a diferença, para a auditoria mostrar o que mudou). Só IDs do catálogo;
    /// marcar qualquer ação de uma tela marca também o CONSULTAR dela (sem consultar não se chega à tela).
    /// </summary>
    private async Task GravarPermissoesAsync(int idPerfil, IEnumerable<int> marcadas)
    {
        var catalogo = await db.Permissoes.AsNoTracking().ToListAsync();
        var escolhidas = catalogo.Where(p => marcadas.Contains(p.IdPermissoes)).ToList();
        var consultar = catalogo.Where(p => p.Acao == "CONSULTAR" && escolhidas.Any(e => e.Tela == p.Tela));
        var finais = escolhidas.Concat(consultar).Select(p => p.IdPermissoes).ToHashSet();

        var atuais = await db.PerfisPermissoes.Where(pp => pp.IdPerfis == idPerfil).ToListAsync();
        db.RemoveRange(atuais.Where(a => !finais.Contains(a.IdPermissoes)));
        foreach (var id in finais.Where(f => atuais.All(a => a.IdPermissoes != f)))
            db.Add(new PerfilPermissao { IdPerfis = idPerfil, IdPermissoes = id });
    }

    // "Recebimento de Amostras — Consultar" → "Recebimento de Amostras"
    private static string NomeDaTela(string descricao) => descricao.Split(" — ")[0];
    private static PermissaoCelula Celula(Permissao p) => new(p.IdPermissoes, p.Codigo, p.Acao, p.Descricao.Split(" — ").Last());
    private static string Normalizar(string nome) => nome.Trim().ToUpperInvariant();
}
