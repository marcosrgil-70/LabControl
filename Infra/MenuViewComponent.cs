using Microsoft.AspNetCore.Mvc;

namespace LabControl.Infra;

/// <summary>Menu lateral do _Layout: <see cref="Menu.Visivel"/> para o usuário do request.</summary>
public sealed class MenuViewComponent(IEmpresaContexto usuario, TelasExistentes telas) : ViewComponent
{
    public IViewComponentResult Invoke() => View(Menu.Visivel(usuario, telas.Existe));
}
