using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models.Entidades;

[Table("PAISES")]
public class Pais
{
    [Key] [Column("ID_PAISES")] public int Id { get; set; }
    [Column("NOME")] [StringLength(50)] public string Descricao { get; set; } = string.Empty;
    [Column("SIGLA")] [StringLength(3)] public string? Sigla { get; set; }
}

[Table("ESTADOS")]
public class Estado
{
    [Key] [Column("ID_ESTADOS")] public int Id { get; set; }
    [Column("NOME")] [StringLength(50)] public string Descricao { get; set; } = string.Empty;
    [Column("UF")] [StringLength(2)] public string? Sigla { get; set; }
    [Column("ID_PAISES")] public int? IdPais { get; set; }
}

[Table("CIDADES")]
public class Cidade
{
    [Key] [Column("ID_CIDADES")] public int Id { get; set; }
    [Column("NOME")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("ID_ESTADOS")] public int? IdEstado { get; set; }
}

[Table("BAIRROS")]
public class Bairro
{
    [Key] [Column("ID_BAIRROS")] public int Id { get; set; }
    [Column("NOME")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("ID_CIDADES")] public int? IdCidade { get; set; }
}

[Table("TIPOS_LOGRADOUROS")]
public class TipoLogradouro
{
    [Key] [Column("ID_TIPOS_LOGRADOUROS")] public int Id { get; set; }
    [Column("DESCRICAO")] [StringLength(30)] public string Descricao { get; set; } = string.Empty;
}

[Table("LOGRADOUROS")]
public class Logradouro
{
    [Key] [Column("ID_LOGRADOUROS")] public int Id { get; set; }
    [Column("NOME")] [StringLength(80)] public string Descricao { get; set; } = string.Empty;
    [Column("ID_TIPOS_LOGRADOUROS")] public int? IdTipoLogradouro { get; set; }
    [Column("ID_BAIRROS")] public int? IdBairro { get; set; }

    [ForeignKey(nameof(IdTipoLogradouro))]
    public TipoLogradouro? TipoLogradouro { get; set; }
}
