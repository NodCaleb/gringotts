using Gringotts.Contracts.Interfaces;
using Gringotts.Contracts.Responses;
using Gringotts.Shared.Enums;

namespace Gringotts.BFF.Endpoints;

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        // GET /bff/customers?search=&pageNumber=&pageSize=
        app.MapGet("/bff/customers", async (IApiClient apiClient, string? search, int? pageNumber, int? pageSize) =>
        {
            var result = await apiClient.SearchCustomersAsync(search, pageNumber, pageSize);
            if (result.Success)
            {
                var resp = new CustomersListResponse { ErrorCode = ErrorCode.None, Customers = result.Customers };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(err),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };
        }).RequireAuthorization().WithName("BffGetCustomers");

        // GET /bff/customers/{id}
        app.MapGet("/bff/customers/{id:long}", async (IApiClient apiClient, long id) =>
        {
            var result = await apiClient.GetCustomerByIdAsync(id);
            if (result.Success && result.Customer != null)
            {
                var resp = new CustomerResponse { ErrorCode = ErrorCode.None, Customer = result.Customer };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };
            return result.ErrorCode switch
            {
                ErrorCode.CustomerNotFound => Results.NotFound(err),
                ErrorCode.ValidationError => Results.BadRequest(err),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };
        }).RequireAuthorization().WithName("BffGetCustomerById");
    }
}
