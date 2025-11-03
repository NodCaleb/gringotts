using Gringotts.Bot.Flows;
using Gringotts.Bot.Markup;
using Gringotts.Contracts.Interfaces;
using Gringotts.Contracts.Requests;
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

                case TransferFlow transferFlow when transferFlow.Step == "recipient-search":

                    var searchQuery = text.Trim();

                    if (string.IsNullOrEmpty(searchQuery) || searchQuery.Length < 3)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Введите хотя бы 3 символа, жалко вам что-ли:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    var customersResult = await _apiClient.SearchCustomersAsync(searchQuery);
                    if (customersResult!.Customers!.Count == 0)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Получатели не найдены. Введите другой запрос для поиска:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    if (customersResult!.Customers!.Count == 1)
                    {
                        var recipient = customersResult.Customers.Single();

                        await _cache.SetAsync(
                        user.Id.ToString(),
                        FlowEnvelopeHelper.Wrap(
                            transferFlow with
                            {
                                Step = "amount-enter",
                                Recipient = recipient
                            }
                            )
                        );

                        await _bot.SendMessage(
                            user.Id,
                            $"Получатель: {recipient.ToString()}{Environment.NewLine}Введите сумму:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    await _cache.SetAsync(
                        user.Id.ToString(),
                        FlowEnvelopeHelper.Wrap(
                            transferFlow with
                            {
                                Step = "recipient-select",
                                Customers = customersResult.Customers
                            }
                            )
                        );
                    await _bot.SendMessage(
                        user.Id,
                        $"Выберите получателя:",
                        replyMarkup: Menus.ChooseCustomerMenu(customersResult.Customers)
                    );
                    return;

                case TransferFlow transferFlow when transferFlow.Step == "amount-enter":
                    
                    if (!decimal.TryParse(text.Trim(), out decimal amount) || amount <= 0)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Введите корректную сумму перевода:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    if (transferFlow.Recipient is null)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Получатель не выбран. Пожалуйста, начните заново.",
                            replyMarkup: Menus.MainMenu
                        );
                        await _cache.RemoveAsync(user.Id.ToString());
                        return;
                    }

                    if (transferFlow.Sender.Balance < amount)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            $"Недостаточно средств. Ваш текущий баланс: {transferFlow.Sender.Balance:N2}. Введите другую сумму:",
                            replyMarkup: Menus.CancelMenu
                        );
                        return;
                    }

                    await _cache.SetAsync(
                        user.Id.ToString(),
                        FlowEnvelopeHelper.Wrap(
                            transferFlow with
                            {
                                Step = "description-enter",
                                Amount = amount
                            }
                            )
                        );
                    await _bot.SendMessage(
                        user.Id,
                        $"Получатель: {transferFlow.Recipient!.ToString()}{Environment.NewLine}Сумма:{amount:N2}{Environment.NewLine}Введите назначение платежа:",
                        replyMarkup: Menus.ConfirmMenu
                    );
                    return;

                case TransferFlow transferFlow when transferFlow.Step == "description-enter":
                    var description = text.Trim();

                    if (string.IsNullOrEmpty(description))
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Описание не может быть пустым. Пожалуйста, введите корректное описание:",
                            replyMarkup: Menus.ConfirmMenu
                        );
                        return;
                    }

                    if (transferFlow.Recipient is null || transferFlow.Amount is null)
                    {
                        await _bot.SendMessage(
                            user.Id,
                            "Ошибка во время перевода. Пожалуйста, начните заново.",
                            replyMarkup: Menus.MainMenu
                        );
                        await _cache.RemoveAsync(user.Id.ToString());
                        return;
                    }

                    await _cache.SetAsync(
                        user.Id.ToString(),
                        FlowEnvelopeHelper.Wrap(
                            transferFlow with
                            {
                                Step = "confirm",
                                Description = description
                            }
                            )
                        );

                    await _bot.SendMessage(
                        user.Id,
                        $"Пожалуйста, подтвердите перевод:{Environment.NewLine}" +
                        $"Получатель: {transferFlow.Recipient.ToString()}{Environment.NewLine}" +
                        $"Сумма: {transferFlow.Amount:N2}{Environment.NewLine}" +
                        $"Описание: {description}",
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
            var customer = await CreateOrUpdateCustomer(user);

            await _bot.SendMessage(
                user.Id,
                "Банк Гринготтс привествует вас!" + Environment.NewLine +
                customer.PersonalName + Environment.NewLine +
                "Введите имя персонажа:",
                replyMarkup: Menus.CancelMenu
            );

            await _cache.SetAsync(
                user.Id.ToString(),
                FlowEnvelopeHelper.Wrap(
                    new SetNameFlow()
                    )
                );

            return;
        }

        if (text == Buttons.Balance || text == Commands.Balance)
        {
            var customer = await CreateOrUpdateCustomer(user);

            await _bot.SendMessage(
                user.Id,
                $"Ваш текущий баланс: {customer.Balance:N2}",
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

        if (text == Buttons.Transfer || text == Commands.Transfer)
        {
            var sender = await CreateOrUpdateCustomer(user);

            await _cache.SetAsync(
                user.Id.ToString(),
                FlowEnvelopeHelper.Wrap(
                    new TransferFlow("recipient-search", sender)
                    )
                );

            await _bot.SendMessage(
                user.Id,
                "Поиск получателя (пользователь Telegram, имя игрока или персонажа):",
                replyMarkup: Menus.CancelMenu
            );
            return;
        }

        if (text == Buttons.Cancel || text == Commands.Cancel)
        {
            await CancelOperation(user.Id);
            return;
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
            await CancelOperation(user.Id);
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
                case TransferFlow transferFlow when transferFlow.Step == "recipient-select":
                    if (long.TryParse(query.Data, out long customerId))
                    {
                        var recipient = transferFlow.Customers!.FirstOrDefault(c => c.Id == customerId);
                        if (recipient is not null)
                        {
                            await _cache.SetAsync(
                                user.Id.ToString(),
                                FlowEnvelopeHelper.Wrap(
                                    transferFlow with
                                    {
                                        Step = "amount-enter",
                                        Recipient = recipient
                                    }
                                    )
                                );
                            await _bot.SendMessage(
                                user.Id,
                                $"Получатель: {recipient.ToString()}{Environment.NewLine}Введите сумму:",
                                replyMarkup: Menus.CancelMenu
                            );
                            return;
                        }
                    }
                    break;
                case TransferFlow transferFlow when transferFlow.Step == "confirm":
                    if (query.Data == "confirm")
                    {
                        if (transferFlow.Recipient is null || transferFlow.Amount is null || transferFlow.Description is null)
                        {
                            await _bot.SendMessage(
                                user.Id,
                                "Ошибка во время перевода. Пожалуйста, начните заново.",
                                replyMarkup: Menus.MainMenu
                            );
                            await _cache.RemoveAsync(user.Id.ToString());
                            return;
                        }

                        var transactionRequest = new TransactionRequest
                        {
                            SenderId = user.Id,
                            RecipientId = transferFlow.Recipient.Id,
                            Amount = transferFlow.Amount.Value,
                            Description = transferFlow.Description
                        };

                        var transferResult = await _apiClient.CreateTransactionAsync(transactionRequest);
                        if (transferResult!.Success)
                        {
                            await _bot.SendMessage(
                                user.Id,
                                $"Перевод успешно выполнен.{Environment.NewLine}" +
                                $"Получатель: {transferFlow.Recipient.ToString()}{Environment.NewLine}" +
                                $"Сумма: {transferFlow.Amount:N2}{Environment.NewLine}" +
                                $"Описание: {transferFlow.Description}",
                                replyMarkup: Menus.MainMenu
                            );

                            await _bot.SendMessage(
                                transferFlow.Recipient.Id,
                                $"Вам поступил перевод.{Environment.NewLine}" +
                                $"Отправитель: {transferFlow.Sender.ToString()}{Environment.NewLine}" +
                                $"Сумма: {transferFlow.Amount:N2}{Environment.NewLine}" +
                                $"Описание: {transferFlow.Description}",
                                replyMarkup: Menus.MainMenu
                            );
                        }
                        else
                        {
                            await _bot.SendMessage(
                                user.Id,
                                $"Ошибка при выполнении перевода: {transferResult.ErrorMessage}",
                                replyMarkup: Menus.MainMenu
                            );
                        }
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

    async Task CancelOperation(long userId)
    {
        await _cache.RemoveAsync(userId.ToString());
        await _bot.SendMessage(
            userId,
            "Действие отменено.",
            replyMarkup: Menus.MainMenu
        );
    }

    async Task<Customer> CreateOrUpdateCustomer(User user)
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

        var response = await _apiClient.CreateCustomerAsync(customer);

        return response!.Customer!;
    }

}