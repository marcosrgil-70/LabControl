using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models;

[Table("USUARIO")]
public class Usuario
{
    [Key]
    [Column("USUCOD")]
    public int Id { get; set; }

    [Column("USUNOM")]
    [StringLength(80)]
    [Required]
    public string Nome { get; set; } = string.Empty;

    [Column("USUSEN")]
    [StringLength(100)]
    public string SenhaHash { get; set; } = string.Empty;

    // 0=Normal, 1=Admin
    [Column("USUADM")]
    public bool IsAdmin { get; set; } = false;

    [Column("INATIVO")]
    public bool Inativo { get; set; } = false;

    [Column("ID_ENTIDADES")]
    public int? IdEntidade { get; set; }

    public ICollection<AcaoUsuario> Acoes { get; set; } = [];
}

[Table("ACOES")]
public class AcaoUsuario
{
    [Key, Column("USUCOD", Order = 0)]
    public int IdUsuario { get; set; }

    [Key, Column("FORM", Order = 1)]
    [StringLength(50)]
    public string Form { get; set; } = string.Empty;

    [Column("INCLUIR")] public bool Incluir { get; set; }
    [Column("ALTERAR")] public bool Alterar { get; set; }
    [Column("CONSULTAR")] public bool Consultar { get; set; }
    [Column("EXCLUIR")] public bool Excluir { get; set; }
    [Column("IMPRIMIR")] public bool Imprimir { get; set; }

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
