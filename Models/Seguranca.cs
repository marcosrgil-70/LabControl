using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models;

[Table("USUARIO")]
public class Usuario
{
    [Key]
    [Column("USUCOD")]
    public int Id { get; set; }

    [Column("USULOG")]
    [StringLength(30)]
    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Column("USUNOM")]
    [StringLength(80)]
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Column("USUSEN")]
    [StringLength(100)]
    public string SenhaHash { get; set; } = string.Empty;

    [Column("USUADM")]
    public bool IsAdmin { get; set; } = false;

    [Column("INATIVO")]
    public bool Inativo { get; set; } = false;

    [Column("ID_ENTIDADES")]
    public int? IdEntidade { get; set; }

    public ICollection<UsuarioPerfil> UsuariosPerfis { get; set; } = [];

    // Mantido para dados já migrados (não usado para novas permissões)
    public ICollection<AcaoUsuario> Acoes { get; set; } = [];
}

[Table("PERMISSOES")]
public class Permissao
{
    [Key]
    [Column("ID_PERMISSOES")]
    public int Id { get; set; }

    [Column("COD_PERMISSAO")]
    [StringLength(30)]
    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Column("DSC_PERMISSAO")]
    [StringLength(100)]
    [Required]
    public string Descricao { get; set; } = string.Empty;

    public ICollection<PerfilPermissao> PerfilPermissoes { get; set; } = [];
}

[Table("PERFIS")]
public class Perfil
{
    [Key]
    [Column("ID_PERFIS")]
    public int Id { get; set; }

    [Column("COD_PERFIL")]
    [StringLength(20)]
    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Column("DSC_PERFIL")]
    [StringLength(80)]
    [Required]
    public string Descricao { get; set; } = string.Empty;

    public ICollection<PerfilPermissao> Permissoes { get; set; } = [];
    public ICollection<UsuarioPerfil> UsuariosPerfis { get; set; } = [];
}

[Table("PERFIS_PERMISSOES")]
public class PerfilPermissao
{
    [Key, Column("ID_PERFIS", Order = 0)]
    public int IdPerfil { get; set; }

    [Key, Column("ID_PERMISSOES", Order = 1)]
    public int IdPermissao { get; set; }

    [Column("INCLUIR")]   public bool Incluir   { get; set; }
    [Column("ALTERAR")]   public bool Alterar   { get; set; }
    [Column("CONSULTAR")] public bool Consultar { get; set; }
    [Column("EXCLUIR")]   public bool Excluir   { get; set; }
    [Column("IMPRIMIR")]  public bool Imprimir  { get; set; }

    public Perfil Perfil { get; set; } = null!;
    public Permissao Permissao { get; set; } = null!;
}

[Table("USUARIOS_PERFIS")]
public class UsuarioPerfil
{
    [Key, Column("USUCOD", Order = 0)]
    public int IdUsuario { get; set; }

    [Key, Column("ID_PERFIS", Order = 1)]
    public int IdPerfil { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Perfil Perfil { get; set; } = null!;
}

// Mantido para compatibilidade com dados migrados do sistema anterior
[Table("ACOES")]
public class AcaoUsuario
{
    [Key, Column("USUCOD", Order = 0)]
    public int IdUsuario { get; set; }

    [Key, Column("FORM", Order = 1)]
    [StringLength(50)]
    public string Form { get; set; } = string.Empty;

    [Column("INCLUIR")]   public bool Incluir   { get; set; }
    [Column("ALTERAR")]   public bool Alterar   { get; set; }
    [Column("CONSULTAR")] public bool Consultar { get; set; }
    [Column("EXCLUIR")]   public bool Excluir   { get; set; }
    [Column("IMPRIMIR")]  public bool Imprimir  { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

[Table("EMPRESAS")]
public class Empresa
{
    [Key]
    [Column("ID_EMPRESAS")]
    public int Id { get; set; }

    [Column("COD_EMPRESAS")]
    [StringLength(10)]
    public string Codigo { get; set; } = string.Empty;

    [Column("ID_ENTIDADES")]
    public int IdEntidade { get; set; }
}
