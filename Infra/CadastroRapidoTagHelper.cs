using LabControl.Services.Auxiliares;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace LabControl.Infra;

/// <summary>
/// Cadastro rápido "+" (T10-00, tarefa 2.4): <c>&lt;select lc-rapido="IDIOMAS" …&gt;</c> ganha, pelo <c>site.js</c>, um
/// botão que inclui o auxiliar num modal e já o deixa selecionado. Sem AUXILIARES.INCLUIR o combo fica como está
/// (a gravação em <c>/Auxiliares/{tabela}/Rapido</c> também exige a permissão).
/// </summary>
[HtmlTargetElement("select", Attributes = "lc-rapido")]
public sealed class CadastroRapidoTagHelper(IEmpresaContexto usuario) : TagHelper
{
    [HtmlAttributeName("lc-rapido")] public string Tabela { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (CatalogoAuxiliares.Encontrar(Tabela) is not { } def)
            throw new InvalidOperationException($"lc-rapido: \"{Tabela}\" não está no CatalogoAuxiliares.");
        if (!usuario.TemPermissao("AUXILIARES.INCLUIR")) return;
        output.Attributes.SetAttribute("data-rapido", def.Tabela);
        output.Attributes.SetAttribute("data-rapido-nome", def.Singular);
    }
}
