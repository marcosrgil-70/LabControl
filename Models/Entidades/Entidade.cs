using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabControl.Models.Entidades;

[Table("ENTIDADES")]
public class Entidade
{
    [Key]
    [Column("ID_ENTIDADES")]
    public int Id { get; set; }

    [Column("CATEGORIA")]
    [StringLength(1)]
    public string Categoria { get; set; } = "F";

    [Column("DATA_CADASTRO")]
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    [Column("NOME")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Column("INATIVO")]
    public bool Inativo { get; set; } = false;

    [Column("TIPO_CLIENTE")]
    public bool TipoCliente { get; set; } = false;

    [Column("TIPO_FORNECEDOR")]
    public bool TipoFornecedor { get; set; } = false;

    [Column("TIPO_VENDEDOR")]
    public bool TipoVendedor { get; set; } = false;

    [Column("TIPO_FUNCIONARIO")]
    public bool TipoFuncionario { get; set; } = false;

    [Column("TIPO_EMPRESA_USUARIA")]
    public bool TipoEmpresaUsuaria { get; set; } = false;

    public EntidadePF? PessoaFisica { get; set; }
    public EntidadePJ? PessoaJuridica { get; set; }
    public EntidadeFuncionario? Funcionario { get; set; }
    public ICollection<EntidadeEndereco> Enderecos { get; set; } = [];
    public ICollection<EntidadeFone> Fones { get; set; } = [];
    public ICollection<EntidadeEmail> Emails { get; set; } = [];
}

[Table("ENTIDADES_PF")]
public class EntidadePF
{
    [Key]
    [ForeignKey(nameof(Entidade))]
    [Column("ID_ENTIDADES")]
    public int Id { get; set; }

    [Column("CPF")]
    [StringLength(14)]
    public string? Cpf { get; set; }

    [Column("NOME")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Column("SOBRENOME")]
    [StringLength(100)]
    public string? Sobrenome { get; set; }

    [Column("SEXO")]
    [StringLength(1)]
    public string? Sexo { get; set; }

    [Column("DATA_NASCIMENTO")]
    public DateTime? DataNascimento { get; set; }

    [Column("RG")]
    [StringLength(20)]
    public string? Rg { get; set; }

    [Column("ESTADO_CIVIL")]
    [StringLength(1)]
    public string? EstadoCivil { get; set; }

    public Entidade Entidade { get; set; } = null!;
}

[Table("ENTIDADES_PJ")]
public class EntidadePJ
{
    [Key]
    [ForeignKey(nameof(Entidade))]
    [Column("ID_ENTIDADES")]
    public int Id { get; set; }

    [Column("CNPJ")]
    [StringLength(18)]
    public string? Cnpj { get; set; }

    [Column("NOME_FANTASIA")]
    [StringLength(150)]
    public string? NomeFantasia { get; set; }

    [Column("INSC_ESTADUAL")]
    [StringLength(30)]
    public string? InscricaoEstadual { get; set; }

    [Column("INSC_MUNICIPAL")]
    [StringLength(30)]
    public string? InscricaoMunicipal { get; set; }

    public Entidade Entidade { get; set; } = null!;
}

[Table("ENTIDADES_FUNCIONARIOS")]
public class EntidadeFuncionario
{
    [Key]
    [ForeignKey(nameof(Entidade))]
    [Column("ID_ENTIDADES")]
    public int Id { get; set; }

    [Column("ID_TIPOS_REG_PROFISSIONAL")]
    public int? IdTipoRegProfissional { get; set; }

    [Column("NR_REGISTRO_PROFISSIONAL")]
    [StringLength(30)]
    public string? NrRegistroProfissional { get; set; }

    public Entidade Entidade { get; set; } = null!;

    [ForeignKey(nameof(IdTipoRegProfissional))]
    public TipoRegProfissional? TipoRegProfissional { get; set; }
}

[Table("TIPOS_REG_PROFISSIONAL")]
public class TipoRegProfissional
{
    [Key]
    [Column("ID_TIPOS_REG_PROFISSIONAL")]
    public int Id { get; set; }

    [Column("DESCRICAO_REG_PROFISSIONAL")]
    [StringLength(50)]
    public string Descricao { get; set; } = string.Empty;
}

[Table("ENTIDADES_ENDERECOS")]
public class EntidadeEndereco
{
    [Key]
    [Column("ID_ENTIDADES_ENDERECOS")]
    public int Id { get; set; }

    [Column("ID_ENTIDADES")]
    public int IdEntidade { get; set; }

    [Column("ID_ENDERECOS_TIPOS")]
    public int? IdEnderecoTipo { get; set; }

    [Column("LOGRADOURO")]
    [StringLength(150)]
    public string? Logradouro { get; set; }

    [Column("NUMERO")]
    [StringLength(10)]
    public string? Numero { get; set; }

    [Column("COMPLEMENTO")]
    [StringLength(60)]
    public string? Complemento { get; set; }

    [Column("BAIRRO")]
    [StringLength(80)]
    public string? Bairro { get; set; }

    [Column("CIDADE")]
    [StringLength(80)]
    public string? Cidade { get; set; }

    [Column("UF")]
    [StringLength(2)]
    public string? Uf { get; set; }

    [Column("CEP")]
    [StringLength(9)]
    public string? Cep { get; set; }

    [ForeignKey(nameof(IdEntidade))]
    public Entidade Entidade { get; set; } = null!;

    [ForeignKey(nameof(IdEnderecoTipo))]
    public EnderecoTipo? EnderecoTipo { get; set; }
}

[Table("ENDERECOS_TIPOS")]
public class EnderecoTipo
{
    [Key]
    [Column("ID_ENDERECOS_TIPOS")]
    public int Id { get; set; }

    [Column("DESCRICAO")]
    [StringLength(50)]
    public string Descricao { get; set; } = string.Empty;
}

[Table("ENTIDADES_FONES")]
public class EntidadeFone
{
    [Key]
    [Column("ID_ENTIDADES_FONES")]
    public int Id { get; set; }

    [Column("ID_ENTIDADES")]
    public int IdEntidade { get; set; }

    [Column("ID_FONES_TIPOS")]
    public int? IdFoneTipo { get; set; }

    [Column("DDD")]
    [StringLength(3)]
    public string? Ddd { get; set; }

    [Column("FONE")]
    [StringLength(15)]
    public string? Fone { get; set; }

    [ForeignKey(nameof(IdEntidade))]
    public Entidade Entidade { get; set; } = null!;

    [ForeignKey(nameof(IdFoneTipo))]
    public FoneTipo? FoneTipo { get; set; }
}

[Table("FONES_TIPOS")]
public class FoneTipo
{
    [Key]
    [Column("ID_FONES_TIPOS")]
    public int Id { get; set; }

    [Column("DESCRICAO")]
    [StringLength(50)]
    public string Descricao { get; set; } = string.Empty;
}

[Table("ENTIDADES_EMAILS")]
public class EntidadeEmail
{
    [Key]
    [Column("ID_ENTIDADES_EMAILS")]
    public int Id { get; set; }

    [Column("ID_ENTIDADES")]
    public int IdEntidade { get; set; }

    [Column("PRINCIPAL")]
    public bool Principal { get; set; } = false;

    [Column("EMAIL")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [ForeignKey(nameof(IdEntidade))]
    public Entidade Entidade { get; set; } = null!;
}
