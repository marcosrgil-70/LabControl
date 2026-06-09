namespace LabControl.Models;

public class AcaoVM
{
    public string Form { get; set; } = string.Empty;
    public string NomeForm { get; set; } = string.Empty;
    public bool Incluir { get; set; }
    public bool Alterar { get; set; }
    public bool Consultar { get; set; }
    public bool Excluir { get; set; }
    public bool Imprimir { get; set; }
}

public class UsuarioEditVM
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool Inativo { get; set; }
    public string? NovaSenha { get; set; }
    public string? ConfirmarSenha { get; set; }
    public List<AcaoVM> Acoes { get; set; } = [];
}
