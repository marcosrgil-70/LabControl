using System.ComponentModel.DataAnnotations;

namespace LabControl.ViewModels;

public sealed record PerfilListaItemVM(int Id, string Nome, string? Descricao, bool Ativo, int Permissoes, int Usuarios, bool Administrador);

/// <summary>Uma permissão do catálogo (<c>PERMISSOES</c>) numa célula da matriz.</summary>
public sealed record PermissaoCelula(int Id, string Codigo, string Acao, string Descricao);

/// <summary>Linha da matriz: uma tela, com as ações padrão e as especiais.</summary>
public sealed record TelaMatriz(string Modulo, string Tela, string Nome, IReadOnlyDictionary<string, PermissaoCelula> Padrao, IReadOnlyList<PermissaoCelula> Especiais);

public sealed class PerfilVM
{
    public static readonly string[] AcoesPadrao = ["CONSULTAR", "INCLUIR", "ALTERAR", "EXCLUIR", "IMPRIMIR"];

    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome."), StringLength(60), Display(Name = "Nome")]
    public string Nome { get; set; } = "";

    [StringLength(200), Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Display(Name = "Ativo")] public bool Ativo { get; set; } = true;

    /// <summary>IDs de <c>PERMISSOES</c> marcados.</summary>
    public List<int> Permissoes { get; set; } = [];

    // exibição
    public bool Administrador { get; set; }
    public IReadOnlyList<TelaMatriz> Matriz { get; set; } = [];
}

public sealed record PerfilOrigemVM(int Id, string Nome, string Empresa, bool DaEmpresaAtiva);

public sealed class CopiarPerfilVM
{
    [Required(ErrorMessage = "Selecione o perfil de origem."), Display(Name = "Copiar de")]
    public int? IdOrigem { get; set; }

    [Required(ErrorMessage = "Informe o nome do novo perfil."), StringLength(60), Display(Name = "Nome do novo perfil")]
    public string Nome { get; set; } = "";

    public IReadOnlyList<PerfilOrigemVM> Origens { get; set; } = [];
}
