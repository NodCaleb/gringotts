using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Results;
using Gringotts.Contracts.DTO;

namespace Gringotts.Infrastructure.Services;

internal class TransactionsService : ITransactionsService
{
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly ICustomersRepository _customersRepository;
    private readonly IEmployeesRepository _employeesRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public TransactionsService(
        ITransactionsRepository transactionsRepository,
        ICustomersRepository customersRepository,
        IEmployeesRepository employeesRepository,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _transactionsRepository = transactionsRepository ?? throw new ArgumentNullException(nameof(transactionsRepository));
        _customersRepository = customersRepository ?? throw new ArgumentNullException(nameof(customersRepository));
        _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
    }

    public async Task<TransactionResult> CreateTransactionAsync(TransactionRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        // basic validations
        if ((request.RecipientId <= 0) && string.IsNullOrWhiteSpace(request.RecipientUsername))
        {
            return new TransactionResult { Success = false, ErrorCode = ErrorCode.ValidationError, ErrorMessage = { "Either RecipientId or RecipientUsername is required." } };
        }

        if (request.SenderId == null && request.EmployeeId == null)
        {
            return new TransactionResult { Success = false, ErrorCode = ErrorCode.ValidationError, ErrorMessage = { "Either SenderId or EmployeeId must be provided." } };
        }

        if (request.Amount <= 0)
        {
            return new TransactionResult { Success = false, ErrorCode = ErrorCode.ValidationError, ErrorMessage = { "Amount must be positive." } };
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return new TransactionResult { Success = false, ErrorCode = ErrorCode.ValidationError, ErrorMessage = { "Description is required." } };
        }

        using var uow = _unitOfWorkFactory.Create();
        try
        {
            // load recipient by id or username
            Customer? recipient = null;
            if (request.RecipientId > 0)
            {
                recipient = await _customersRepository.GetByIdAsync(request.RecipientId, uow.Connection, uow.Transaction);
            }
            else
            {
                recipient = await _customersRepository.GetByNameAsync(request.RecipientUsername!, uow.Connection, uow.Transaction);
            }

            if (recipient == null)
            {
                return new TransactionResult { Success = false, ErrorCode = ErrorCode.CustomerNotFound, ErrorMessage = { "Recipient not found." } };
            }

            Customer? sender = null;
            if (request.SenderId != null)
            {
                sender = await _customersRepository.GetByIdAsync(request.SenderId.Value, uow.Connection, uow.Transaction);
                if (sender == null)
                {
                    return new TransactionResult { Success = false, ErrorCode = ErrorCode.CustomerNotFound, ErrorMessage = { "Sender not found." } };
                }

                // ensure sender has sufficient funds
                if (sender.Balance < request.Amount)
                {
                    return new TransactionResult { Success = false, ErrorCode = ErrorCode.InsufficientFunds, ErrorMessage = { "Sender has insufficient funds." } };
                }
            }

            // if employee provided, validate existence
            if (request.EmployeeId != null)
            {
                var employee = await _employeesRepository.GetByIdAsync(request.EmployeeId.Value, uow.Connection, uow.Transaction);
                if (employee == null)
                {
                    return new TransactionResult { Success = false, ErrorCode = ErrorCode.EmployeeNotFound, ErrorMessage = { "Employee not found." } };
                }
            }

            // prepare transaction entity
            var tx = new Transaction
            {
                Date = DateTime.UtcNow,
                SenderId = request.SenderId,
                RecipientId = recipient.Id,
                EmployeeId = request.EmployeeId,
                Amount = request.Amount,
                Description = request.Description
            };

            // persist transaction
            var added = await _transactionsRepository.AddAsync(tx, uow.Connection, uow.Transaction);

            // update balances
            if (sender != null)
            {
                sender.Balance -= request.Amount;
                await _customersRepository.UpdateAsync(sender, uow.Connection, uow.Transaction);
            }

            // credit recipient
            recipient.Balance += request.Amount;
            await _customersRepository.UpdateAsync(recipient, uow.Connection, uow.Transaction);

            await uow.CommitAsync();

            return new TransactionResult { Success = true, Transaction = added, ErrorCode = ErrorCode.None };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new TransactionResult { Success = false, ErrorCode = ErrorCode.InternalError, ErrorMessage = { ex.Message } };
        }
    }

    public async Task<TransactionsListResult> GetTransactionsByCustomerAsync(long customerId, int? pageNumber = null, int? pageSize = null)
    {
        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var list = await _transactionsRepository.GetByCustomerAsync(customerId, uow.Connection, uow.Transaction, pageNumber, pageSize);
            await uow.CommitAsync();

            return new TransactionsListResult { Success = true, Transactions = list?.ToList() ?? new List<TransactionInfo>() };
        }
        catch (Exception ex)
        {
            try { await uow.RollbackAsync(); } catch { }
            return new TransactionsListResult { Success = false, ErrorCode = ErrorCode.InternalError, ErrorMessage = { ex.Message } };
        }
    }
}
