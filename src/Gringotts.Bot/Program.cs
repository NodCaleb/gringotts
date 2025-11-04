// See https://aka.ms/new-console-template for more information

using Gringotts.Bot;
using Gringotts.Contracts.Interfaces;
using Gringotts.Infrastructure.Bootstrapping;
using Gringotts.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

var builder = Host.CreateApplicationBuilder(args);

// Apply standard service defaults (service discovery, resilience, health checks, OpenTelemetry)
builder.AddServiceDefaults();

builder.AddRedisDistributedCache(connectionName: "cache");

// Register services
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    string botToken = configuration["Telegram:BotToken"];
    return new TelegramBotClient(botToken);
});

// Register IHttpClientFactory and the ApiClient implementation for IApiClient
builder.Services.AddHttpClient("GringottsApiClient", client =>
{
    client.BaseAddress = new Uri("http+https://gringotts-api");
});

builder.Services.AddSingleton<IApiClient, ApiClient>();

builder.Services.AddCache("Redis");

builder.Services.AddHostedService<GringottsWorker>();

var host = builder.Build();

await host.RunAsync();