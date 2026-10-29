using System.ComponentModel.DataAnnotations;
using LabControl.Infra;
using LabControl.Models;

namespace LabControl.ViewModels;

/// <summary>Situação procurada na T24: arquivada, descartada ou ainda sem localização.</summary>
public enum SituacaoLocalizacao { Arquivada, Descartada, SemLocalizacao }

/// <summary>Consulta da T24: basta um filtro (o legado exigia cliente e produto).</summary>
public sealed class LocalizacaoFiltroVM
{
    public const int TamanhoPagina = 25;
    /// <summary>Nome, fantasia ou CPF/CNPJ do cliente.</summary>
    public string? Cliente { get; set; }
    /// <summary>Código ou descrição do produto.</summary>
    public string? Produto { get; set; }
    /// <summary>Parte do nº da amostra ("1382", "04-01382-01/26").</summary>
    public string? Numero { get; set; }
    public SituacaoLocalizacao? Situacao { get; set; }
    /// <summary>Data do arquivo de/até.</summary>
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }
    public string? Armario { get; set; }
    public string? Prateleira { get; set; }
    public string? Caixa { get; set; }
    public int Pagina { get; set; } = 1;

    public bool Informado => !string.IsNullOrWhiteSpace(Cliente) || !string.IsNullOrWhiteSpace(Produto) || !string.IsNullOrWhiteSpace(Numero)
        || Situacao is not null || De is not null || Ate is not null
        || !string.IsNullOrWhiteSpace(Armario) || !string.IsNullOrWhiteSpace(Prateleira) || !string.IsNullOrWhiteSpace(Caixa);
}

public sealed record LocalizacaoItemVM(int Id, string NrAmostra, string Cliente, string? CpfCnpj, string Produto, SituacaoArquivo? Situacao,
    DateTime? DtHrArquivo, string? Armario, string? Prateleira, string? Caixa, DateTime? DtHrDescarte, string? ProfissionalDescarte);

public sealed class LocalizacaoListaVM
{
    public required LocalizacaoFiltroVM Filtro { get; init; }
    public required IReadOnlyList<LocalizacaoItemVM> Itens { get; init; }
    public int Total { get; init; }
    public int Paginas => Paginacao.Paginas(Total, LocalizacaoFiltroVM.TamanhoPagina);
    /// <summary>Dados do "Arquivar em lote" (data padrão = agora).</summary>
    public required LocalizacaoLoteVM Lote { get; init; }
}

/// <summary>Arquivar em lote (novo): as amostras marcadas, na mesma caixa e com a mesma data.</summary>
public sealed class LocalizacaoLoteVM
{
    public List<int> Amostras { get; set; } = [];
    [Display(Name = "Data/hora do arquivo")] public DateTime? DtHrArquivo { get; set; }
    [Display(Name = "Armário"), StringLength(15)] public string? Armario { get; set; }
    [Display(Name = "Prateleira"), StringLength(15)] public string? Prateleira { get; set; }
    [Display(Name = "Caixa"), StringLength(15)] public string? Caixa { get; set; }
    [Display(Name = "Observação"), StringLength(250)] public string? Observacao { get; set; }
}

/// <summary>Resultado do lote: quantas foram arquivadas e as que já tinham localização (não são sobrescritas).</summary>
public sealed record LoteArquivado(int Arquivadas, IReadOnlyList<string> JaLocalizadas);

/// <summary>Localização gravada; <c>Aviso</c> = o que o usuário precisa saber (ex.: status da amostra que continua reprovado).</summary>
public sealed record LocalizacaoGravada(int Id, string? Aviso);

/// <summary>Localização de uma amostra (T24): grupos Arquivo e Descarte. <c>Id</c> = 0 → ainda sem localização.</summary>
public sealed class LocalizacaoVM
{
    public int Id { get; set; }
    /// <summary>Amostra aberta (qualquer revisão); a localização é sempre da amostra física.</summary>
    public int IdAmostra { get; set; }
    [Display(Name = "Situação")] public SituacaoArquivo Status { get; set; } = SituacaoArquivo.Arquivada;
    [Display(Name = "Data/hora do arquivo")] public DateTime? DtHrArquivo { get; set; }
    [Display(Name = "Armário"), StringLength(15, ErrorMessage = "Armário: até 15 caracteres.")] public string? Armario { get; set; }
    [Display(Name = "Prateleira"), StringLength(15, ErrorMessage = "Prateleira: até 15 caracteres.")] public string? Prateleira { get; set; }
    [Display(Name = "Caixa"), StringLength(15, ErrorMessage = "Caixa: até 15 caracteres.")] public string? Caixa { get; set; }
    [Display(Name = "Observação"), StringLength(250, ErrorMessage = "Observação: até 250 caracteres.")] public string? Observacao { get; set; }
    [Display(Name = "Data/hora do descarte")] public DateTime? DtHrDescarte { get; set; }
    /// <summary>No descarte: mudar o status da amostra para "REPROVADA - DESCARTADA".</summary>
    [Display(Name = "Mudar o status da amostra para REPROVADA - DESCARTADA")] public bool MarcarReprovada { get; set; }

    // só leitura
    public string NrAmostra { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public string Produto { get; set; } = string.Empty;
    public string StatusAmostra { get; set; } = string.Empty;
    public bool UltimaRevisao { get; set; }
    public string? ProfissionalDescarte { get; set; }
    /// <summary>Status "REPROVADA - DESCARTADA" existe na empresa (a pergunta só aparece se existir).</summary>
    public bool TemStatusReprovada { get; set; }
    /// <summary>A última revisão tem boletim emitido: o status não muda por aqui (o banco recusa; gere revisão da amostra).</summary>
}
