using System.ComponentModel.DataAnnotations;
using LabControl.Models;

namespace LabControl.ViewModels;

/// <summary>Filtro da consulta de clientes/fornecedores (T12).</summary>
public sealed class ClienteFiltroVM
{
    public const int TamanhoPagina = 20;
    /// <summary>Parte do nome, nome fantasia ou CPF/CNPJ.</summary>
    public string? Texto { get; set; }
    /// <summary>cliente, fornecedor, comercial, transportador (vazio = todos).</summary>
    public string? Tipo { get; set; }
    public bool Inativos { get; set; }
    public int Pagina { get; set; } = 1;
}

public sealed record ClienteListaItemVM(int Id, Categoria Categoria, DateOnly DataCadastro, string Nome, string? NomeFantasia,
    string? CpfCnpj, string? Fone, string? Email, string Tipos, bool Inativo);

public sealed class ClienteListaVM
{
    public required ClienteFiltroVM Filtro { get; init; }
    public required IReadOnlyList<ClienteListaItemVM> Itens { get; init; }
    public int Total { get; init; }
    /// <summary>Inativos que a pesquisa acharia com "mostrar inativos" (aviso na lista).</summary>
    public int InativosOcultos { get; init; }
    public int Paginas => Math.Max(1, (Total + ClienteFiltroVM.TamanhoPagina - 1) / ClienteFiltroVM.TamanhoPagina);
}

/// <summary>Dados básicos + observações do cliente/fornecedor (as abas de contatos são o componente <c>_Contatos</c>).</summary>
public sealed class ClienteVM
{
    public int Id { get; set; }
    [Display(Name = "Data do cadastro")] public DateOnly? DataCadastro { get; set; }
    [Display(Name = "Categoria")] public Categoria Categoria { get; set; } = Categoria.Juridica;

    [Required(ErrorMessage = "Informe o nome / razão social."), StringLength(150), Display(Name = "Nome / Razão social")]
    public string Nome { get; set; } = "";
    /// <summary>PJ: nome fantasia · PF: sobrenome.</summary>
    [StringLength(150)] public string? NomeFantasia { get; set; }
    [StringLength(18), Display(Name = "CPF / CNPJ")] public string? CpfCnpj { get; set; }
    /// <summary>PF: RG · PJ: inscrição estadual.</summary>
    [StringLength(20)] public string? RgIe { get; set; }
    [StringLength(20), Display(Name = "Inscrição municipal")] public string? InscMunicipal { get; set; }
    [StringLength(20), Display(Name = "Inscrição SUFRAMA")] public string? InscSuframa { get; set; }
    [Display(Name = "Sexo")] public Sexo? Sexo { get; set; }
    [Display(Name = "Data de nascimento")] public DateOnly? DtNascimento { get; set; }
    [Display(Name = "Estado civil")] public EstadoCivil? EstadoCivil { get; set; }

    [Display(Name = "Cliente")] public bool TipoCliente { get; set; } = true;
    [Display(Name = "Fornecedor")] public bool TipoFornecedor { get; set; }
    [Display(Name = "Comercial (vendedor)")] public bool TipoVendedor { get; set; }
    [Display(Name = "Transportador")] public bool TipoTransportador { get; set; }
    /// <summary>Só exibição: funcionário é mantido na tela de Profissionais (T13).</summary>
    public bool TipoFuncionario { get; set; }

    [StringLength(20), Display(Name = "Avaliação")] public string? Avaliacao { get; set; }
    [Display(Name = "Observações")] public string? Observacao { get; set; }
    [Display(Name = "Incluir a observação no faturamento")] public bool ObsIncluirFaturamento { get; set; }
    [Display(Name = "Inativo")] public bool Inativo { get; set; }

    /// <summary>Aba aberta ao voltar (dados, enderecos, telefones, emails, observacoes).</summary>
    public string? Aba { get; set; }
}
