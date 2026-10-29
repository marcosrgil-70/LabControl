using System.Runtime.CompilerServices;

namespace LabControl.Tests;

/// <summary>O que o Program.cs faz na inicialização e os testes de serviço (sem subir o app) também precisam.</summary>
internal static class Inicializacao
{
    [ModuleInitializer]
    internal static void Configurar() => LabControl.Infra.Pdf.Configurar();   // licença do QuestPDF (boletim, proposta, relatórios)
}
