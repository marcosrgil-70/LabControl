using System.ComponentModel.DataAnnotations;
using LabControl.Infra;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LabControl.ViewModels;

/// <summary>Filtros da consulta de amostras (T22 › Consultar).</summary>
public sealed class AmostraFiltroVM
{
    public const int TamanhoPagina = 25;
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>Data de entrega a partir de (padrão: hoje − DIAS_PESQUISA_PADRAO).</summary>
    public DateOnly? APartirDe { get; set; }
    /// <summary>Parte do nº da amostra ("01383", "04-01383-01/26").</summary>
    public string? Numero { get; set; }
    /// <summary>Parte do nº da proposta.</summary>
    public string? Proposta { get; set; }
    /// <summary>Código ou descrição do produto.</summary>
    public string? Produto { get; set; }
    public int? IdStatus { get; set; }
    public bool SoUltima { get; set; } = true;
    public int Pagina { get; set; } = 1;

    // pesquisa de amostras (T29): filtros a mais e os da tela que abre o componente
    public string? Lote { get; set; }
    /// <summary>Data de entrega até.</summary>
    public DateOnly? Ate { get; set; }
    /// <summary>Só status que permite lançar resultado (T26).</summary>
    public bool ApenasPermiteResultado { get; set; }
    /// <summary>Só com resultado lançado (T27).</summary>
    public bool ApenasComResultado { get; set; }
    /// <summary>Só ainda não arquivadas/descartadas (T24).</summary>
    public bool ApenasSemLocalizacao { get; set; }
    /// <summary>T29 ordena pelo número; a T22 pela entrega mais recente.</summary>
    public bool OrdenarPorNumero { get; set; }
}

public sealed record AmostraListaItemVM(int Id, string NrAmostra, bool UltimaRevisao, DateOnly DtEntrega, TimeOnly HrEntrega,
    decimal Qtde, string Embalagem, string? Lote, string? Fabricacao, string? Validade, decimal? Temperatura, string? Aspecto, string? Cor,
    string Status, string? CorStatus, string Cliente, string Produto, string TipoAmostra, string TipoAnalise, string NrProposta, string? NrPedido);

public sealed class AmostraListaVM
{
    public required AmostraFiltroVM Filtro { get; init; }
    public required IReadOnlyList<AmostraListaItemVM> Itens { get; init; }
    public required IReadOnlyList<OpcaoVM> Status { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, AmostraFiltroVM.TamanhoPagina);
}

/// <summary>Amostra em edição (T22). Número/ano/código vêm do banco; tipos de análise restritos aos do item da proposta.</summary>
public sealed class AmostraVM
{
    public int Id { get; set; }

    // só leitura (vindos do banco)
    public string? NrAmostra { get; set; }
    public int Revisao { get; set; }
    public string? MotivoRevisao { get; set; }
    public string? NrPedido { get; set; }
    /// <summary>Por que não pode ser alterada (revisão antiga, boletim emitido); null = editável.</summary>
    public string? Bloqueio { get; set; }
    /// <summary>Com resultado: tipo/proposta/produto travados e testes não são regerados.</summary>
    public bool TemResultado { get; set; }
    public bool UltimaRevisao { get; set; } = true;

    [Display(Name = "Cliente")] public int? IdEntidades { get; set; }
    [Display(Name = "Proposta")] public int? IdLabPropostas { get; set; }
    [Display(Name = "Produto (item da proposta)")] public int? IdLabPropostasProdutos { get; set; }
    [Display(Name = "Tipo da amostra")] public int? IdAmostrasTipo { get; set; }
    [Display(Name = "Tipo de análise")] public int? IdAnalisesTipo { get; set; }
    [Display(Name = "Data da entrega")] public DateOnly? DtEntrega { get; set; }
    [Display(Name = "Hora")] public TimeOnly? HrEntrega { get; set; }
    [Display(Name = "Local de recebimento"), StringLength(50)] public string? LocalRecebimento { get; set; }
    [Display(Name = "Tipo de embalagem")] public int? IdEmbalagensTipos { get; set; }
    /// <summary>Texto, para aceitar "2,5".</summary>
    [Display(Name = "Qtde de embalagens")] public string? QtdeEmbalagens { get; set; }
    [Display(Name = "Lote"), StringLength(30)] public string? NrLote { get; set; }
    public int? FabricacaoDia { get; set; }
    public int? FabricacaoMes { get; set; }
    public int? FabricacaoAno { get; set; }
    public int? ValidadeDia { get; set; }
    public int? ValidadeMes { get; set; }
    public int? ValidadeAno { get; set; }
    [Display(Name = "Nota do rótulo"), StringLength(255)] public string? NotaRotulo { get; set; }
    [Display(Name = "Aspecto"), StringLength(50)] public string? AspectoAmostra { get; set; }
    [Display(Name = "Cor"), StringLength(20)] public string? Cor { get; set; }
    [Display(Name = "Outras características"), StringLength(100)] public string? OutrasCaracteristicas { get; set; }
    [Display(Name = "Observação da verificação"), StringLength(255)] public string? ObservacaoVerificacao { get; set; }
    [Display(Name = "Temperatura (°C)")] public string? Temperatura { get; set; }
    [Display(Name = "Acompanha ficha técnica")] public bool AcompanhaFichaTecnica { get; set; }
    [Display(Name = "Acompanha padrão analítico")] public bool AcompanhaPadraoAnalitico { get; set; }
    [Display(Name = "Acompanha CA do cliente")] public bool AcompanhaCaCliente { get; set; }
    [Display(Name = "Enviar a outro laboratório")] public bool EnviarOutroLaboratorio { get; set; }
    [Display(Name = "Responsável")] public int? IdEntidadesFuncResp { get; set; }
    [Display(Name = "Status")] public int? IdAmostrasStatus { get; set; }
    [Display(Name = "Coletor"), StringLength(60)] public string? NomeColetor { get; set; }
    [Display(Name = "Data e hora da coleta")] public DateTime? DtHrColeta { get; set; }
    /// <summary>Texto rico (HTML simples, ver HtmlSimples).</summary>
    public string? TxObservacao { get; set; }

    /// <summary>O usuário confirmou a data de entrega mais antiga que DIAS_RETROATIVOS_ENTREGA (P04).</summary>
    public bool ConfirmouRetroativa { get; set; }

    [ValidateNever] public AmostraOpcoesVM Opcoes { get; set; } = new();
    [ValidateNever] public IReadOnlyList<AmostraTesteVM> Testes { get; set; } = [];
    [ValidateNever] public IReadOnlyList<AmostraLogVM> Historico { get; set; } = [];
}

/// <summary>Combos e dados da cascata cliente → proposta → item (JSON para o <c>amostras.js</c>).</summary>
public sealed class AmostraOpcoesVM
{
    public IReadOnlyList<OpcaoVM> Clientes { get; set; } = [];
    public IReadOnlyList<OpcaoCodigoVM> TiposAmostra { get; set; } = [];
    public IReadOnlyList<OpcaoCodigoVM> TiposAnalise { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Embalagens { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Responsaveis { get; set; } = [];
    public IReadOnlyList<OpcaoVM> Status { get; set; } = [];
    public AmostraClienteVM? Cliente { get; set; }
    public AmostraPropostaVM? Proposta { get; set; }
    public int DiasRetroativos { get; set; }
    /// <summary>Hoje no servidor do banco (yyyy-MM-dd): o ano do número é o dele.</summary>
    public string Hoje { get; set; } = "";
    public bool PodeRetroativa { get; set; }
}

/// <summary>Opção com o código (TT/AA do número da amostra).</summary>
public sealed record OpcaoCodigoVM(int Id, string Nome, int Codigo);

public sealed record AmostraClienteVM(int Id, string? CpfCnpj, string? Endereco, string? Fone, IReadOnlyList<OpcaoVM> Propostas);

public sealed record AmostraPropostaVM(int Id, string NrProposta, string? NrPedido, IReadOnlyList<AmostraItemVM> Itens);

/// <summary>Item da proposta com os tipos de análise das suas análises (o tipo da amostra fica restrito a eles).</summary>
public sealed record AmostraItemVM(int Id, int Item, string Produto, string TipoDocumento, IReadOnlyList<int> TiposAnalise);

public sealed record AmostraTesteVM(string TipoAnalise, string Parametro, string Metodo, string Idioma, string Prazo, DateOnly? DtPrevista);

/// <summary>Linha do histórico (log); a do legado não tem data nem usuário.</summary>
public sealed record AmostraLogVM(string Acao, DateTime? Quando, bool Legado, string Resumo);

/// <summary>Comprovante de recebimento (página de impressão — decisão do usuário, 4.3).</summary>
public sealed record AmostraComprovanteVM(
    string Empresa, string NrAmostra, string Cliente, string? CpfCnpj, string? Endereco, string NrProposta, string? NrPedido,
    string Produto, string? Lote, string? Fabricacao, string? Validade, string Embalagem, decimal Qtde, DateOnly DtEntrega, TimeOnly HrEntrega,
    string LocalRecebimento, DateTime? DtHrColeta, string? Coletor, string? Responsavel, string Status,
    IReadOnlyList<AmostraTesteVM> Testes, string? Observacao);
