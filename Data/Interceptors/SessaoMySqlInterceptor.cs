using System.Data.Common;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;

namespace LabControl.Data.Interceptors;

/// <summary>
/// Define as variáveis de sessão lidas pelas triggers/procedures em TODA abertura de conexão
/// (docs/processos/P22-erros-transacoes.md). Como o pool reaproveita conexões, nada de um request
/// anterior sobrevive: retroativo e migração voltam a 0 e o usuário é o do request atual.
/// </summary>
public sealed class SessaoMySqlInterceptor(IEmpresaContexto empresa) : DbConnectionInterceptor
{
    private const string Sql =
        "SET @LC_ID_USUARIOS = @usuario, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0, @LC_DESFAZENDO = NULL";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = Comando(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
                                                     CancellationToken cancellationToken = default)
    {
        await using var cmd = Comando(connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand Comando(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = Sql;
        cmd.Parameters.Add(new MySqlParameter("@usuario", (object?)empresa.UsuarioId ?? DBNull.Value));
        return cmd;
    }
}
