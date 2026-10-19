using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LabControl.Infra;

/// <summary>Chaves de TempData exibidas pelo _Layout.</summary>
public static class Mensagens
{
    public const string Sucesso = "Sucesso";
    public const string Erro = "Erro";
}

public static class ResultadoMvcExtensions
{
    /// <summary>
    /// Formulário: erro vai para o ModelState (no campo, se informado) para reexibir a view;
    /// sucesso grava a mensagem opcional no TempData (para depois do redirect). Devolve <c>Sucesso</c>.
    /// </summary>
    public static bool AplicarNoFormulario(this Controller controller, IResultado resultado, string? mensagemSucesso = null)
    {
        if (resultado.Sucesso)
        {
            if (mensagemSucesso is not null) controller.TempData[Mensagens.Sucesso] = mensagemSucesso;
            return true;
        }
        controller.ModelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Mensagem!);
        return false;
    }

    /// <summary>Ação sem formulário (botão "Gerar revisão", "Emitir"…): mensagem no TempData para depois do redirect.</summary>
    public static bool AplicarNoTempData(this Controller controller, IResultado resultado, string? mensagemSucesso = null)
    {
        if (resultado.Sucesso)
        {
            if (mensagemSucesso is not null) controller.TempData[Mensagens.Sucesso] = mensagemSucesso;
            return true;
        }
        controller.TempData[Mensagens.Erro] = resultado.Mensagem;
        return false;
    }
}

/// <summary>
/// Rede de segurança para erro de negócio do MySQL que escapou do serviço (45000, 1062, 1451, 1452):
/// não é erro técnico — mostra a mensagem em vez da página 500. AJAX recebe 422 + JSON <c>{ erro }</c>;
/// navegação volta para a página anterior com a mensagem no TempData. Erros técnicos seguem para o handler padrão.
/// </summary>
public sealed class ErroNegocioFilter(ITempDataDictionaryFactory tempData) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (ErrosMySql.MensagemDeNegocio(context.Exception) is not { } mensagem) return;

        var req = context.HttpContext.Request;
        var ajax = req.Headers.XRequestedWith == "XMLHttpRequest"
                   || req.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        if (ajax)
        {
            context.Result = new JsonResult(new { erro = mensagem }) { StatusCode = StatusCodes.Status422UnprocessableEntity };
        }
        else
        {
            tempData.GetTempData(context.HttpContext)[Mensagens.Erro] = mensagem;
            var volta = req.Headers.Referer.FirstOrDefault();
            context.Result = Uri.TryCreate(volta, UriKind.Absolute, out var uri) && uri.Host == req.Host.Host
                ? new RedirectResult(uri.PathAndQuery)
                : new RedirectToActionResult("Index", "Home", null);
        }
        context.ExceptionHandled = true;
    }
}
