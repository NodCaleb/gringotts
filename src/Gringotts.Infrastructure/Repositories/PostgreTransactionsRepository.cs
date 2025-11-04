using System.Data;
using Dapper;
using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Contracts.DTO;

namespace Gringotts.Infrastructure.Repositories;

internal class PostgreTransactionsRepository : ITransactionsRepository
{
    private const string TableName = "transactions";

    public async Task<TransactionInfo?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT t.id as Id,
             t.date as Date,
             t.amount as Amount,
             t.description as Description,
             t.senderid as SenderId,
             COALESCE(s.charactername, s.personalname, s.username) as SenderName,
             t.recipientid as RecipientId,
             COALESCE(r.charactername, r.personalname, r.username) as RecipientName,
             t.employeeid as EmployeeId,
             e.username as EmployeeName
            FROM {TableName} t
            LEFT JOIN customers s ON s.id = t.senderid
            INNER JOIN customers r ON r.id = t.recipientid
            LEFT JOIN employees e ON e.id = t.employeeid
            WHERE t.id = @Id";

        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: dbTransaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<TransactionInfo>(cmd);
    }

    public async Task<IReadOnlyList<TransactionInfo>> GetAllAsync(IDbConnection connection, IDbTransaction dbTransaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT t.id as Id,
             t.date as Date,
             t.amount as Amount,
             t.description as Description,
             t.senderid as SenderId,
             COALESCE(s.charactername, s.personalname, s.username) as SenderName,
             t.recipientid as RecipientId,
             COALESCE(r.charactername, r.personalname, r.username) as RecipientName,
             t.employeeid as EmployeeId,
             e.username as EmployeeName
            FROM {TableName} t
            LEFT JOIN customers s ON s.id = t.senderid
            INNER JOIN customers r ON r.id = t.recipientid
            LEFT JOIN employees e ON e.id = t.employeeid
            ORDER BY t.date";

        var cmd = new CommandDefinition(sql, transaction: dbTransaction, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<TransactionInfo>(cmd);
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

    public async Task<IReadOnlyList<TransactionInfo>> GetByCustomerAsync(long customerId, IDbConnection connection, IDbTransaction dbTransaction, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        // Join customers (sender and recipient) and employees to produce TransactionInfo
        var sql = $@"
            SELECT t.id as Id,
             t.date as Date,
             t.amount as Amount,
             t.description as Description,
             t.senderid as SenderId,
             COALESCE(s.charactername, s.personalname, s.username) as SenderName,
             t.recipientid as RecipientId,
             COALESCE(r.charactername, r.personalname, r.username) as RecipientName,
             t.employeeid as EmployeeId,
             e.username as EmployeeName
            FROM {TableName} t
            LEFT JOIN customers s ON s.id = t.senderid
            INNER JOIN customers r ON r.id = t.recipientid
            LEFT JOIN employees e ON e.id = t.employeeid
            WHERE t.senderid = @CustomerId OR t.recipientid = @CustomerId
            ORDER BY t.date DESC
            ";

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            sql += " LIMIT @PageSize OFFSET @Offset";
        }

        var cmd = new CommandDefinition(sql, new
        {
            CustomerId = customerId,
            PageSize = pageSize,
            Offset = (pageNumber.HasValue && pageSize.HasValue) ? (pageNumber.Value - 1) * pageSize.Value : 0
        }, transaction: dbTransaction, cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TransactionInfo>(cmd);
        return rows.AsList();
    }
}
