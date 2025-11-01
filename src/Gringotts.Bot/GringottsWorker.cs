using Gringotts.Bot.Markup;
using Gringotts.Contracts.Interfaces;
using Gringotts.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Gringotts.Bot;

internal class GringottsWorker : BackgroundService
{
    string _greatings = "Выберите действие";
    private readonly ITelegramBotClient _bot;
    private readonly IApiClient _apiClient;
    private readonly ICache _cache;

    public GringottsWorker(ITelegramBotClient bot, IApiClient apiClient, ICache cache)
    {
        _bot = bot;
        _apiClient = apiClient;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _bot.GetMe();

        _bot.StartReceiving(
            updateHandler: HandleUpdate,
            errorHandler: HandleError,
            cancellationToken: stoppingToken
        );

        Console.WriteLine(
            $"Bot @{me.Username} is running." +
            Environment.NewLine +
            $"Listening for updates." +
            Environment.NewLine +
            $"Press enter to stop"
            );

        while (!stoppingToken.IsCancellationRequested)
        {

        }
    }

    // Each time a user interacts with the bot, this method is called
    async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                // A message was received
                case UpdateType.Message:
                    await HandleMessage(update.Message!);
                    break;

                // A button was pressed
                case UpdateType.CallbackQuery:
                    await HandleButton(update.CallbackQuery!);
                    break;
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message);
        }
    }

    async Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        await Console.Error.WriteLineAsync(exception.Message);
    }

    async Task HandleMessage(Message msg)
    {
        var user = msg.From;
        var text = msg.Text ?? string.Empty;

        if (user is null)
            return;

        // Print to console
        Console.WriteLine($"{user.FirstName} wrote {text}");

        // When we get a command, we react accordingly
        if (text.StartsWith("/"))
        {
            await HandleCommand(user, text);
            return;
        }

        if (text == Buttons.Cancel)
        {
            await _cache.RemoveAsync(user.Id.ToString());
        }

        await _bot.SendMessage(
            user.Id,
            _greatings,
            replyMarkup: Menus.MainMenu
        );
    }


    async Task HandleCommand(User user, string command)
    {
        //CodeGuessGame game;

        switch (command)
        {
            case "/start":
                var username = "@" + user.Username;
                var fullName = user.FirstName + (string.IsNullOrEmpty(user.LastName) ? "" : " " + user.LastName);
                var userId = user.Id;

                var customer = new Customer
                {
                    Id = userId,
                    UserName = username,
                    PersonalName = fullName
                };

                await _apiClient.CreateCustomerAsync(customer);

                await _bot.SendMessage(
                    user.Id,
                    "Привет!" + Environment.NewLine +
                    fullName + Environment.NewLine,
                    replyMarkup: Menus.MainMenu
                );
                break;
        }

        await Task.CompletedTask;
    }

    async Task HandleButton(CallbackQuery query)
    {

    }

    async Task SendMenu(long userId)
    {
        await _bot.SendMessage(
            userId,
            _greatings
        );
    }

    
}