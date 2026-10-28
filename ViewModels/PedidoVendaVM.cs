using LabControl.Infra;
using LabControl.Models;

namespace LabControl.ViewModels;

/// <summary>Filtros da consulta de pedidos de venda (T21 › Consulta).</summary>
public sealed class PedidoVendaFiltroVM
{
    public const int TamanhoPagina = 20;
    public int? NrPedido { get; set; }
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>"123", "000123" ou "000123/26" (qualquer revisão).</summary>
    public string? Proposta { get; set; }
    public SituacaoPedido? Situacao { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record PedidoVendaListaItemVM(int Id, string NrPedido, DateOnly DtPedido, int IdProposta, string NrProposta, string? CpfCnpj,
    string Cliente, decimal VrTotal, SituacaoPedido Situacao, int Recebidas, int Contratadas);

public sealed class PedidoVendaListaVM
{
    public required PedidoVendaFiltroVM Filtro { get; init; }
    public required IReadOnlyList<PedidoVendaListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, PedidoVendaFiltroVM.TamanhoPagina);
}

public sealed record PedidoVendaItemVM(int Item, string Produto, int Contratada, int Recebida)
{
    public int Saldo => Contratada - Recebida;
}

/// <param name="Boletim">"Emitido em dd/MM/yyyy", "Em montagem" ou "Sem boletim" (boletim vigente da amostra).</param>
public sealed record PedidoVendaAmostraVM(int Id, string NrAmostra, DateOnly DtEntrega, string? Status, string? CorStatus, string Boletim, bool Emitido);

/// <summary>Tela do pedido (T21): cabeçalho só leitura (vem da proposta), situação, itens, amostras e observação.</summary>
public sealed class PedidoVendaVM
{
    public int Id { get; init; }
    public required string NrPedido { get; init; }
    public DateOnly DtPedido { get; init; }
    public SituacaoPedido Situacao { get; init; }
    public int IdProposta { get; init; }
    public required string NrProposta { get; init; }
    /// <summary>Revisão vigente da proposta quando o pedido está numa anterior ("Atualizar para a revisão vigente").</summary>
    public int? IdPropostaVigente { get; init; }
    public string? NrPropostaVigente { get; init; }
    public required string Cliente { get; init; }
    public string? CpfCnpj { get; init; }
    public string? Cobranca { get; init; }
    public string? CondicaoPagamento { get; init; }
    public string? Moeda { get; init; }
    public decimal VrTotal { get; init; }
    public DateOnly DtAutorizacao { get; init; }
    public string? AutorizadoPor { get; init; }
    public string? NrPedidoCliente { get; init; }
    public DateTime? DtCancelamento { get; init; }
    public string? MotivoCancelamento { get; init; }
    public string? Observacao { get; set; }
    public IReadOnlyList<PedidoVendaItemVM> Itens { get; init; } = [];
    public IReadOnlyList<PedidoVendaAmostraVM> Amostras { get; init; } = [];

    public bool Cancelado => Situacao == SituacaoPedido.Cancelado;
    public bool PodeConcluir => Situacao is SituacaoPedido.Aberto or SituacaoPedido.EmExecucao;
    public bool PodeCancelar => Situacao != SituacaoPedido.Cancelado && Amostras.Count == 0;
    public bool PodeAtualizar => IdPropostaVigente is not null && Situacao is SituacaoPedido.Aberto or SituacaoPedido.EmExecucao;
}

/// <summary>Modal "Gerar pedido" (T20 e T21): proposta vigente sem pedido + dados da autorização do cliente.</summary>
public sealed class PedidoVendaGerarVM
{
    public int IdProposta { get; set; }
    public string? NrProposta { get; set; }
    public string? Cliente { get; set; }
    public DateOnly? DtAutorizacao { get; set; }
    public string? AutorizadoPor { get; set; }
    public string? NrPedidoCliente { get; set; }
}

public static class SituacoesPedido
{
    public static string Nome(SituacaoPedido s) => s switch
    {
        SituacaoPedido.Aberto => "Aberto",
        SituacaoPedido.EmExecucao => "Em execução",
        SituacaoPedido.Concluido => "Concluído",
        _ => "Cancelado",
    };

    public static string Classe(SituacaoPedido s) => s switch
    {
        SituacaoPedido.Aberto => "text-bg-secondary",
        SituacaoPedido.EmExecucao => "text-bg-primary",
        SituacaoPedido.Concluido => "text-bg-success",
        _ => "text-bg-danger",
    };
}
