using System.Data;
using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetAllAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<Employee> AddAsync(Employee employee, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
}
