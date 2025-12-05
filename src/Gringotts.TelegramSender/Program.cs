using Gringotts.TelegramSender;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Registers QueueServiceClient using connection string named "telegram-outbox"
builder.AddAzureQueueServiceClient("telegram-outbox");

builder.Services.AddHostedService<TelegramSenderWorker>();

var app = builder.Build();

app.Run();
