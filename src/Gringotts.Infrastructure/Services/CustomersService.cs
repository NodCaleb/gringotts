using Gringotts.Infrastructure.Contracts;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;

namespace Gringotts.Infrastructure.Services;

internal class CustomersService : ICustomersService
{
    private readonly ICustomersRepository _customersRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public CustomersService(ICustomersRepository customersRepository, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
        _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
    }

    public async Task<CustomerResult> GetCustomerById(long id)
    {
        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var customer = await _customersRepository.GetByIdAsync(id, uow.Connection, uow.Transaction);
            if (customer == null)
            {
                return new CustomerResult
                {
                    Success = false,
                    ErrorCode = ErrorCode.CustomerNotFound,
                    ErrorMessage = { "Customer not found." }
                };
            }

            return new CustomerResult
            {
                Success = true,
                Customer = customer
            };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = { ex.Message }
            };
        }
    }

    public async Task<CustomerResult> CreateCustomer(Customer customer)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));

        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var added = await _customersRepository.AddAsync(customer, uow.Connection, uow.Transaction);
            await uow.CommitAsync();

            return new CustomerResult
            {
                Success = true,
                Customer = added
            };
        }
        catch (ArgumentException aex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.ValidationError,
                ErrorMessage = { aex.Message }
            };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = { ex.Message }
            };
        }
    }

    public async Task<CustomerResult> UpdateCustomer(long id, Customer customer)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));

        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var existing = await _customersRepository.GetByIdAsync(id, uow.Connection, uow.Transaction);
            if (existing == null)
            {
                return new CustomerResult
                {
                    Success = false,
                    ErrorCode = ErrorCode.CustomerNotFound,
                    ErrorMessage = { "Customer not found." }
                };
            }

            // Apply updates from provided customer onto existing entity (preserve Id)
            existing.UserName = customer.UserName;
            existing.PersonalName = customer.PersonalName;
            existing.CharacterName = customer.CharacterName;
            existing.Balance = customer.Balance;

            await _customersRepository.UpdateAsync(existing, uow.Connection, uow.Transaction);
            await uow.CommitAsync();

            return new CustomerResult
            {
                Success = true,
                Customer = existing
            };
        }
        catch (ArgumentException aex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.ValidationError,
                ErrorMessage = { aex.Message }
            };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = { ex.Message }
            };
        }
    }

    public async Task<CustomerResult> UpdateCharacterName(long id, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var existing = await _customersRepository.GetByIdAsync(id, uow.Connection, uow.Transaction);
            if (existing == null)
            {
                return new CustomerResult
                {
                    Success = false,
                    ErrorCode = ErrorCode.CustomerNotFound,
                    ErrorMessage = { "Customer not found." }
                };
            }

            existing.CharacterName = name;

            await _customersRepository.UpdateAsync(existing, uow.Connection, uow.Transaction);
            await uow.CommitAsync();

            return new CustomerResult
            {
                Success = true,
                Customer = existing
            };
        }
        catch (ArgumentException aex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.ValidationError,
                ErrorMessage = { aex.Message }
            };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new CustomerResult
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = { ex.Message }
            };
        }
    }
}
