using System.Linq;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Results;
using Gringotts.Contracts.DTO;

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

    public async Task<AuthResult> CheckAccessCode(string userName, int accessCode)
    {
        var result = new AuthResult();

        if (string.IsNullOrWhiteSpace(userName))
        {
            result.ErrorMessage.Add("User name must be provided.");
            result.Success = false;
            result.ErrorCode = ErrorCode.ValidationError;
            return result;
        }

        // Access code validation: assume 0 is invalid
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
        result.EmployeeId = employee.Id;
        return result;
    }

    public async Task<IReadOnlyList<EmployeeInfo>> GetEmployeeListAsync()
    {
        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var employees = await _employeeRepository.GetAllAsync(uow.Connection, uow.Transaction);
            await uow.CommitAsync();
            if (employees == null)
                return Array.Empty<EmployeeInfo>();

            // Filter out access codes by projecting to EmployeeInfo (Id + UserName)
            return employees.Select(e => new EmployeeInfo { Id = e.Id, UserName = e.UserName }).ToList();
        }
        catch
        {
            await uow.RollbackAsync();
            return Array.Empty<EmployeeInfo>();
        }
    }

    public async Task<EmployeeResult> GetEmployeeByIdAsync(Guid id)
    {
        var result = new EmployeeResult();
        if (id == Guid.Empty)
        {
            result.Success = false;
            result.ErrorCode = ErrorCode.ValidationError;
            result.Errors.Add("Invalid employee id.");
            return result;
        }

        using var uow = _unitOfWorkFactory.Create();
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(id, uow.Connection, uow.Transaction);
            await uow.CommitAsync();

            if (employee == null)
            {
                result.Success = false;
                result.ErrorCode = ErrorCode.EmployeeNotFound;
                result.Errors.Add("Employee not found.");
                return result;
            }

            result.Success = true;
            result.ErrorCode = ErrorCode.None;
            result.Employee = new EmployeeInfo { Id = employee.Id, UserName = employee.UserName };
            return result;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            result.Success = false;
            result.ErrorCode = ErrorCode.InternalError;
            result.Errors.Add(ex.Message);
            return result;
        }
    }
}
