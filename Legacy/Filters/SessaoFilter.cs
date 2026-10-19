using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabControl.Filters;

public class SessaoFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();

        // Login não precisa de sessão ativa
        if (string.Equals(controller, "Login", StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        var usuario = context.HttpContext.Session.GetString("UsuarioNome");
        if (string.IsNullOrEmpty(usuario))
        {
            context.Result = new RedirectToActionResult("Index", "Login", null);
            return;
        }

        base.OnActionExecuting(context);
    }
}
