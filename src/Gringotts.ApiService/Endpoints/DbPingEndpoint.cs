using Npgsql;

namespace Gringotts.ApiService.Endpoints;

public static class DbPingEndpoint
{
    public static void MapDbPing(this WebApplication app)
    {
        app.MapGet("/db-ping", async (NpgsqlDataSource ds) =>
        {
            await using var cmd = ds.CreateCommand("select 'pong'::text");
            var result = await cmd.ExecuteScalarAsync();
            return Results.Json(new { db = result });
        })
        .WithName("DbPing");
    }
}
