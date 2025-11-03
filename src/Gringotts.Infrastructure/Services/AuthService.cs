using System.Linq;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Results;

namespace Gringotts.Infrastructure.Services;

internal class AuthService : IAuthService
{
    private readonly IEmployeesRepository _employeeRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public AuthService(IEmployeesRepository employeeRepository, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _employeeRepository = employeeRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Result> CheckAccessCode(string userName, int accessCode)
    {
        var result = new Result();

        if (string.IsNullOrWhiteSpace(userName))
        {
            result.ErrorMessage.Add("User name must be provided.");
            result.Success = false;
            result.ErrorCode = ErrorCode.ValidationError;
            return result;
        }

        // Access code validation: assume0 is invalid
        if (accessCode == 0)
        {
            result.ErrorMessage.Add("Access code must be provided and non-zero.");
            result.Success = false;
            result.ErrorCode = ErrorCode.ValidationError;
            return result;
        }

        using var uow = _unitOfWorkFactory.Create();
        Employee? employee = null;

        try
        {
            employee = await _employeeRepository.GetByNameAsync(userName.Trim(), uow.Connection, uow.Transaction);

            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();

            result.ErrorMessage.Add("Failed to retrieve employee.");
            result.ErrorMessage.Add(ex.Message);
            result.Success = false;
            result.ErrorCode = ErrorCode.InternalError;
            return result;
        }

        if (employee is null)
        {
            result.ErrorMessage.Add("Employee not found.");
            result.Success = false;
            result.ErrorCode = ErrorCode.EmployeeNotFound;
            return result;
        }

        if (employee.AccessCode != accessCode)
        {
            result.ErrorMessage.Add("Access code does not match.");
            result.Success = false;
            result.ErrorCode = ErrorCode.AuthenticationFailed;
            return result;
        }

        result.Success = true;
        result.ErrorCode = ErrorCode.None;
        return result;
    }

    public async Task<IReadOnlyList<string>> GetEmployeeNamesAsync()
    {
        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var employees = await _employeeRepository.GetAllAsync(uow.Connection, uow.Transaction);
            await uow.CommitAsync();
            if (employees == null)
                return Array.Empty<string>();

            // Filter out access codes by projecting only usernames
            return employees.Select(e => e.UserName).ToList();
        }
        catch
        {
            await uow.RollbackAsync();
            return Array.Empty<string>();
        }
    }
}
