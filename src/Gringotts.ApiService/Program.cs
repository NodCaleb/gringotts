using Gringotts.ApiService.Endpoints;
using Gringotts.Infrastructure.Bootstrapping;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("gringottsdb");

// Register Infrastructure services and repositories
builder.Services.AddInfrastructure();

builder.Services.AddProblemDetails();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gringotts API",
        Version = "v1",
        Description = "API for the Gringotts service"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gringotts API V1");
    });
}

app.MapDbPing();

app.MapAuthEndpoints();
app.MapCustomersEndpoints();
app.MapTransactionsEndpoints();

app.MapDefaultEndpoints();

app.Run();
