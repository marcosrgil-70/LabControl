using Microsoft.AspNetCore.Mvc.Controllers;

namespace LabControl.Infra;

/// <summary>
/// Request autenticado sem empresa ativa vai para a escolha de empresa (T00). Só vale para actions MVC
/// (arquivos estáticos passam) e libera as rotas de conta (login, troca de senha, escolha de empresa).
/// Registrar depois de UseRouting/UseAuthentication.
/// </summary>
public sealed class ExigirEmpresaMiddleware(RequestDelegate next)
{
    public const string RotaEscolherEmpresa = "/Conta/Empresa";

    public Task InvokeAsync(HttpContext context, IEmpresaContexto empresa)
    {
        if (context.User.Identity?.IsAuthenticated == true && empresa.EmpresaId is null
            && context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action
            && !string.Equals(action.ControllerName, "Conta", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect(RotaEscolherEmpresa);
            return Task.CompletedTask;
        }
        return next(context);
    }
}
