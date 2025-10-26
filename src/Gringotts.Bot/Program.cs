// See https://aka.ms/new-console-template for more information

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

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        //services.AddSingleton<IGameService, MemoryGameService>();
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            string botToken = configuration["Telegram:BotToken"];
            return new TelegramBotClient(botToken);
        });
        services.AddHostedService<Gringotts.Bot.BackgroundWorker>();
    })
    .Build();

await host.RunAsync();