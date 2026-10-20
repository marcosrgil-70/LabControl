using System.ComponentModel.DataAnnotations;
using LabControl.Services;

namespace LabControl.ViewModels;

public sealed class LoginVM
{
    [Required(ErrorMessage = "Informe o usuário."), StringLength(30), Display(Name = "Usuário")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha."), DataType(DataType.Password), Display(Name = "Senha")]
    public string Senha { get; set; } = "";
}

public sealed class TrocarSenhaVM
{
    [Required(ErrorMessage = "Informe a senha atual."), DataType(DataType.Password), Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = "";

    [Required(ErrorMessage = "Informe a nova senha."), DataType(DataType.Password), Display(Name = "Nova senha")]
    public string NovaSenha { get; set; } = "";

    [Required(ErrorMessage = "Confirme a nova senha."), DataType(DataType.Password), Display(Name = "Confirme a nova senha")]
    public string Confirmacao { get; set; } = "";

    /// <summary>Troca obrigatória (1º acesso / senha provisória): sem "Cancelar".</summary>
    public bool Obrigatoria { get; set; }
}

public sealed class EscolherEmpresaVM
{
    public IReadOnlyList<EmpresaDisponivel> Empresas { get; set; } = [];

    [Required(ErrorMessage = "Selecione a empresa."), Display(Name = "Empresa")]
    public int? IdEmpresa { get; set; }

    /// <summary>Empresa ativa atual (troca de empresa), para destacar na lista.</summary>
    public int? EmpresaAtual { get; set; }
}
