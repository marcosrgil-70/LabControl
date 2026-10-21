using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

public sealed record ProfissionalDisponivel(int IdEntidades, string Nome, int? IdUsuarioVinculado, string? LoginVinculado);

/// <summary>
/// Vínculo usuário ↔ profissional por empresa (<c>USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC</c>): um serviço só para a T02
/// (grid de empresas do usuário) e a T13 (campo "Usuário do sistema"). Regras: o profissional é da empresa, está ativo
/// e não está vinculado a outro usuário nessa empresa (RN-02.09 / <c>UQ_USUEMP_FUNC</c>, aqui com mensagem clara).
/// Empresa diferente da ativa só para ADMIN_GLOBAL (a tela de outra empresa lê sem o filtro global).
/// </summary>
public sealed class VinculoUsuarioProfissionalService(LabDbContext db, IEmpresaContexto usuario)
{
    public const string MensagemProfissionalEmUso = "Este profissional já está vinculado a outro usuário nesta empresa.";

    /// <summary>Profissionais ativos da empresa, com o usuário a que já estão vinculados (para o combo).</summary>
    public async Task<IReadOnlyList<ProfissionalDisponivel>> ProfissionaisAsync(int idEmpresa) =>
        await Profissionais(idEmpresa)
            .Where(p => !p.Inativo)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalDisponivel(p.IdEntidades, p.NomeCompleto ?? p.Nome, p.IdUsuarios, p.Login))
            .ToListAsync();

    /// <summary>Confere se <paramref name="idProfissional"/> pode ser o profissional de <paramref name="idUsuario"/> na empresa.</summary>
    public async Task<Resultado> ValidarAsync(int idUsuario, int idEmpresa, int? idProfissional, string? campo = null)
    {
        if (idProfissional is not { } func) return Resultado.Ok();
        var prof = await Profissionais(idEmpresa).Where(p => p.IdEntidades == func)
            .Select(p => new { p.Inativo }).SingleOrDefaultAsync();
        if (prof is null) return Resultado.Erro("Profissional não encontrado nesta empresa.", campo);
        if (prof.Inativo) return Resultado.Erro("Profissional inativo.", campo);
        if (await db.UsuariosEmpresas.AnyAsync(v => v.IdEmpresas == idEmpresa && v.IdEntidadesFunc == func && v.IdUsuarios != idUsuario))
            return Resultado.Erro(MensagemProfissionalEmUso, campo);
        return Resultado.Ok();
    }

    /// <summary>Grava o vínculo (T13). O usuário já precisa ter acesso à empresa (linha em USUARIOS_EMPRESAS).</summary>
    public async Task<Resultado> VincularAsync(int idUsuario, int idEmpresa, int? idProfissional)
    {
        var vinculo = await db.UsuariosEmpresas.SingleOrDefaultAsync(v => v.IdUsuarios == idUsuario && v.IdEmpresas == idEmpresa);
        if (vinculo is null) return Resultado.Erro("O usuário não tem acesso a esta empresa.");
        var ok = await ValidarAsync(idUsuario, idEmpresa, idProfissional);
        if (!ok.Sucesso) return ok;
        vinculo.IdEntidadesFunc = idProfissional;
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync()); // UQ_USUEMP_FUNC continua valendo (concorrência)
        return r.Sucesso ? Resultado.Ok() : r.Falha;
    }

    private IQueryable<Models.VwProfissional> Profissionais(int idEmpresa)
    {
        if (idEmpresa != usuario.EmpresaId && !usuario.AdminGlobal)
            throw new InvalidOperationException("Profissionais de outra empresa só para ADMIN_GLOBAL.");
        // ADMIN_GLOBAL editando outra empresa: lê sem o filtro global, com a empresa fixada no Where
        return db.VwProfissionais.IgnoreQueryFilters().Where(p => p.IdEmpresas == idEmpresa);
    }
}
