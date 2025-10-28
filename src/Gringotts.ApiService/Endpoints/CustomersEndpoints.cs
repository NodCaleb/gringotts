using Gringotts.Domain.Entities;
using Gringotts.Infrastructure.Contracts;
using Gringotts.Shared.Enums;

namespace Gringotts.ApiService.Endpoints;

public record CharacterNameUpdate(string CharacterName);

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        app.MapGet("/customers", () => Results.Json(new { message = "Customers endpoint is under construction." }))
           .WithName("GetCustomers");

        // GET /customers/{id}
        app.MapGet("/customers/{id:long}", async (ICustomersService customersService, long id) =>
        {
            var result = await customersService.GetCustomerById(id);
            if (result.Success && result.Customer != null)
            {
                return Results.Ok(result.Customer);
            }

            if (result.ErrorCode == ErrorCode.CustomerNotFound)
            {
                return Results.NotFound(new { errors = result.ErrorMessage });
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("GetCustomerById");

        // POST /customers
        app.MapPost("/customers", async (ICustomersService customersService, Customer customer) =>
        {
            var result = await customersService.CreateCustomer(customer);
            if (result.Success && result.Customer != null)
            {
                return Results.Created($"/customers/{result.Customer.Id}", result.Customer);
            }

            if (result.ErrorCode == ErrorCode.ValidationError)
            {
                return Results.BadRequest(new { errors = result.ErrorMessage });
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("CreateCustomer");

        // PATCH /customers/{id}/charactername
        app.MapPatch("/customers/{id:long}/charactername", async (ICustomersService customersService, long id, CharacterNameUpdate update) =>
        {
            if (update == null || string.IsNullOrWhiteSpace(update.CharacterName))
            {
                return Results.BadRequest(new { errors = new[] { "CharacterName is required." } });
            }

            var result = await customersService.UpdateCharacterName(id, update.CharacterName);
            if (result.Success && result.Customer != null)
            {
                return Results.Ok(result.Customer);
            }

            if (result.ErrorCode == ErrorCode.CustomerNotFound)
            {
                return Results.NotFound(new { errors = result.ErrorMessage });
            }

            if (result.ErrorCode == ErrorCode.ValidationError)
            {
                return Results.BadRequest(new { errors = result.ErrorMessage });
            }

            return Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.");
        }).WithName("UpdateCharacterName");
    }
}
