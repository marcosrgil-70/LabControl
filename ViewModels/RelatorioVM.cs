using LabControl.Models;
using LabControl.Services.Relatorios;

namespace LabControl.ViewModels;

/// <summary>
/// Filtros dos relatórios gerenciais (T30-00): um só para os seis; cada relatório usa os que a sua
/// <see cref="DefinicaoRelatorio.Filtros"/> mostra. Tela, PDF e CSV recebem os mesmos (query string).
/// </summary>
public sealed class RelatorioFiltroVM
{
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public string? Cliente { get; set; }
    public string? Cobranca { get; set; }
    public SituacaoPedido? Situacao { get; set; }
    public string? Produto { get; set; }
    public string? Parametro { get; set; }
    public string? TipoAnalise { get; set; }
    public bool? Acreditado { get; set; }
    public string? Analista { get; set; }
    /// <summary>Padrão marcado, como o legado (dúvida 9, decisão do usuário em 29/10/2026).</summary>
    public bool SomenteComPedido { get; set; } = true;
    public bool AgruparPorCliente { get; set; }
    /// <summary>Botão "Visualizar": sem ele a tela abre só com os filtros (período padrão).</summary>
    public bool Visualizar { get; set; }

    public static string NomeSituacao(SituacaoPedido s) => s switch
    {
        SituacaoPedido.Aberto => "Aberto",
        SituacaoPedido.EmExecucao => "Em execução",
        SituacaoPedido.Concluido => "Concluído",
        _ => "Cancelado",
    };
}

public sealed class RelatorioTelaVM
{
    public required DefinicaoRelatorio Definicao { get; init; }
    public required RelatorioFiltroVM Filtro { get; init; }
    public RelatorioDados? Dados { get; init; }
}
