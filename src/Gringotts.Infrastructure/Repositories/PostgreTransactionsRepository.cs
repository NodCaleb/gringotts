using System.Data;
using Dapper;
using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Contracts;

namespace Gringotts.Infrastructure.Repositories;

internal class PostgreTransactionsRepository : ITransactionsRepository
{
    private const string TableName = "transactions";

    public async Task<Transaction?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT id, date, senderid AS ""SenderId"", recipientid AS ""RecipientId"", employeeid AS ""EmployeeId"", amount, description
FROM {TableName}
WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: dbTransaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Transaction>(cmd);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT id, date, senderid AS ""SenderId"", recipientid AS ""RecipientId"", employeeid AS ""EmployeeId"", amount, description
FROM {TableName}
ORDER BY date";

        var cmd = new CommandDefinition(sql, transaction: dbTransaction, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Transaction>(cmd);
        return result.AsList();
    }

    public async Task<Transaction> AddAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"INSERT INTO {TableName} (date, senderid, recipientid, employeeid, amount, description)
VALUES (@Date, @SenderId, @RecipientId, @EmployeeId, @Amount, @Description)
RETURNING id";

        var cmd = new CommandDefinition(sql, new
        {
            tx.Date,
            tx.SenderId,
            tx.RecipientId,
            tx.EmployeeId,
            tx.Amount,
            tx.Description
        }, transaction: dbTransaction, cancellationToken: cancellationToken);

        var id = await connection.ExecuteScalarAsync<Guid>(cmd);
        tx.Id = id;
        return tx;
    }

    public async Task UpdateAsync(Transaction tx, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"UPDATE {TableName}
SET date = @Date,
 senderid = @SenderId,
 recipientid = @RecipientId,
 employeeid = @EmployeeId,
 amount = @Amount,
 description = @Description
WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new
        {
            tx.Date,
            tx.SenderId,
            tx.RecipientId,
            tx.EmployeeId,
            tx.Amount,
            tx.Description,
            tx.Id
        }, transaction: dbTransaction, cancellationToken: cancellationToken);

        await connection.ExecuteAsync(cmd);
    }

    public async Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM {TableName} WHERE id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: dbTransaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }
}
