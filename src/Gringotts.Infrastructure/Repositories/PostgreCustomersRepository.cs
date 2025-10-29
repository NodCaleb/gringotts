using System.Data;
using Dapper;
using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Interfaces;

namespace Gringotts.Infrastructure.Repositories;

internal class PostgreCustomersRepository : ICustomersRepository
{
    private const string TableName = "customers";

    public async Task<Customer?> GetByIdAsync(long id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT *
                    FROM {TableName}
                    WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Customer>(cmd);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT *
                    FROM {TableName}
                    ORDER BY id";

        var cmd = new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Customer>(cmd);
        return result.AsList();
    }

    public async Task<Customer> AddAsync(Customer customer, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"INSERT INTO {TableName} (username, personalname, charactername, balance)
                    VALUES (@UserName, @PersonalName, @CharacterName, @Balance)
                    RETURNING id";

        var cmd = new CommandDefinition(sql, new
        {
            customer.UserName,
            customer.PersonalName,
            customer.CharacterName,
            customer.Balance
        }, transaction: transaction, cancellationToken: cancellationToken);

        var id = await connection.ExecuteScalarAsync<long>(cmd);
        customer.Id = id;
        return customer;
    }

    public async Task UpdateAsync(Customer customer, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"UPDATE {TableName}
                    SET username = @UserName,
                        personalname = @PersonalName,
                        charactername = @CharacterName,
                        balance = @Balance
                    WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new
        {
            customer.UserName,
            customer.PersonalName,
            customer.CharacterName,
            customer.Balance,
            customer.Id
        }, transaction: transaction, cancellationToken: cancellationToken);

        await connection.ExecuteAsync(cmd);
    }

    public async Task DeleteAsync(long id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM {TableName} WHERE id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }

    public async Task<Customer?> GetByNameAsync(string userName, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT *
                    FROM {TableName}
                    WHERE username = @Username";

        var cmd = new CommandDefinition(sql, new { Username = userName }, transaction: transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Customer>(cmd);
    }
}
