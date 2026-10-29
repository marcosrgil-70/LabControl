using System.Text.Json;
using LabControl.Data;
using LabControl.Models;

namespace LabControl.Infra;

/// <summary>
/// Eventos de auditoria que não são um INSERT/UPDATE/DELETE do EF nem de procedure (docs/processos/P21-auditoria-log.md):
/// login (L), reset de senha, ajuste de numeração… As alterações feitas pelo EF nas tabelas críticas já são auditadas
/// pelo <see cref="LabDbContext"/>, e as procedures (revisão, emissão, pedido…) gravam o próprio evento (PR_AUDITAR).
/// </summary>
public interface IAuditoria
{
    /// <param name="acao">Um de <see cref="AcoesAuditoria"/>.</param>
    /// <param name="antes">Objeto serializado em JSON (null = sem dados). Nunca passe senha ou blob.</param>
    /// <param name="idUsuario">Usuário do evento quando ainda não há sessão (login); sem ele, o da sessão.</param>
    Task RegistrarAsync(string acao, string tabela, string idRegistro, object? antes = null, object? depois = null, int? idUsuario = null);
}

/// <summary>Leitura dos JSON <c>DADOS_ANTES</c>/<c>DADOS_DEPOIS</c> (T06, histórico de envios do boletim).</summary>
public static class DadosAuditoria
{
    /// <summary>Campo → valor em texto (objetos e listas aninhados ficam como JSON; JSON inválido vira "(dados)").</summary>
    public static Dictionary<string, string?> Campos(string? json)
    {
        var r = new Dictionary<string, string?>();
        if (string.IsNullOrWhiteSpace(json)) return r;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return new() { ["(dados)"] = json };
            foreach (var p in doc.RootElement.EnumerateObject())
                r[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => p.Value.GetString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => p.Value.GetRawText(),
                };
        }
        catch (JsonException) { r["(dados)"] = json; }
        return r;
    }
}

/// <summary>
/// Grava por INSERT direto (não usa o ChangeTracker: nada que o serviço tenha pendente vai junto). Dentro de uma
/// transação aberta pelo serviço, o evento fica na mesma transação.
/// </summary>
public sealed class ServicoAuditoria(LabDbContext db, IEmpresaContexto empresa) : IAuditoria
{
    public Task RegistrarAsync(string acao, string tabela, string idRegistro, object? antes = null, object? depois = null, int? idUsuario = null) =>
        AuditoriaAlteracoes.InserirAsync(db.Database, [new Auditoria
        {
            IdEmpresas = empresa.EmpresaId, IdUsuarios = idUsuario ?? empresa.UsuarioId, Ip = empresa.Ip,
            Tabela = tabela, IdRegistro = idRegistro, Acao = acao,
            DadosAntes = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosDepois = depois is null ? null : JsonSerializer.Serialize(depois),
        }]);
}
