namespace LabControl.Models;

public class PerfilSelecaoVM
{
    public int IdPerfil { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Selecionado { get; set; }
}

public class UsuarioEditVM
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool Inativo { get; set; }
    public string? NovaSenha { get; set; }
    public string? ConfirmarSenha { get; set; }
    public List<PerfilSelecaoVM> Perfis { get; set; } = [];
}
