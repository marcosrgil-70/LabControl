using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LabControl.Data.Interceptors;

/// <summary>
/// Toda transação da aplicação em READ COMMITTED (ver <see cref="SessaoMySqlInterceptor"/>).
/// Necessário porque o MySqlConnector, em <c>BeginTransaction()</c> sem nível, envia
/// <c>SET TRANSACTION ISOLATION LEVEL REPEATABLE READ</c> e anularia o isolamento da sessão.
/// Um nível pedido explicitamente pelo código é respeitado.
/// </summary>
public sealed class IsolamentoInterceptor : DbTransactionInterceptor
{
    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result) =>
        eventData.IsolationLevel == IsolationLevel.Unspecified
            ? InterceptionResult<DbTransaction>.SuppressWithResult(connection.BeginTransaction(IsolationLevel.ReadCommitted))
            : result;

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default) =>
        eventData.IsolationLevel == IsolationLevel.Unspecified
            ? InterceptionResult<DbTransaction>.SuppressWithResult(await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
            : result;
}
