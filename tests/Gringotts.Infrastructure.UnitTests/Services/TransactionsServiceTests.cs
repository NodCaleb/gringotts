using System.Data;
using Moq;
using Gringotts.Infrastructure.Services;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Enums;

namespace Gringotts.Infrastructure.UnitTests.Services;

public class TransactionsServiceTests
{
    private readonly Mock<ITransactionsRepository> _transactionsRepo = new();
    private readonly Mock<ICustomersRepository> _customersRepo = new();
    private readonly Mock<IEmployeesRepository> _employeesRepo = new();
    private readonly Mock<IUnitOfWorkFactory> _uowFactory = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private TransactionsService CreateService()
    {
        _uowFactory.Setup(f => f.Create()).Returns(_uow.Object);
        _uow.SetupGet(u => u.Connection).Returns((IDbConnection?)null);
        _uow.SetupGet(u => u.Transaction).Returns((IDbTransaction?)null);
        _uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        return new TransactionsService(_transactionsRepo.Object, _customersRepo.Object, _employeesRepo.Object, _uowFactory.Object);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsValidationError_WhenRecipientMissing()
    {
        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 0,
            RecipientUsername = " ",
            SenderId = 1,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Either RecipientId or RecipientUsername is required."));

        _uow.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsValidationError_WhenSenderAndEmployeeMissing()
    {
        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 1,
            SenderId = null,
            EmployeeId = null,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Either SenderId or EmployeeId must be provided."));
    }

    [Fact]
    public async Task CreateTransaction_ReturnsValidationError_WhenAmountNonPositive()
    {
        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 1,
            SenderId = 1,
            Amount = 0,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Amount must be positive."));
    }

    [Fact]
    public async Task CreateTransaction_ReturnsValidationError_WhenDescriptionMissing()
    {
        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 1,
            SenderId = 1,
            Amount = 5,
            Description = " "
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Description is required."));
    }

    [Fact]
    public async Task CreateTransaction_ReturnsCustomerNotFound_WhenRecipientNotFound()
    {
        _customersRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 123,
            SenderId = 1,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CustomerNotFound, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Recipient not found."));

        _uow.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsCustomerNotFound_WhenSenderNotFound()
    {
        var recipient = new Customer { Id = 2, Balance = 100 };
        _customersRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(recipient);
        _customersRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = 99,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CustomerNotFound, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Sender not found."));

        _uow.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsInsufficientFunds_WhenSenderHasInsufficientFunds()
    {
        var recipient = new Customer { Id = 2, Balance = 50 };
        var sender = new Customer { Id = 1, Balance = 5 };

        _customersRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(recipient);
        _customersRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(sender);

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = 1,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.InsufficientFunds, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Sender has insufficient funds."));

        _uow.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsEmployeeNotFound_WhenEmployeeMissing()
    {
        var recipient = new Customer { Id = 2, Balance = 50 };
        _customersRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(recipient);
        _employeesRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = null,
            EmployeeId = Guid.NewGuid(),
            Amount = 20,
            Description = "service"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.EmployeeNotFound, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Employee not found."));

        _uow.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsSuccess_WhenSenderHasSufficientFunds()
    {
        var recipient = new Customer { Id = 2, Balance = 50 };
        var sender = new Customer { Id = 1, Balance = 100 };

        _customersRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(recipient);
        _customersRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(sender);

        _transactionsRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((Transaction tx, IDbConnection _, IDbTransaction __, System.Threading.CancellationToken ___) =>
            {
                tx.Id = Guid.NewGuid();
                return tx;
            });

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = 1,
            Amount = 25,
            Description = "gift"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.True(result.Success);
        Assert.Equal(ErrorCode.None, result.ErrorCode);
        Assert.NotNull(result.Transaction);
        Assert.Equal(25, result.Transaction.Amount);

        _customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 1 && c.Balance == 75), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        _customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 2 && c.Balance == 75), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsSuccess_WhenEmployeeProvided()
    {
        var recipient = new Customer { Id = 2, Balance = 10 };
        var employee = new Employee { Id = Guid.NewGuid() };

        _customersRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(recipient);
        _employeesRepo.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(employee);

        _transactionsRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((Transaction tx, IDbConnection _, IDbTransaction __, System.Threading.CancellationToken ___) =>
            {
                tx.Id = Guid.NewGuid();
                return tx;
            });

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = null,
            EmployeeId = employee.Id,
            Amount = 15,
            Description = "reward"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.True(result.Success);
        Assert.Equal(ErrorCode.None, result.ErrorCode);
        Assert.NotNull(result.Transaction);

        _customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 2 && c.Balance == 25), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        _customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 1), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsInternalError_WhenRepositoryThrows()
    {
        _customersRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
            .ThrowsAsync(new System.Exception("boom"));

        var service = CreateService();

        var req = new TransactionRequest
        {
            RecipientId = 2,
            SenderId = 1,
            Amount = 10,
            Description = "payment"
        };

        var result = await service.CreateTransactionAsync(req);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.InternalError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("boom"));

        _uow.Verify(u => u.RollbackAsync(), Times.Once);
    }
}
