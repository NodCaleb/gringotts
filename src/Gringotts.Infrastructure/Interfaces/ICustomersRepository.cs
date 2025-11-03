using System.Data;
using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Interfaces;

public interface ICustomersRepository
{
    Task<Customer?> GetByIdAsync(long id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<Customer> AddAsync(Customer customer, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<Customer?> GetByNameAsync(string userName, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchCustomer(string substring, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken = default);
}
