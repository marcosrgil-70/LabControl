using LabControl.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LabControl.Infra;

/// <summary>
/// Revalida o cookie a cada request (docs/processos/P02-autenticacao-permissoes.md): usuário inativado, vínculo
/// com a empresa desativado ou ADMIN_GLOBAL alterado perde a sessão na hora. O bloqueio por tentativas NÃO derruba sessão aberta (senão
/// errar a senha de alguém 5 vezes encerraria a sessão dessa pessoa). As permissões ficam as da claim até a
/// próxima entrada ou troca de empresa.
/// </summary>
public sealed class ValidarSessao : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var valido = int.TryParse(principal?.FindFirst(LabClaims.UsuarioId)?.Value, out var usuario)
            && await context.HttpContext.RequestServices.GetRequiredService<ContaService>()
                .SessaoValidaAsync(usuario, int.TryParse(principal!.FindFirst(LabClaims.EmpresaId)?.Value, out var e) ? e : null,
                    principal.FindFirst(LabClaims.AdminGlobal)?.Value == "1");
        if (!valido)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
