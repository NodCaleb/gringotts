using Gringotts.Infrastructure.Contracts;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Requests;

namespace Gringotts.ApiService.Endpoints;

public static class TransactionsEndpoints
{
    public static void MapTransactionsEndpoints(this WebApplication app)
    {
        app.MapPost("/transactions", async (ITransactionsService transactionsService, TransactionRequest request) =>
        {
            if (request == null)
            {
                var bad = new BaseResponse { ErrorCode = ErrorCode.ValidationError, Errors = new List<string> { "Request body is required." } };
                return Results.BadRequest(bad);
            }

            var result = await transactionsService.CreateTransactionAsync(request);

            var errorResponse = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            if (result.Success && result.Transaction != null)
            {
                var response = new { ErrorCode = ErrorCode.None, Transaction = result.Transaction };
                return Results.Created($"/transactions/{result.Transaction.Id}", response);
            }

            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(errorResponse),
                ErrorCode.CustomerNotFound => Results.NotFound(errorResponse),
                ErrorCode.InsufficientFunds => Results.BadRequest(errorResponse),
                ErrorCode.EmployeeNotFound => Results.NotFound(errorResponse),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };

        }).WithName("CreateTransaction");
    }
}
