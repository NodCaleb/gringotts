using Gringotts.Infrastructure.Interfaces;

namespace Gringotts.Infrastructure.UnitOfWork;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly string _connectionString;

    public UnitOfWorkFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IUnitOfWork Create()
    {
        return new DapperUnitOfWork(_connectionString);
    }
}
