using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace LabControl.Infra;

/// <summary>
/// Exige a permissão <c>TELA.ACAO</c> na empresa ativa (RN-02.07; ADMIN_GLOBAL passa sempre). Sem ela: 403 —
/// página "Acesso negado" ou, em AJAX, JSON <c>{ erro }</c>. Pode ser repetido (todas são exigidas) e usado na
/// classe (vale para todas as actions) e na action.
/// <code>[Permissao("AMOSTRAS.INCLUIR")] public IActionResult Criar() …</code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissaoAttribute(string codigo) : Attribute, IAuthorizationFilter
{
    public const string MensagemSemPermissao = "Você não tem permissão para esta ação.";

    public string Codigo { get; } = codigo;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.Result is not null) return;
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }
        if (!context.HttpContext.RequestServices.GetRequiredService<IEmpresaContexto>().TemPermissao(Codigo))
            context.Result = AcessoNegado(context.HttpContext, context.ModelState, Codigo);
    }

    public static IActionResult AcessoNegado(HttpContext http, ModelStateDictionary modelState, string? codigo)
    {
        if (http.Request.Headers.XRequestedWith == "XMLHttpRequest")
            return new JsonResult(new { erro = MensagemSemPermissao }) { StatusCode = StatusCodes.Status403Forbidden };
        return new ViewResult
        {
            ViewName = "AcessoNegado",
            StatusCode = StatusCodes.Status403Forbidden,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState) { ["Permissao"] = codigo },
        };
    }
}

/// <summary>
/// Mostra o conteúdo só com a permissão (menu e botões — RN-02.07):
/// <c>&lt;permissao codigo="AMOSTRAS.INCLUIR"&gt;…&lt;/permissao&gt;</c> ou, num elemento,
/// <c>&lt;a lc-permissao="AMOSTRAS.INCLUIR" …&gt;</c>. Esconder não substitui o <see cref="PermissaoAttribute"/> na action.
/// </summary>
[HtmlTargetElement("permissao", Attributes = "codigo")]
[HtmlTargetElement(Attributes = "lc-permissao")]
public sealed class PermissaoTagHelper(IEmpresaContexto usuario) : TagHelper
{
    [HtmlAttributeName("codigo")] public string? Codigo { get; set; }
    [HtmlAttributeName("lc-permissao")] public string? NoElemento { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!usuario.TemPermissao(NoElemento ?? Codigo ?? ""))
            output.SuppressOutput();
        else if (output.TagName == "permissao")
            output.TagName = null; // só o conteúdo
    }
}
