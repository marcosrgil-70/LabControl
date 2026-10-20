using System.Reflection;

namespace LabControl.Infra;

/// <summary>Versão do sistema exibida no menu (substitui o "Sobre o Sistema" do Delphi, <c>un_About</c>).</summary>
public static class Versao
{
    // <Version> do LabControl.csproj; o SDK acrescenta "+<commit>" na InformationalVersion
    public static string Atual { get; } =
        "v" + (typeof(Versao).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?");
}
