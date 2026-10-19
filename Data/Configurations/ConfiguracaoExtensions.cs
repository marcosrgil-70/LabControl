using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabControl.Data.Configurations;

/// <summary>
/// Colunas preenchidas pelo banco (triggers/defaults). Usadas pelas configurações geradas.
/// </summary>
public static class ConfiguracaoExtensions
{
    /// <summary>
    /// Sempre calculada pelo banco (ex.: NR_AMOSTRA, RESULTADO_FORMATADO, ALTERADO_EM):
    /// o EF nunca envia e relê o valor após INSERT e UPDATE.
    /// </summary>
    public static PropertyBuilder<T> CalculadaPeloBanco<T>(this PropertyBuilder<T> p)
    {
        p.ValueGeneratedOnAddOrUpdate();
        p.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        p.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        return p;
    }

    /// <summary>
    /// Preenchida pelo banco na inclusão quando vier vazia (ex.: COD_AMOSTRA = 0, CODIGO do produto vazio):
    /// o EF omite a coluna no INSERT quando o valor é o padrão do tipo e relê o valor gerado.
    /// <paramref name="alteravel"/> = false: o EF não envia a coluna em UPDATE (número/ano do documento).
    /// <paramref name="enviar"/> = false: nunca é enviada no INSERT (ex.: CRIADO_EM).
    /// </summary>
    public static PropertyBuilder<T> PreenchidaPeloBancoNaInclusao<T>(this PropertyBuilder<T> p, bool alteravel = true, bool enviar = true)
    {
        p.ValueGeneratedOnAdd();
        if (!enviar) p.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        p.Metadata.SetAfterSaveBehavior(alteravel ? PropertySaveBehavior.Save : PropertySaveBehavior.Ignore);
        return p;
    }
}
