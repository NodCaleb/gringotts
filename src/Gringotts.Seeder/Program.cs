// See https://aka.ms/new-console-template for more information
using Npgsql;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Starting database seeder");

var builder = Host.CreateApplicationBuilder(args);
builder.AddNpgsqlDataSource("gringottsdb");
var app = builder.Build();

// Attempt to locate the SQL file in the project (Database/create_tables_postgres.sql)
var relativePath = Path.Combine("Database", "create_tables_postgres.sql");
var sqlFile = FindFileInParentDirectories(relativePath);
if (sqlFile == null)
{
    Console.Error.WriteLine($"Could not locate SQL file '{relativePath}'. Make sure it exists in the Gringotts.Seeder project under the 'Database' folder.");
    return;
}

await using var scope = app.Services.CreateAsyncScope();
var ds = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
var sql = await File.ReadAllTextAsync(sqlFile);
await using var cmd = ds.CreateCommand(sql);
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("Database tables created/ensured.");

// Now check employees table. If empty, read employees from Data/employees.txt and insert with random6-digit access codes.
await using var conn = ds.CreateConnection();
await conn.OpenAsync();

await using (var countCmd = conn.CreateCommand())
{
    countCmd.CommandText = "SELECT COUNT(*) FROM employees;";
    var countObj = await countCmd.ExecuteScalarAsync();
    var count = Convert.ToInt64(countObj ?? 0L);

    if (count == 0)
    {
        Console.WriteLine("Employees table is empty. Reading employees from Data/employees.txt...");
        var empRelative = Path.Combine("Data", "employees.txt");
        var empFile = FindFileInParentDirectories(empRelative);
        if (empFile == null)
        {
            Console.Error.WriteLine($"Could not locate employees file '{empRelative}'. Add employee names (one per line) to 'Gringotts.Seeder/Data/employees.txt'.");
        }
        else
        {
            var lines = File.ReadAllLines(empFile)
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToArray();

            var rng = new Random();

            foreach (var name in lines)
            {
                var accessCode = rng.Next(100000, 1000000); //6-digit

                await using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO employees (username, accesscode) VALUES (@u, @a);";
                var p1 = insertCmd.CreateParameter(); p1.ParameterName = "@u"; p1.Value = name; insertCmd.Parameters.Add(p1);
                var p2 = insertCmd.CreateParameter(); p2.ParameterName = "@a"; p2.Value = accessCode; insertCmd.Parameters.Add(p2);

                await insertCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"Added employee: {name} -> AccessCode: {accessCode}");
            }
        }
    }
    else
    {
        Console.WriteLine($"Employees table contains {count} record(s). Listing employees and access codes:");

        await using var listCmd = conn.CreateCommand();
        listCmd.CommandText = "SELECT username, accesscode FROM employees ORDER BY username;";
        await using var reader = await listCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var username = reader.GetString(0);
            var accessCode = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
            Console.WriteLine($"{username} -> AccessCode: {accessCode}");
        }
    }
}

Console.WriteLine("Database seeding complete");

static string? FindFileInParentDirectories(string relativePath)
{
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir))
    {
        var candidate = Path.Combine(dir, relativePath);
        if (File.Exists(candidate))
            return candidate;

        var parent = Directory.GetParent(dir);
        if (parent == null)
            break;

        dir = parent.FullName;
    }

    // also try current working directory as a last resort
    var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    if (File.Exists(cwdCandidate))
        return cwdCandidate;

    return null;
}