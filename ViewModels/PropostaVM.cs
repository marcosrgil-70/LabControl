using System.ComponentModel.DataAnnotations;
using LabControl.Infra;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LabControl.ViewModels;

/// <summary>Filtros da consulta de propostas (T20 › Consultar).</summary>
public sealed class PropostaFiltroVM
{
    public const int TamanhoPagina = 20;
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>"123", "000123" ou "000123/26".</summary>
    public string? Numero { get; set; }
    public int? NrPedido { get; set; }
    public int? IdStatus { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    /// <summary>Padrão marcado: só a revisão vigente de cada proposta.</summary>
    public bool SoUltima { get; set; } = true;
    public int Pagina { get; set; } = 1;
}

public sealed record PropostaListaItemVM(int Id, string NrProposta, bool UltimaRevisao, DateOnly DtSolicitacao, string Status, string? CorStatus,
    string? CpfCnpj, string Cliente, string? NrPedido, decimal VrTotal);

public sealed class PropostaListaVM
{
    public required PropostaFiltroVM Filtro { get; init; }
    public required IReadOnlyList<PropostaListaItemVM> Itens { get; init; }
    public required IReadOnlyList<OpcaoVM> Status { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, PropostaFiltroVM.TamanhoPagina);
}

/// <summary>
/// Proposta em edição (T20, parte essencial da 4.2): cabeçalho + textos nos campos do formulário; produtos e análises
/// num JSON (<see cref="ItensJson"/>) montado pelo <c>propostas.js</c> — tudo grava junto, numa transação, como no Delphi.
/// </summary>
public sealed class PropostaVM
{
    public int Id { get; set; }

    // só leitura (vindos do banco)
    public string? NrProposta { get; set; }
    public int? IdPedido { get; set; }
    public string? NrPedido { get; set; }
    public DateOnly? DtEnvioCliente { get; set; }
    /// <summary>Por que a proposta não pode ser alterada aqui (revisão antiga, enviada, com pedido ou com amostra); null = editável.</summary>
    public string? Bloqueio { get; set; }
    public int Revisao { get; set; }
    public string? MotivoRevisao { get; set; }
    public bool UltimaRevisao { get; set; } = true;
    /// <summary>Todas as revisões da proposta (painel "Revisões", 6.1), da mais nova para a mais antiga.</summary>
    [ValidateNever] public IReadOnlyList<PropostaRevisaoVM> Revisoes { get; set; } = [];
    /// <summary>A empresa tem contrato padrão (botão "Usar contrato padrão").</summary>
    public bool TemContratoPadrao { get; set; }

    [Display(Name = "Data da solicitação")]
    public DateOnly? DtSolicitacao { get; set; }
    [Display(Name = "Validade")]
    public DateOnly? DtValidade { get; set; }
    [Display(Name = "Status")]
    public int? IdLabPropostasStatus { get; set; }
    [Display(Name = "Data de autorização")]
    public DateOnly? DtAutorizacao { get; set; }

    [Display(Name = "Comercial")]
    public int? IdEntidadesComerc { get; set; }
    [Display(Name = "Cliente")]
    public int? IdEntidades { get; set; }
    [Display(Name = "Contato")]
    public int? IdEntContato { get; set; }

    [Display(Name = "Endereço do resultado")]
    public int? IdEntEndLaudo { get; set; }
    [Display(Name = "Contato do resultado")]
    public int? IdEntContatoResultado { get; set; }
    [Display(Name = "Empresa de cobrança")]
    public int? IdEntCobranca { get; set; }
    [Display(Name = "Endereço da cobrança")]
    public int? IdEntEndCobranca { get; set; }
    [Display(Name = "Contato da NF")]
    public int? IdEntContatoNf { get; set; }

    /// <summary>Texto, para aceitar "10,5".</summary>
    [Display(Name = "% desconto geral")]
    public string? PorcDesconto { get; set; }
    [Display(Name = "Desconto geral (R$)")]
    public string? VrDesconto { get; set; }
    [Display(Name = "Condição de pagamento")]
    public int? IdCondicoesPagtos { get; set; }
    [Display(Name = "Moeda")]
    public int? IdMoedas { get; set; }

    // totais do banco (exibidos; a tela recalcula enquanto se digita)
    public decimal VrSomaVrAnalis { get; set; }
    public decimal VrSomaDescAnalis { get; set; }
    public decimal VrSomaVrTotAnalis { get; set; }
    public decimal VrTotalProposta { get; set; }

    // textos ricos (HTML simples, ver HtmlSimples)
    public string? TxItensSolicitados { get; set; }
    public string? TxItensAdquiridos { get; set; }
    public string? TxObservacao { get; set; }
    /// <summary>Aba Contrato (6.1).</summary>
    public string? TxContrato { get; set; }

    /// <summary>Produtos e análises (<see cref="PropostaItemVM"/>[]) em JSON.</summary>
    public string? ItensJson { get; set; }

    // combos e listas da página (preenchidos pelo serviço)
    [ValidateNever] public PropostaOpcoesVM Opcoes { get; set; } = new();
}

/// <summary>Produto da proposta (<c>LAB_PROPOSTAS_PRODUTOS</c>) com as suas análises.</summary>
public sealed class PropostaItemVM
{
    /// <summary>0 = novo.</summary>
    public int Id { get; set; }
    public int Item { get; set; }
    public int IdProdutos { get; set; }
    public int QtdeAmostras { get; set; }
    public List<PropostaAnaliseVM> Analises { get; set; } = [];
}

/// <summary>Linha de análise (<c>LAB_PROPOSTAS_PRODANALISES</c>). Subtotal/total: o banco calcula (P06).</summary>
public sealed class PropostaAnaliseVM
{
    public int Id { get; set; }
    public int IdParametro { get; set; }
    public int IdMetodo { get; set; }
    public int IdIdioma { get; set; }
    public int IdPrazo { get; set; }
    /// <summary>BOLETIM ou RELATORIO.</summary>
    public string TipoDocumento { get; set; } = "BOLETIM";
    public decimal VrUnitario { get; set; }
    public decimal PorcDesconto { get; set; }
    public decimal VrDesconto { get; set; }
    public bool StRevAnterior { get; set; }
}

/// <summary>Uma revisão da proposta no painel "Revisões".</summary>
public sealed record PropostaRevisaoVM(int Id, string NrProposta, int Revisao, string Status, DateOnly? DtValidade, decimal VrTotal, string? Motivo, bool Vigente);

/// <summary>Opção de combo com dados extras para a tela (JSON).</summary>
public sealed record OpcaoExtraVM(int Id, string Nome, IReadOnlyDictionary<string, object?> Extra);

/// <summary>Combos da proposta: os da empresa ativa (ativos + os já escolhidos).</summary>
public sealed class PropostaOpcoesVM
{
    public IReadOnlyList<OpcaoVM> Status { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Clientes { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Comerciais { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Condicoes { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Moedas { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Unidades { get; set; } = [];
    /// <summary>Produtos: Extra = unidade.</summary>
    public IReadOnlyList<OpcaoExtraVM> Produtos { get; set; } = [];
    /// <summary>Parâmetros: Extra = tipo de análise, valor unitário, método padrão.</summary>
    public IReadOnlyList<OpcaoExtraVM> Parametros { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Metodos { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Idiomas { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Prazos { get; set; } = [];
    /// <summary>Contatos/endereços do cliente e da cobrança já escolhidos (para abrir a página preenchida).</summary>
    public PropostaEntidadeVM? Cliente { get; set; }
    public PropostaEntidadeVM? Cobranca { get; set; }
}

/// <summary>Contatos (e-mails) e endereços de uma entidade, com as sugestões da T20 (JSON para o <c>propostas.js</c>).</summary>
public sealed record PropostaEntidadeVM(
    int Id, string? CpfCnpj, string? Fone,
    IReadOnlyList<PropostaContatoVM> Contatos, IReadOnlyList<PropostaEnderecoVM> Enderecos,
    int? SugestaoEndLaudo, int? SugestaoEndCobranca, int? SugestaoContatoResultado, int? SugestaoContatoNf);

public sealed record PropostaContatoVM(int Id, string Texto, string Email, string? Departamento);

public sealed record PropostaEnderecoVM(int Id, string Texto, string Detalhe);
