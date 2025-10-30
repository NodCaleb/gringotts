using Gringotts.Contracts.Interfaces;
using Gringotts.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Gringotts.Bot;

internal class BackgroundWorker : BackgroundService
{
    string _guide = "Бот в разработке";
    private readonly ITelegramBotClient _bot;
    private readonly IApiClient _apiClient;

    public BackgroundWorker(ITelegramBotClient bot, IApiClient apiClient)
    {
        _bot = bot;
        _apiClient = apiClient;
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

        //var iGame = _gameService.GetGame(user.Id.ToString());

        //if (iGame is not null && iGame.GetType() == typeof(CodeGuessGame))
        //{
        //    var game = (CodeGuessGame)iGame;

        //    var response = game.Guess(text);

        //    if (!response.CorrectInput)
        //    {
        //        await _bot.SendMessage(
        //            user.Id,
        //            $"Пожалуйста, введи {game.CodeLength} цифры!" +
        //            Environment.NewLine +
        //            $"Или /stop, чтобы закончить игру"
        //        );
        //    }
        //    else if (response.CorrectGuess)
        //    {
        //        await _bot.SendMessage(
        //            user.Id,
        //            "Это правильный ответ 😉"
        //        );
        //    }
        //    else
        //    {
        //        await _bot.SendMessage(
        //            user.Id,
        //            $"Верных цифр в правильном месте: {response.CorrectSymbolAndPositionCount}" +
        //            Environment.NewLine +
        //            $"Верных цифр в неправильном месте: {response.CorrectSymbolCount}"
        //        );
        //    }

        //    return;
        //}

        await _bot.SendMessage(
            user.Id,
            _guide
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
                    fullName + Environment.NewLine 
                );
                break;

            //case "/stop":
            //    _gameService.StopGame(userId.ToString());
            //    await _bot.SendMessage(
            //        userId,
            //        "Игра остановлена" + Environment.NewLine + _guide
            //    );
            //    break;

            //case "/game1":
            //    game = new CodeGuessGame(4);
            //    _gameService.AddGame(userId.ToString(), game);
            //    await _bot.SendMessage(
            //        userId,
            //        $"Я загадал код из {game.CodeLength} уникальных цифр, попробуй угадать ;)"
            //    );
            //    break;

            //case "/game2":
            //    game = new CodeGuessGame(6, true);
            //    _gameService.AddGame(userId.ToString(), game);
            //    await _bot.SendMessage(
            //        userId,
            //        $"Я загадал код из {game.CodeLength} цифр (цифры могут повторяться), попробуй угадать ;)"
            //    );
            //    break;

            //case "/menu":
            //    await SendMenu(userId);
            //    break;
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
            _guide
        );
    }
}