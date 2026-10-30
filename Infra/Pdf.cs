using QuestPDF.Infrastructure;

namespace LabControl.Infra;

/// <summary>Configuração e apoio comuns aos PDFs (QuestPDF): boletim, proposta/pedido e relatórios.</summary>
public static class Pdf
{
    /// <summary>
    /// Licença Community (dúvida 10, 27/10/2026): gratuita só para faturamento anual &lt; US$ 1 mi. Chamada uma vez na
    /// inicialização (Program.cs e, nos testes, o ModuleInitializer).
    /// </summary>
    public static void Configurar()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        RegistrarCalibri();
    }

    /// <summary>Fonte do boletim (Calibri, como o legado): registrada a partir das fontes do Windows quando existem; sem ela (null), o boletim fica com a padrão do QuestPDF.</summary>
    public static string? FonteBoletim { get; private set; }

    private static void RegistrarCalibri()
    {
        var pasta = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        foreach (var arquivo in new[] { "calibri.ttf", "calibrib.ttf", "calibrii.ttf", "calibriz.ttf" }.Select(a => Path.Combine(pasta, a)).Where(File.Exists))
        {
            using var fonte = File.OpenRead(arquivo);
            QuestPDF.Drawing.FontManager.RegisterFontFromStream(fonte);
            FonteBoletim = "Calibri";
        }
    }

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
