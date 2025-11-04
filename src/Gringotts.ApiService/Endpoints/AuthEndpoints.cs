using Gringotts.Infrastructure.Interfaces;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Requests;
using Microsoft.AspNetCore.Http;

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

            var response = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            if (result.Success)
            {
                // success
                response.ErrorCode = ErrorCode.None;
                return Results.Ok(response);
            }

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
    }
}
