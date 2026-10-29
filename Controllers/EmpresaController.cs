using LabControl.Infra;
using LabControl.Services;
using LabControl.Services.Email;
using LabControl.Services.Entidades;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace LabControl.Controllers;

/// <summary>T03 — dados da empresa ativa (<c>/Empresa/Dados</c>), logo e contatos (endereços, telefones e e-mails da entidade da empresa). A lista e a inclusão ficam em <see cref="EmpresasController"/>.</summary>
public sealed class EmpresaController(EmpresaService empresas, ParametrosEmpresaService parametros, ConfiguracaoEmailService email, ContatosService contatos,
    ContaService conta, IEmpresaContexto ctx) : Controller
{
    private int Ativa => ctx.EmpresaId ?? throw new InvalidOperationException("Sem empresa ativa.");

    [HttpGet, Permissao("EMPRESAS.CONSULTAR")]
    public async Task<IActionResult> Dados()
    {
        if (await empresas.CarregarAsync(Ativa) is not { } vm) return NotFound();
        await ContatosNaTelaAsync();
        return View(vm);
    }

    [HttpPost, Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> Dados(EmpresaVM vm)
    {
        vm.Id = Ativa;
        ModelState.Remove(nameof(vm.CodEmpresa)); // o código não muda (não vem no formulário)
        if (ModelState.IsValid && this.AplicarNoFormulario(await empresas.AlterarAsync(vm), "Dados da empresa usuária gravados."))
        {
            await AtualizarTopoAsync();
            return RedirectToAction(nameof(Dados));
        }
        await empresas.PrepararAsync(vm);
        vm.CodEmpresa = (await empresas.CarregarAsync(Ativa))?.CodEmpresa ?? "";
        await ContatosNaTelaAsync();
        return View(vm);
    }

    /// <summary>T05 — parâmetros da empresa ativa (aba "Parâmetros"), numeração somente leitura (aba "Numeração") e e-mail (aba "E-mail").</summary>
    [HttpGet, Permissao("PARAMETROS_EMPRESA.CONSULTAR")]
    public async Task<IActionResult> Parametros()
    {
        if (await parametros.CarregarAsync() is not { } vm) return NotFound();
        vm.Email = (await email.CarregarAsync())!;
        return View(vm);
    }

    [HttpPost, Permissao("PARAMETROS_EMPRESA.ALTERAR")]
    public async Task<IActionResult> Parametros(ParametrosEmpresaVM vm)
    {
        if (ModelState.IsValid && this.AplicarNoFormulario(await parametros.AlterarAsync(vm), "Parâmetros da empresa gravados."))
            return RedirectToAction(nameof(Parametros));
        await parametros.PrepararAsync(vm);
        vm.Email = (await email.CarregarAsync())!;
        return View(vm);
    }

    /// <summary>T05 — aba E-mail (8.3): servidor de saída e modelos do e-mail do boletim. Recusa: a aba volta com o digitado.</summary>
    [HttpPost, Permissao("PARAMETROS_EMPRESA.ALTERAR")]
    public async Task<IActionResult> Email([Bind(Prefix = nameof(ParametrosEmpresaVM.Email))] EmailEmpresaVM dados)
    {
        var r = await email.GravarAsync(dados);
        if (r.Sucesso)
        {
            TempData[Mensagens.Sucesso] = "Configuração de e-mail gravada.";
            return Redirect(Url.Action(nameof(Parametros)) + "#email");
        }
        ModelState.AddModelError(r.Campo is null ? nameof(ParametrosEmpresaVM.Email) : $"{nameof(ParametrosEmpresaVM.Email)}.{r.Campo}", r.Mensagem!);
        var vm = (await parametros.CarregarAsync())!;
        var gravado = (await email.CarregarAsync())!;
        (dados.TemSenha, dados.Configurado, dados.NovaSenha) = (gravado.TemSenha, gravado.Configurado, null);
        vm.Email = dados;
        ViewData["Aba"] = "email";
        return View(nameof(Parametros), vm);
    }

    /// <summary>"Enviar e-mail de teste" com a configuração gravada.</summary>
    [HttpPost, Permissao("PARAMETROS_EMPRESA.ALTERAR")]
    public async Task<IActionResult> TestarEmail(string? destino)
    {
        this.AplicarNoTempData(await email.TestarAsync(destino), $"E-mail de teste enviado para {destino?.Trim()}. Confira a caixa de entrada (e o spam).");
        return Redirect(Url.Action(nameof(Parametros)) + "#email");
    }

    /// <summary>T05 — ajuste manual de uma linha da numeração, com motivo (só ADMIN_GLOBAL).</summary>
    [HttpPost, SomenteAdminGlobal]
    public async Task<IActionResult> AjustarNumeracao([Bind(Prefix = nameof(ParametrosEmpresaVM.Ajuste))] AjusteNumeracaoVM ajuste)
    {
        Resultado r = ModelState.IsValid
            ? await parametros.AjustarNumeracaoAsync(ajuste)
            : Resultado.Erro(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage);
        this.AplicarNoTempData(r, $"Numeração ajustada: {ParametrosEmpresaService.Documentos.GetValueOrDefault(ajuste.Tipo, ajuste.Tipo)} → {ajuste.NovoValor}.");
        return Redirect(Url.Action(nameof(Parametros)) + "#numeracao");
    }

    /// <summary>Logo (tela e PDFs): da empresa ativa ou, para ADMIN_GLOBAL, de qualquer uma.</summary>
    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Logo(int? id) =>
        await empresas.LogoAsync(id ?? Ativa) is { } logo ? File(logo.Dados, logo.Mime) : NotFound();

    // ---- contatos da empresa ativa (endereços, telefones e e-mails da entidade da empresa — P25)

    private const string RotaContatos = "/Empresa";

    [HttpGet("Empresa/Contatos"), Permissao("EMPRESAS.CONSULTAR")]
    public async Task<IActionResult> Contatos() =>
        await this.AbasAsync(contatos, CadastroDono.Empresa, await EntidadeAsync(), RotaContatos, "EMPRESAS.ALTERAR", ctx);

    [HttpPost("Empresa/ContatoEndereco"), Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> ContatoEndereco(EnderecoVM vm) =>
        await this.RespostaAsync(contatos.GravarEnderecoAsync(CadastroDono.Empresa, await EntidadeAsync(), vm));

    [HttpPost("Empresa/ContatoFone"), Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> ContatoFone(FoneVM vm) =>
        await this.RespostaAsync(contatos.GravarFoneAsync(CadastroDono.Empresa, await EntidadeAsync(), vm));

    [HttpPost("Empresa/ContatoEmail"), Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> ContatoEmail(EmailVM vm) =>
        await this.RespostaAsync(contatos.GravarEmailAsync(CadastroDono.Empresa, await EntidadeAsync(), vm));

    [HttpPost("Empresa/ContatoExcluir"), Permissao("EMPRESAS.ALTERAR")]
    public async Task<IActionResult> ContatoExcluir(TipoContato tipo, int idContato) =>
        await this.RespostaAsync(contatos.ExcluirAsync(CadastroDono.Empresa, await EntidadeAsync(), tipo, idContato));

    /// <summary>Abas de contatos renderizadas junto com a página (sem segundo request).</summary>
    private async Task ContatosNaTelaAsync() =>
        ViewData["Contatos"] = await contatos.CarregarAsync(CadastroDono.Empresa, await EntidadeAsync(), RotaContatos, ctx.TemPermissao("EMPRESAS.ALTERAR"));

    private async Task<int> EntidadeAsync() => await empresas.EntidadeDaEmpresaAsync(Ativa) ?? 0;

    // nome/código da empresa no topo vêm das claims: regrava a identidade depois de alterar
    private async Task AtualizarTopoAsync()
    {
        if (ctx.UsuarioId is not { } usuario) return;
        var r = await conta.MontarIdentidadeAsync(usuario, Ativa);
        if (r.Sucesso) await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, r.Valor);
    }
}
