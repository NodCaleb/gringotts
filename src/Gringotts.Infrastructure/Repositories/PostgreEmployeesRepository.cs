using System.Data;
using Dapper;
using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Contracts;

namespace Gringotts.Infrastructure.Repositories;

internal class PostgreEmployeesRepository : IEmployeesRepository
{
    private const string TableName = "employees";

    public async Task<Employee?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT *
            FROM {TableName}
            WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Employee>(cmd);
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"SELECT *
            FROM {TableName}
            ORDER BY id";

        var cmd = new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<Employee>(cmd);
        return result.AsList();
    }

    public async Task<Employee> AddAsync(Employee employee, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"INSERT INTO {TableName} (username, accesscode)
            VALUES (@UserName, @AccessCode)
            RETURNING Id";

        var cmd = new CommandDefinition(sql, new
        {
            employee.UserName,
            employee.AccessCode
        }, transaction: transaction, cancellationToken: cancellationToken);

        var id = await connection.ExecuteScalarAsync<Guid>(cmd);
        employee.Id = id;
        return employee;
    }

    public async Task UpdateAsync(Employee employee, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"UPDATE {TableName}
            SET username = @UserName,
             accesscode = @AccessCode
            WHERE id = @Id";

        var cmd = new CommandDefinition(sql, new
        {
            employee.UserName,
            employee.AccessCode,
            employee.Id
        }, transaction: transaction, cancellationToken: cancellationToken);

        await connection.ExecuteAsync(cmd);
    }

    public async Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $"DELETE FROM {TableName} WHERE id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(cmd);
    }
}
