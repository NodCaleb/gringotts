using System;
using Gringotts.Infrastructure.Contracts;
using Npgsql;

namespace Gringotts.Infrastructure.UnitOfWork;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public UnitOfWorkFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public IUnitOfWork Create()
    {
        return new DapperUnitOfWork(_dataSource);
    }
}
