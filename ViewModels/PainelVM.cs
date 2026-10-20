namespace LabControl.ViewModels;

/// <summary>Contador do painel (T01). <see cref="Href"/> só quando a tela de destino já existe.</summary>
public sealed record ContadorPainel(string Titulo, string Icone, string Cor, int Valor, string? Detalhe, string? Href);

public sealed class PainelVM
{
    public string EmpresaNome { get; init; } = "";
    public IReadOnlyList<ContadorPainel> Contadores { get; init; } = [];
}
