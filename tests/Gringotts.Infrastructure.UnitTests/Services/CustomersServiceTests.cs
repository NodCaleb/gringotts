using System.Data;
using Moq;
using Gringotts.Infrastructure.Services;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Contracts.Enums;

namespace Gringotts.Infrastructure.UnitTests.Services;

public class CustomersServiceTests
{
    [Fact]
    public async Task GetCustomerById_ReturnsNotFound_WhenCustomerMissing()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        customersRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
        .ReturnsAsync((Customer?)null);

        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.GetCustomerById(123);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CustomerNotFound, result.ErrorCode);
        Assert.Contains("Customer not found.", result.ErrorMessage);
        customersRepo.Verify(r => r.GetByIdAsync(123, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default), Times.Once);
    }

    [Fact]
    public async Task GetCustomerById_ReturnsCustomer_WhenFound()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        var customer = new Customer { Id = 1, UserName = "user1", PersonalName = "P", CharacterName = "C", Balance = 10m };
        customersRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
        .ReturnsAsync(customer);

        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.GetCustomerById(1);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage.FirstOrDefault());
        Assert.Equal(customer, result.Customer);
        customersRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateCustomer_ReturnsCreated_WhenRepositoryAdds()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        var input = new Customer { UserName = "u", PersonalName = "p", CharacterName = "c", Balance = 0m };
        var added = new Customer { Id = 10, UserName = "u", PersonalName = "p", CharacterName = "c", Balance = 0m };

        customersRepo.Setup(r => r.AddAsync(input, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
        .ReturnsAsync(added);

        uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.CreateCustomer(input);

        Assert.True(result.Success);
        Assert.Equal(added, result.Customer);
        uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCustomer_ReturnsValidationError_OnArgumentException()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        var input = new Customer { UserName = "u", PersonalName = "p", CharacterName = "c", Balance = 0m };

        customersRepo.Setup(r => r.AddAsync(input, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
        .ThrowsAsync(new ArgumentException("invalid"));

        uow.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.CreateCustomer(input);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains("invalid", result.ErrorMessage);
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCustomer_ReturnsNotFound_WhenMissing()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        customersRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default))
        .ReturnsAsync((Customer?)null);

        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.UpdateCustomer(5, new Customer { UserName = "x" });

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CustomerNotFound, result.ErrorCode);
        Assert.Contains("Customer not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateCustomer_UpdatesAndCommits_WhenFound()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        var existing = new Customer { Id = 7, UserName = "old", PersonalName = "P", CharacterName = "C", Balance = 5m };
        customersRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default)).ReturnsAsync(existing);
        customersRepo.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default)).Returns(Task.CompletedTask);

        uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var update = new Customer { UserName = "new", PersonalName = "NewP", CharacterName = "NewC", Balance = 20m };

        var result = await svc.UpdateCustomer(7, update);

        Assert.True(result.Success);
        Assert.Equal(7, result.Customer!.Id);
        Assert.Equal("new", result.Customer!.UserName);
        Assert.Equal(20m, result.Customer!.Balance);
        uow.Verify(u => u.CommitAsync(), Times.Once);
        customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 7 && c.UserName == "new"), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateCharacterName_ReturnsNotFound_WhenMissing()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        customersRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default)).ReturnsAsync((Customer?)null);
        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.UpdateCharacterName(9, "Hero");

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.CustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateCharacterName_UpdatesAndCommits_WhenFound()
    {
        var customersRepo = new Mock<ICustomersRepository>();
        var uow = new Mock<IUnitOfWork>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();

        var existing = new Customer { Id = 11, UserName = "u11", PersonalName = "P11", CharacterName = "Old", Balance = 0m };
        customersRepo.Setup(r => r.GetByIdAsync(11, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default)).ReturnsAsync(existing);
        customersRepo.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default)).Returns(Task.CompletedTask);

        uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        uowFactory.Setup(f => f.Create()).Returns(uow.Object);

        var svc = new CustomersService(customersRepo.Object, uowFactory.Object);

        var result = await svc.UpdateCharacterName(11, "NewName");

        Assert.True(result.Success);
        Assert.Equal("NewName", result.Customer!.CharacterName);
        uow.Verify(u => u.CommitAsync(), Times.Once);
        customersRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Id == 11 && c.CharacterName == "NewName"), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), default), Times.Once);
    }
}
