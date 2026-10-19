using System.Security.Claims;

namespace LabControl.Infra;

/// <summary>
/// Empresa ativa, usuário e profissional do request (docs/processos/P01-multiempresa.md).
/// Sem empresa ativa: as consultas filtradas voltam vazias e as gravações são recusadas — nunca há empresa padrão.
/// </summary>
public interface IEmpresaContexto
{
    int? EmpresaId { get; }
    int? UsuarioId { get; }
    /// <summary><c>USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC</c> do usuário na empresa ativa.</summary>
    int? ProfissionalId { get; }
    bool AdminGlobal { get; }
}

/// <summary>Nomes das claims gravadas no login/troca de empresa (Fase 1, T00).</summary>
public static class LabClaims
{
    public const string UsuarioId = ClaimTypes.NameIdentifier;
    public const string EmpresaId = "lc:empresa";
    public const string ProfissionalId = "lc:profissional";
    public const string AdminGlobal = "lc:admin_global";
}

/// <summary>Lê o contexto das claims do usuário autenticado.</summary>
public sealed class EmpresaContexto(IHttpContextAccessor http) : IEmpresaContexto
{
    private ClaimsPrincipal? Usuario => http.HttpContext?.User;

    private int? Inteiro(string claim) => int.TryParse(Usuario?.FindFirstValue(claim), out var v) ? v : null;

    public int? EmpresaId => Inteiro(LabClaims.EmpresaId);
    public int? UsuarioId => Usuario?.Identity?.IsAuthenticated == true ? Inteiro(LabClaims.UsuarioId) : null;
    public int? ProfissionalId => Inteiro(LabClaims.ProfissionalId);
    public bool AdminGlobal => Usuario?.FindFirstValue(LabClaims.AdminGlobal) == "1";
}
