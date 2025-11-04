using Gringotts.BFF.Endpoints;
using Gringotts.Contracts.Interfaces;
using Gringotts.Infrastructure.Clients;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register IHttpClientFactory and the ApiClient implementation for IApiClient
builder.Services.AddHttpClient("GringottsApiClient", client =>
{
    client.BaseAddress = new Uri("http+https://gringotts-api");
});

builder.Services.AddSingleton<IApiClient, ApiClient>();

builder.Services.AddProblemDetails();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gringotts BFF",
        Version = "v1",
        Description = "Backend for the Gringotts Web UI"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gringotts BFF V1");
    });
}

app.MapAuthEndpoints();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.Run();
