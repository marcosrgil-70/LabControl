using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LabControl.ViewModels;

/// <summary>Aba "Parâmetros" da T05 (<c>EMPRESAS_PARAMETROS</c> da empresa ativa).</summary>
public sealed class ParametrosEmpresaVM
{
    [Required(ErrorMessage = "Informe o status inicial da proposta."), Display(Name = "Status inicial da proposta")]
    public int? IdLabPropostasStatusInicial { get; set; }

    [Required(ErrorMessage = "Informe o status \"Enviado ao cliente\"."), Display(Name = "Status \"Enviado ao cliente\"")]
    public int? IdLabPropostasStatusEnvcli { get; set; }

    [Required(ErrorMessage = "Informe o status inicial da amostra."), Display(Name = "Status inicial da amostra")]
    public int? IdAmostrasStatusInicial { get; set; }

    [Display(Name = "Status que permite resultados (legado)")]
    public int? IdAmostrasStatusResultados { get; set; }

    [Required(ErrorMessage = "Informe o status inicial do boletim."), Display(Name = "Status inicial do boletim")]
    public int? IdBoletinsStatusInicial { get; set; }

    [Required(ErrorMessage = "Informe o status do boletim emitido."), Display(Name = "Status do boletim emitido")]
    public int? IdBoletinsStatusEmitido { get; set; }

    [Range(0, 365, ErrorMessage = "Informe de 0 a 365 dias."), Display(Name = "Dias retroativos para a data de entrega")]
    public int DiasRetroativosEntrega { get; set; } = 7;

    [Range(0, 3650, ErrorMessage = "Informe de 0 a 3650 dias."), Display(Name = "Validade padrão da proposta (dias)")]
    public int DiasValidadeProposta { get; set; } = 30;

    [Range(0, 3650, ErrorMessage = "Informe de 0 a 3650 dias."), Display(Name = "Período padrão de pesquisa (dias)")]
    public int DiasPesquisaPadrao { get; set; } = 365;

    [Display(Name = "Exige 2º responsável no boletim")] public bool ExigeResp2Boletim { get; set; }
    [Display(Name = "Legenda padrão do boletim")] public string? TextoPadraoLegenda { get; set; }
    [Display(Name = "Contrato padrão da proposta")] public string? TextoPadraoContrato { get; set; }

    // exibição
    public DateTime? AlteradoEm { get; set; }
    public bool AdminGlobal { get; set; }
    public IReadOnlyList<OpcaoVM> StatusPropostas { get; set; } = [];
    public IReadOnlyList<OpcaoVM> StatusAmostras { get; set; } = [];
    public IReadOnlyList<OpcaoVM> StatusBoletins { get; set; } = [];
    public IReadOnlyList<SequenciaVM> Numeracao { get; set; } = [];
    [ValidateNever] public AjusteNumeracaoVM Ajuste { get; set; } = new(); // formulário próprio (AjustarNumeracao)
    [ValidateNever] public EmailEmpresaVM Email { get; set; } = new();     // aba E-mail, formulário próprio (8.3)
}

/// <summary>Linha da aba "Numeração" (<c>SEQUENCIAS_DOCUMENTOS</c>). <paramref name="Ano"/> 0 = numeração não anual.</summary>
public sealed record SequenciaVM(string Tipo, string Documento, short Ano, int UltimoNumero, DateTime AlteradoEm);

/// <summary>Ajuste manual da numeração (só ADMIN_GLOBAL, com motivo).</summary>
public sealed class AjusteNumeracaoVM
{
    [Required] public string Tipo { get; set; } = "";
    public short Ano { get; set; }

    [Required(ErrorMessage = "Informe o novo valor."), Range(0, int.MaxValue, ErrorMessage = "Informe um número válido."), Display(Name = "Último número")]
    public int? NovoValor { get; set; }

    [Required(ErrorMessage = "Informe o motivo do ajuste."), StringLength(500, MinimumLength = 10, ErrorMessage = "Descreva o motivo (de 10 a 500 caracteres)."), Display(Name = "Motivo")]
    public string Motivo { get; set; } = "";
}
