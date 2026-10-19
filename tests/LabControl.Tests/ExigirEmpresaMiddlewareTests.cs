using System.Security.Claims;
using LabControl.Infra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace LabControl.Tests;

public class ExigirEmpresaMiddlewareTests
{
    private static async Task<(bool Passou, string? Destino)> ExecutarAsync(bool autenticado, int? empresa, string? controller)
    {
        var ctx = new DefaultHttpContext();
        if (autenticado) ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(LabClaims.UsuarioId, "1")], "teste"));
        if (controller is not null)
            ctx.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new ControllerActionDescriptor { ControllerName = controller }), controller));
        var passou = false;
        var mw = new ExigirEmpresaMiddleware(_ => { passou = true; return Task.CompletedTask; });
        await mw.InvokeAsync(ctx, new EmpresaFixa { EmpresaId = empresa });
        return (passou, ctx.Response.Headers.Location.FirstOrDefault());
    }

    [Fact]
    public async Task Autenticado_sem_empresa_vai_para_escolha_de_empresa()
    {
        var r = await ExecutarAsync(autenticado: true, empresa: null, controller: "Home");
        Assert.False(r.Passou);
        Assert.Equal(ExigirEmpresaMiddleware.RotaEscolherEmpresa, r.Destino);
    }

    [Theory]
    [InlineData(true, 1, "Home")]      // com empresa
    [InlineData(true, null, "Conta")]  // login / escolha de empresa
    [InlineData(true, null, null)]     // arquivo estático (sem action MVC)
    [InlineData(false, null, "Home")]  // anônimo: a autenticação decide
    public async Task Demais_casos_seguem(bool autenticado, int? empresa, string? controller)
    {
        var r = await ExecutarAsync(autenticado, empresa, controller);
        Assert.True(r.Passou);
    }
}
