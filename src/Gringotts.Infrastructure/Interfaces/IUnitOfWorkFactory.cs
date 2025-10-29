namespace Gringotts.Infrastructure.Interfaces;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
