using System.Data;

namespace Gringotts.Infrastructure.Contracts;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    Task CommitAsync();
    Task RollbackAsync();
}