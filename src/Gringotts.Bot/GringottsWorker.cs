using Gringotts.Bot.Flows;
using Gringotts.Bot.Markup;
using Gringotts.Contracts.Interfaces;
using Gringotts.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static System.Net.Mime.MediaTypeNames;

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

        var flowEnvelope = await _cache.GetAsync<FlowEnvelope>(user.Id.ToString());

        if (flowEnvelope is not null)
        {
            var flowState = FlowEnvelopeHelper.Unwrap(flowEnvelope);
            switch (flowState)
            {
                case SetNameFlow setNameFlow:
                    var characterName = text.Trim();
                    if (string.IsNullOrEmpty(characterName))
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Имя персонажа не может быть пустым. Пожалуйста, введите корректное имя:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    await _cache.SetAsync(
                        user.Id.ToString(),
                        FlowEnvelopeHelper.Wrap(
                            new SetNameFlow(characterName)
                            )
                        );

                    await _bot.SendMessage(
                        user.Id,
                        $"Введено имя персонажа: {characterName}",
                        replyMarkup: Menus.ConfirmMenu
                    );
                    return;

                default:
                    // Unknown flow state, remove from cache
                    await _cache.RemoveAsync(user.Id.ToString());
                    break;
            }
        }

        // Handle buttons and commands accordingly
        if (text == Commands.Start)
        {
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

            return;
        }

        if (text == Buttons.Balance || text == Commands.Balance)
        {
            var customerResult = await _apiClient.GetCustomerByIdAsync(user.Id);
            await _bot.SendMessage(
                user.Id,
                $"Ваш текущий баланс: {customerResult!.Customer!.Balance:C2}",
                replyMarkup: Menus.MainMenu
            );
            return;
        }

        if (text == Buttons.CharacterName || text == Commands.CharacterName)
        {
            await _cache.SetAsync(
                user.Id.ToString(),
                FlowEnvelopeHelper.Wrap(
                    new SetNameFlow()
                    )
                );

            await _bot.SendMessage(
                user.Id,
                "Введите имя персонажа:",
                replyMarkup: Menus.CancelMenu
            );
            return;
        }

        if (text == Buttons.Cancel || text == Commands.Cancel)
        {
            await _cache.RemoveAsync(user.Id.ToString());
        }

        await SendMenu(user.Id);
    }

    async Task HandleButton(CallbackQuery query)
    {
        var user = query.From;

        // Print to console
        Console.WriteLine($"Callback from {user.FirstName}: {query.Data}");

        if (query.Data == "cancel")
        {
            await _cache.RemoveAsync(user.Id.ToString());
            await _bot.SendMessage(
                user.Id,
                "Действие отменено.",
                replyMarkup: Menus.MainMenu
            );
            return;
        }

        var flowEnvelope = await _cache.GetAsync<FlowEnvelope>(user.Id.ToString());

        if (flowEnvelope is not null)
        {
            var flowState = FlowEnvelopeHelper.Unwrap(flowEnvelope);
            switch (flowState)
            {
                case SetNameFlow setNameFlow:
                    if (query.Data == "confirm")
                    {
                        var characterName = setNameFlow.Name;

                        await _apiClient.UpdateCharacterNameAsync(user.Id, characterName!);

                        await _bot.SendMessage(
                            user.Id,
                            $"Имя персонажа '{characterName}' сохранено.",
                            replyMarkup: Menus.MainMenu
                        );
                        await _cache.RemoveAsync(user.Id.ToString());
                        return;
                    }
                    break;
                default:
                    // Unknown flow state, remove from cache
                    await _cache.RemoveAsync(user.Id.ToString());
                    break;
            }
        }

        await SendMenu(user.Id);
    }

    async Task SendMenu(long userId)
    {
        await _bot.SendMessage(
            userId,
            _greatings,
            replyMarkup: Menus.MainMenu
        );
    }

    
}