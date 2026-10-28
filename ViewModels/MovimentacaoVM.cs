using System.ComponentModel.DataAnnotations;
using LabControl.Infra;
using LabControl.Models;

namespace LabControl.ViewModels;

/// <summary>Busca da T23: cliente, proposta ou número (pelo menos um, RN-06.07); só a última revisão.</summary>
public sealed class MovimentacaoFiltroVM
{
    public const int TamanhoPagina = 25;
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    public string? Proposta { get; set; }
    /// <summary>Parte do nº da amostra ("1382", "04-01382-01/26").</summary>
    public string? Numero { get; set; }
    public int Pagina { get; set; } = 1;

    public bool Informado => !string.IsNullOrWhiteSpace(Cliente) || !string.IsNullOrWhiteSpace(Proposta) || !string.IsNullOrWhiteSpace(Numero);
}

public sealed record MovimentacaoAmostraItemVM(int Id, string NrAmostra, DateOnly DtEntrega, string Cliente, string Produto, string NrProposta,
    string? Lote, string? Fabricacao, string? Validade, string TipoAmostra, string TipoAnalise, string Status, string? CorStatus, decimal Saldo);

public sealed class MovimentacaoListaVM
{
    public required MovimentacaoFiltroVM Filtro { get; init; }
    public required IReadOnlyList<MovimentacaoAmostraItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, MovimentacaoFiltroVM.TamanhoPagina);
}

/// <summary>Teste (parâmetro) da amostra oferecido na saída; <c>JaAtendido</c> = indicado em movimento anterior.</summary>
public sealed record MovimentacaoParametroVM(int IdTeste, string Parametro, string TipoAnalise, bool JaAtendido);

public sealed record MovimentoVM(int Id, DateTime DataMov, EntradaSaida Tipo, decimal Qtde, string? Justificativa, string Profissional,
    bool Automatica, IReadOnlyList<string> Parametros);

/// <summary>Painel da amostra (T23): cabeçalho, saldo da amostra física, histórico e testes para a saída.</summary>
public sealed class MovimentacaoAmostraVM
{
    public int Id { get; init; }
    public required string NrAmostra { get; init; }
    public required string Cliente { get; init; }
    public required string Produto { get; init; }
    public string? Lote { get; init; }
    public string? Fabricacao { get; init; }
    public string? Validade { get; init; }
    public required string Embalagem { get; init; }
    public decimal Saldo { get; init; }
    /// <summary>Revisão antiga aberta pelo link: os movimentos são da amostra física, mas a tela avisa.</summary>
    public bool UltimaRevisao { get; init; }
    public required IReadOnlyList<MovimentoVM> Movimentos { get; init; }
    public required IReadOnlyList<MovimentacaoParametroVM> Parametros { get; init; }
    public required MovimentoNovoVM Novo { get; init; }
}

/// <summary>Nova movimentação (modal): entrada complementar ou saída.</summary>
public sealed class MovimentoNovoVM
{
    public int IdAmostra { get; set; }
    [Display(Name = "Data/hora")]
    public DateTime? DataMov { get; set; }
    [Display(Name = "Tipo")]
    public EntradaSaida Tipo { get; set; } = EntradaSaida.Saida;
    /// <summary>Texto em pt-BR ("1,5"), lido por <c>Textos.Decimal</c> como no recebimento.</summary>
    [Display(Name = "Quantidade")]
    public string? Qtde { get; set; }
    [Display(Name = "Justificativa"), StringLength(250, ErrorMessage = "Justificativa: até 250 caracteres.")]
    public string? Justificativa { get; set; }
    /// <summary>Testes atendidos pela saída.</summary>
    public List<int> Testes { get; set; } = [];
}
