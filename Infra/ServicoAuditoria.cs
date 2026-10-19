using System.Text.Json;
using LabControl.Data;
using LabControl.Models;

namespace LabControl.Infra;

/// <summary>
/// Eventos de auditoria que não são um INSERT/UPDATE/DELETE do EF (docs/processos/P21-auditoria-log.md):
/// login (L), revisão (R), emissão (M), desfazer revisão (D) e procedures. As alterações feitas pelo EF
/// nas tabelas críticas já são auditadas pelo <see cref="LabDbContext"/>.
/// </summary>
public interface IAuditoria
{
    /// <param name="acao">Um de <see cref="AcoesAuditoria"/>.</param>
    /// <param name="antes">Objeto serializado em JSON (null = sem dados). Nunca passe senha ou blob.</param>
    Task RegistrarAsync(string acao, string tabela, string idRegistro, object? antes = null, object? depois = null);
}

public sealed class ServicoAuditoria(LabDbContext db, IEmpresaContexto empresa) : IAuditoria
{
    public async Task RegistrarAsync(string acao, string tabela, string idRegistro, object? antes = null, object? depois = null)
    {
        db.Set<Auditoria>().Add(new Auditoria
        {
            IdEmpresas = empresa.EmpresaId, IdUsuarios = empresa.UsuarioId, Ip = empresa.Ip,
            Tabela = tabela, IdRegistro = idRegistro, Acao = acao,
            DadosAntes = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosDepois = depois is null ? null : JsonSerializer.Serialize(depois),
        });
        await db.SaveChangesAsync();
    }
}
