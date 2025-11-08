using Gringotts.Contracts.Interfaces;
using Gringotts.Contracts.Responses;
using Gringotts.Shared.Enums;
using System.Security.Claims;
using Gringotts.Contracts.Requests;

namespace Gringotts.BFF.Endpoints;

public static class TransactionsEndpoints
{
    public static void MapTransactionsEndpoints(this WebApplication app)
    {
        // GET /bff/transactions?pageNumber=&pageSize=
        app.MapGet("/bff/transactions", async (IApiClient apiClient, int? pageNumber, int? pageSize) =>
        {
            var result = await apiClient.GetTransactionsAsync(pageNumber, pageSize);
            if (result.Success)
            {
                var resp = new TransactionsListResponse { ErrorCode = ErrorCode.None, Transactions = result.Transactions };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(err),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };
        }).RequireAuthorization().WithName("BffGetAllTransactions");

        // GET /bff/customers/{id}/transactions?pageNumber=&pageSize=
        app.MapGet("/bff/customers/{id:long}/transactions", async (IApiClient apiClient, long id, int? pageNumber, int? pageSize) =>
        {
            var result = await apiClient.GetTransactionsByCustomerAsync(id, pageNumber, pageSize);
            if (result.Success)
            {
                var resp = new TransactionsListResponse { ErrorCode = ErrorCode.None, Transactions = result.Transactions };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return result.ErrorCode switch
            {
                ErrorCode.CustomerNotFound => Results.NotFound(err),
                ErrorCode.ValidationError => Results.BadRequest(err),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };
        }).RequireAuthorization().WithName("BffGetTransactionsByCustomer");

        // POST /bff/transactions
        // Accepts RecipientId, Amount and Description from body. EmployeeId is taken from authenticated user. SenderId is null.
        app.MapPost("/bff/transactions", async (IApiClient apiClient, HttpContext http, CreateTransactionDto dto) =>
        {
            if (dto == null)
            {
                var bad = new BaseResponse { ErrorCode = ErrorCode.ValidationError, Errors = new List<string> { "Request body is required." } };
                return Results.BadRequest(bad);
            }

            // Extract employee id from claims (NameIdentifier contains the user id set by TokensService)
            var claim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var employeeId))
            {
                return Results.Unauthorized();
            }

            var request = new TransactionRequest
            {
                RecipientId = dto.RecipientId,
                SenderId = null,
                EmployeeId = employeeId,
                Amount = dto.Amount,
                Description = dto.Description ?? string.Empty
            };

            var result = await apiClient.CreateTransactionAsync(request);

            if (result.Success && result.Transaction != null)
            {
                var response = new { ErrorCode = ErrorCode.None, Transaction = result.Transaction };
                return Results.Created($"/bff/transactions/{result.Transaction.Id}", response);
            }

            var errorResponse = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(errorResponse),
                ErrorCode.CustomerNotFound => Results.NotFound(errorResponse),
                ErrorCode.InsufficientFunds => Results.BadRequest(errorResponse),
                ErrorCode.EmployeeNotFound => Results.NotFound(errorResponse),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };

        }).RequireAuthorization().WithName("BffCreateTransaction");
    }

    // Minimal DTO for creating transaction from BFF client
    public sealed record CreateTransactionDto(long RecipientId, decimal Amount, string Description);
}
