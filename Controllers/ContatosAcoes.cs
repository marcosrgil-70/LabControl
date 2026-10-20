using LabControl.Infra;
using LabControl.Services.Entidades;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>
/// Respostas das ações de contatos (endereços/telefones/e-mails) usadas pela Empresa (T03), Clientes (T12) e
/// Profissionais (T13). Cada controller declara as próprias actions com a sua <see cref="PermissaoAttribute"/>
/// e só delega para cá. Protocolo do componente <c>_ContatosAbas</c>: GET devolve o HTML das abas; POST devolve
/// <c>{ ok, id }</c> ou 422 <c>{ erro, campo }</c> e a tela recarrega as abas.
/// </summary>
public static class ContatosAcoes
{
    public static async Task<IActionResult> AbasAsync(this Controller c, ContatosService s, CadastroDono dono, int idEntidade,
        string urlBase, string permissaoAlterar, IEmpresaContexto ctx)
    {
        var vm = await s.CarregarAsync(dono, idEntidade, urlBase, ctx.TemPermissao(permissaoAlterar));
        return vm is null ? c.NotFound() : c.PartialView("_ContatosAbas", vm);
    }

    public static async Task<IActionResult> RespostaAsync(this Controller c, Task<Resultado<int>> gravar)
    {
        var r = await gravar;
        return r.Sucesso ? c.Json(new { ok = true, id = r.Valor }) : c.UnprocessableEntity(new { erro = r.Mensagem, campo = r.Campo });
    }

    public static async Task<IActionResult> RespostaAsync(this Controller c, Task<Resultado> excluir)
    {
        var r = await excluir;
        return r.Sucesso ? c.Json(new { ok = true }) : c.UnprocessableEntity(new { erro = r.Mensagem });
    }
}
