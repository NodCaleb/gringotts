namespace Gringotts.ApiService.Endpoints;

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        app.MapGet("/customers", () => Results.Json(new { message = "Customers endpoint is under construction." }))
           .WithName("GetCustomers");
    }
}
