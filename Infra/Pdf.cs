using QuestPDF.Infrastructure;

namespace LabControl.Infra;

/// <summary>Configuração e apoio comuns aos PDFs (QuestPDF): boletim, proposta/pedido e relatórios.</summary>
public static class Pdf
{
    /// <summary>
    /// Licença Community (dúvida 10, 27/10/2026): gratuita só para faturamento anual &lt; US$ 1 mi. Chamada uma vez na
    /// inicialização (Program.cs e, nos testes, o ModuleInitializer).
    /// </summary>
    public static void Configurar() => QuestPDF.Settings.License = LicenseType.Community;

    /// <summary>Imagem que o QuestPDF não lê não pode derrubar a impressão: sai sem ela (<paramref name="aoFalhar"/> registra).</summary>
    public static byte[]? ImagemValida(byte[]? imagem, Action<Exception>? aoFalhar = null)
    {
        if (imagem is null) return null;
        try { Image.FromBinaryData(imagem).Dispose(); return imagem; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            aoFalhar?.Invoke(ex);
            return null;
        }
    }
}
