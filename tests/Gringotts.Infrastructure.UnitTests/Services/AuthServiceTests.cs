using System.Data;
using Moq;
using Gringotts.Infrastructure.Services;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;

namespace Gringotts.Infrastructure.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IEmployeesRepository> _employeeRepo = new();
    private readonly Mock<IUnitOfWorkFactory> _uowFactory = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AuthService CreateService()
    {
        _uowFactory.Setup(f => f.Create()).Returns(_uow.Object);
        _uow.SetupGet(u => u.Connection).Returns((IDbConnection?)null);
        _uow.SetupGet(u => u.Transaction).Returns((IDbTransaction?)null);
        _uow.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        return new AuthService(_employeeRepo.Object, _uowFactory.Object);
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsValidationError_WhenUserNameIsNullOrWhitespace()
    {
        var service = CreateService();

        var result = await service.CheckAccessCode(" ", 123);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("User name must be provided"));
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsValidationError_WhenAccessCodeIsZero()
    {
        var service = CreateService();

        var result = await service.CheckAccessCode("bob", 0);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.ValidationError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Access code must be provided"));
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsEmployeeNotFound_WhenRepositoryReturnsNull()
    {
        _employeeRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
        .ReturnsAsync((Employee?)null);

        var service = CreateService();

        var result = await service.CheckAccessCode("alice", 123);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.EmployeeNotFound, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Employee not found"));

        _uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsAuthenticationFailed_WhenAccessCodeDoesNotMatch()
    {
        var employee = new Employee { UserName = "carol", AccessCode = 999 };
        _employeeRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
        .ReturnsAsync(employee);

        var service = CreateService();

        var result = await service.CheckAccessCode("carol", 123);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.AuthenticationFailed, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Access code does not match"));

        _uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsSuccess_WhenAccessCodeMatches()
    {
        var employee = new Employee { UserName = "dave", AccessCode = 321 };
        _employeeRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
        .ReturnsAsync(employee);

        var service = CreateService();

        var result = await service.CheckAccessCode("dave", 321);

        Assert.True(result.Success);
        Assert.Equal(ErrorCode.None, result.ErrorCode);
        Assert.Empty(result.ErrorMessage);

        _uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckAccessCode_ReturnsInternalError_WhenRepositoryThrows()
    {
        _employeeRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>(), It.IsAny<System.Threading.CancellationToken>()))
        .ThrowsAsync(new System.Exception("boom"));

        var service = CreateService();

        var result = await service.CheckAccessCode("eve", 111);

        Assert.False(result.Success);
        Assert.Equal(ErrorCode.InternalError, result.ErrorCode);
        Assert.Contains(result.ErrorMessage, m => m.Contains("Failed to retrieve employee"));
        Assert.Contains(result.ErrorMessage, m => m.Contains("boom"));

        _uow.Verify(u => u.RollbackAsync(), Times.Once);
    }
}
