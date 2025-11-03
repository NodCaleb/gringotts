using Gringotts.Shared.Enums;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Requests;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Contracts.DTO;
using Gringotts.Contracts.Results;

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

        app.MapGet("/customers/{id:long}/transactions", async (ITransactionsService transactionsService, long id, int? pageNumber, int? pageSize) =>
        {
            var result = await transactionsService.GetTransactionsByCustomerAsync(id, pageNumber, pageSize);
            if (result.Success)
            {
                var resp = new TransactionsListResponse { ErrorCode = ErrorCode.None, Transactions = result.Transactions };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("GetTransactionsByCustomer");

        app.MapGet("/transactions", async (ITransactionsService transactionsService, int? pageNumber, int? pageSize) =>
        {
            var result = await transactionsService.GetTransactionsByCustomerAsync(null, pageNumber, pageSize);
            if (result.Success)
            {
                var resp = new TransactionsListResponse { ErrorCode = ErrorCode.None, Transactions = result.Transactions };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("GetAllTransactions");
    }
}
