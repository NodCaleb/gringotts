// See https://aka.ms/new-console-template for more information
using Npgsql;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Starting database seeder");

var builder = Host.CreateApplicationBuilder(args);
builder.AddNpgsqlDataSource("gringottsdb");
var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();
var ds = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
var sql = await File.ReadAllTextAsync("seed.sql");
await using var cmd = ds.CreateCommand(sql);
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("Database seeding complete");