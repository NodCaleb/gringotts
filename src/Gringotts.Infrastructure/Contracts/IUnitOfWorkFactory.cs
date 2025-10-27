namespace Gringotts.Infrastructure.Contracts;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
