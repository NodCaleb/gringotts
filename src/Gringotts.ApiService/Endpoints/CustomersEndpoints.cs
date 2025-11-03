using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Interfaces;
using Gringotts.Shared.Enums;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Results;

namespace Gringotts.ApiService.Endpoints;

public record CharacterNameUpdate(string CharacterName);

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        app.MapGet("/customers", async (ICustomersService customersService, string? search, int? pageNumber, int? pageSize) =>
        {
            // If search query provided, use SearchCustomers, otherwise return all customers (with pagination)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var listResult = await customersService.SearchCustomers(search, pageNumber, pageSize);
                if (listResult.Success)
                {
                    var resp = new CustomersListResponse { ErrorCode = ErrorCode.None, Customers = listResult.Customers };
                    return Results.Ok(resp);
                }

                var error = new BaseResponse { ErrorCode = listResult.ErrorCode, Errors = listResult.ErrorMessage };
                if (listResult.ErrorCode == ErrorCode.ValidationError)
                    return Results.BadRequest(error);

                return Results.Problem(detail: listResult.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
            }

            var allResult = await customersService.GetAllCustomers(pageNumber, pageSize);
            if (allResult.Success)
            {
                var resp = new CustomersListResponse { ErrorCode = ErrorCode.None, Customers = allResult.Customers };
                return Results.Ok(resp);
            }

            var err = new BaseResponse { ErrorCode = allResult.ErrorCode, Errors = allResult.ErrorMessage };
            if (allResult.ErrorCode == ErrorCode.ValidationError)
                return Results.BadRequest(err);

            return Results.Problem(detail: allResult.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        })
        .WithName("GetCustomers");

        // GET /customers/{id}
        app.MapGet("/customers/{id:long}", async (ICustomersService customersService, long id) =>
        {
            var result = await customersService.GetCustomerById(id);
            if (result.Success && result.Customer != null)
            {
                var response = new CustomerResponse { ErrorCode = ErrorCode.None, Customer = result.Customer };
                return Results.Ok(response);
            }

            var errorResponse = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            if (result.ErrorCode == ErrorCode.CustomerNotFound)
            {
                return Results.NotFound(errorResponse);
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("GetCustomerById");

        // POST /customers
        app.MapPost("/customers", async (ICustomersService customersService, Customer customer) =>
        {
            var result = await customersService.CreateOrUpdateCustomer(customer);
            if (result.Success && result.Customer != null)
            {
                var response = new CustomerResponse { ErrorCode = ErrorCode.None, Customer = result.Customer };
                return Results.Created($"/customers/{result.Customer.Id}", response);
            }

            var errorResponse = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            if (result.ErrorCode == ErrorCode.ValidationError)
            {
                return Results.BadRequest(errorResponse);
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("CreateCustomer");

        // PATCH /customers/{id}/charactername
        app.MapPatch("/customers/{id:long}/charactername", async (ICustomersService customersService, long id, CharacterNameUpdate update) =>
        {
            if (update == null || string.IsNullOrWhiteSpace(update.CharacterName))
            {
                var bad = new BaseResponse { ErrorCode = ErrorCode.ValidationError, Errors = new List<string> { "CharacterName is required." } };
                return Results.BadRequest(bad);
            }

            var result = await customersService.UpdateCharacterName(id, update.CharacterName);
            if (result.Success && result.Customer != null)
            {
                var response = new CustomerResponse { ErrorCode = ErrorCode.None, Customer = result.Customer };
                return Results.Ok(response);
            }

            var errorResponse = new BaseResponse { ErrorCode = result.ErrorCode, Errors = result.ErrorMessage };

            if (result.ErrorCode == ErrorCode.CustomerNotFound)
            {
                return Results.NotFound(errorResponse);
            }

            if (result.ErrorCode == ErrorCode.ValidationError)
            {
                return Results.BadRequest(errorResponse);
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("UpdateCharacterName");
    }
}
