using Gringotts.Infrastructure.Interfaces;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Enums;

namespace Gringotts.ApiService.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/check", async (IAuthService authService, AuthRequest request) =>
        {
            if (request == null)
            {
                var bad = new BaseResponse { ErrorCode = ErrorCode.ValidationError, Errors = new List<string> { "Request body is required." } };
                return Results.BadRequest(bad);
            }

            var result = await authService.CheckAccessCode(request.UserName, request.AccessCode);

            if (result.Success)
            {
                var success = new AuthResponse { ErrorCode = ErrorCode.None, EmployeeId = result.EmployeeId };
                return Results.Ok(success);
            }

            var response = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            // Map common error codes to HTTP responses
            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(response),
                ErrorCode.EmployeeNotFound => Results.NotFound(response),
                // Return 401 with response body when authentication failed
                ErrorCode.AuthenticationFailed => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };
        }).WithName("CheckAccessCode");

        app.MapGet("/employees", async (IAuthService authService) =>
        {
            var infos = await authService.GetEmployeeListAsync();
            var resp = new EmployeesListResponse { ErrorCode = ErrorCode.None, Employees = infos.ToList() };
            return Results.Ok(resp);
        }).WithName("GetEmployees");

        app.MapGet("/employees/{id:guid}", async (IAuthService authService, Guid id) =>
        {
            var result = await authService.GetEmployeeByIdAsync(id);
            if (result.Success && result.Employee != null)
            {
                var resp = new EmployeeResponse { ErrorCode = ErrorCode.None, Employee = result.Employee };
                return Results.Ok(resp);
            }

            var err = new EmployeeResponse { ErrorCode = result.ErrorCode, Errors = result.Errors };

            return result.ErrorCode switch
            {
                ErrorCode.EmployeeNotFound => Results.NotFound(err),
                ErrorCode.ValidationError => Results.BadRequest(err),
                _ => Results.Problem(detail: result.Errors.FirstOrDefault() ?? "An error occurred.")
            };
        }).WithName("GetEmployeeById");
    }
}
