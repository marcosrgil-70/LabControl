namespace LabControl.ViewModels;

/// <summary>Parcelas de uma condição de pagamento (T10-11): grade opcional, Σ % = 100.</summary>
public sealed class ParcelasCondicaoVM
{
    public int Id { get; set; }
    /// <summary>Linhas na ordem da grade (o nº da parcela é a posição). Texto, para aceitar "33,3333".</summary>
    public List<ParcelaLinhaVM> Linhas { get; set; } = [];

    // exibição (preenchidos pelo serviço)
    public string Codigo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? Erro { get; set; }
}

public sealed class ParcelaLinhaVM
{
    public string? Dias { get; set; }
    public string? Porcentagem { get; set; }
}
