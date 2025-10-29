using System.Data;

namespace Gringotts.Infrastructure.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    Task CommitAsync();
    Task RollbackAsync();
}