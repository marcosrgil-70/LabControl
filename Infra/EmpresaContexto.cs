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
    /// <summary>IP do cliente (gravado na AUDITORIA).</summary>
    string? Ip { get; }
    /// <summary>Permissão <c>TELA.ACAO</c> na empresa ativa (ADMIN_GLOBAL tem todas).</summary>
    bool TemPermissao(string codigo);
}

/// <summary>Nomes das claims gravadas no login/troca de empresa (T00, Services/ContaService.cs).</summary>
public static class LabClaims
{
    public const string UsuarioId = ClaimTypes.NameIdentifier;
    public const string Login = "lc:login";
    public const string EmpresaId = "lc:empresa";
    public const string EmpresaCodigo = "lc:empresa_codigo";
    public const string EmpresaNome = "lc:empresa_nome";
    public const string ProfissionalId = "lc:profissional";
    public const string AdminGlobal = "lc:admin_global";
    /// <summary>Uma claim por código de permissão (união dos perfis ativos do usuário na empresa).</summary>
    public const string Permissao = "lc:permissao";
    /// <summary>Troca de senha obrigatória pendente: só /Conta/TrocarSenha e /Conta/Sair.</summary>
    public const string TrocarSenha = "lc:trocar_senha";
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
    public string? Ip => http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool TemPermissao(string codigo) =>
        AdminGlobal || (EmpresaId is not null && Usuario?.HasClaim(LabClaims.Permissao, codigo) == true);
}
