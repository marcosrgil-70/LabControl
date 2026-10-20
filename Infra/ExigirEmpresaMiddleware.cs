using Microsoft.AspNetCore.Mvc.Controllers;

namespace LabControl.Infra;

/// <summary>
/// Etapas obrigatórias do login (T00), só para actions MVC (arquivos estáticos passam):
/// 1. troca de senha pendente (<see cref="LabClaims.TrocarSenha"/>) → só /Conta/TrocarSenha e /Conta/Sair;
/// 2. autenticado sem empresa ativa → escolha de empresa (as rotas do controller Conta ficam liberadas).
/// Registrar depois de UseRouting/UseAuthentication.
/// </summary>
public sealed class ExigirEmpresaMiddleware(RequestDelegate next)
{
    public const string RotaEscolherEmpresa = "/Conta/Empresa";
    public const string RotaTrocarSenha = "/Conta/TrocarSenha";

    public Task InvokeAsync(HttpContext context, IEmpresaContexto empresa)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action)
        {
            var conta = string.Equals(action.ControllerName, "Conta", StringComparison.OrdinalIgnoreCase);
            if (context.User.HasClaim(LabClaims.TrocarSenha, "1"))
            {
                if (!(conta && action.ActionName is "TrocarSenha" or "Sair"))
                    return Redirecionar(context, RotaTrocarSenha);
            }
            else if (empresa.EmpresaId is null && !conta)
                return Redirecionar(context, RotaEscolherEmpresa);
        }
        return next(context);
    }

    private static Task Redirecionar(HttpContext context, string rota)
    {
        context.Response.Redirect(rota);
        return Task.CompletedTask;
    }
}
