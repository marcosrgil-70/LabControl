using System.ComponentModel.DataAnnotations;
using LabControl.Services;

namespace LabControl.ViewModels;

public sealed class UsuarioFiltroVM
{
    public string? Texto { get; set; }
    /// <summary>null = todos; true = ativos; false = inativos.</summary>
    public bool? Ativos { get; set; } = true;
}

public sealed record UsuarioListaItemVM(int Id, string Login, string Nome, string? Email, bool Ativo, bool AdminGlobal,
                                        bool Bloqueado, DateTime? UltimoAcesso, string Empresas);

public sealed class UsuarioListaVM
{
    public UsuarioFiltroVM Filtro { get; init; } = new();
    public IReadOnlyList<UsuarioListaItemVM> Itens { get; init; } = [];
}

/// <summary>Formulário da T02 (dados do usuário + grid de empresas/perfis/profissional).</summary>
public sealed class UsuarioVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o login."), StringLength(30, ErrorMessage = "Login com no máximo 30 caracteres."),
     RegularExpression(@"^\S+$", ErrorMessage = "O login não pode ter espaços."), Display(Name = "Login")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Informe o nome."), StringLength(100), Display(Name = "Nome")]
    public string Nome { get; set; } = "";

    [EmailAddress(ErrorMessage = "E-mail inválido."), StringLength(150), Display(Name = "E-mail")]
    public string? Email { get; set; }

    [Display(Name = "Ativo")] public bool Ativo { get; set; } = true;
    [Display(Name = "Administrador global")] public bool AdminGlobal { get; set; }

    /// <summary>Empresa padrão (pré-seleção no login): uma por usuário.</summary>
    [Display(Name = "Empresa padrão")] public int? EmpresaPadrao { get; set; }

    public List<UsuarioEmpresaVM> Empresas { get; set; } = [];

    // somente leitura (exibição)
    public bool TrocarSenha { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public DateTime? UltimoAcesso { get; set; }
    /// <summary>Quem edita pode alterar os dados globais (nome, ativo, senha…)? Ver UsuarioService.</summary>
    public bool PodeEditarDados { get; set; } = true;
    public bool PodeMarcarAdminGlobal { get; set; }
}

public sealed class UsuarioEmpresaVM
{
    public int IdEmpresa { get; set; }
    /// <summary>Linha marcada = o usuário tem (ou passa a ter) acesso à empresa.</summary>
    public bool Acesso { get; set; }
    public bool Ativo { get; set; } = true;
    public int? IdProfissional { get; set; }
    public List<int> Perfis { get; set; } = [];

    // exibição
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public IReadOnlyList<ProfissionalDisponivel> Profissionais { get; set; } = [];
    public IReadOnlyList<(int Id, string Nome)> PerfisDisponiveis { get; set; } = [];
}

/// <summary>Senha provisória mostrada uma vez (inclusão e reset), sem passar por TempData/cookie.</summary>
public sealed record SenhaProvisoriaVM(int IdUsuario, string Login, string Senha, bool Inclusao);
