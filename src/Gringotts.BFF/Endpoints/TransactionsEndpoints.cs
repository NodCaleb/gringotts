using Gringotts.Contracts.Interfaces;
using Gringotts.Contracts.Responses;
using Gringotts.Shared.Enums;

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
        }).WithName("BffGetAllTransactions");

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
        }).WithName("BffGetTransactionsByCustomer");
    }
}
