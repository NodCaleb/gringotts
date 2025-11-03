using Gringotts.Domain.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace Gringotts.Bot.Markup;

internal static class Menus
{
    public static ReplyMarkup MainMenu =>
    new ReplyKeyboardMarkup(new[]
    {
        new []
        {
            new KeyboardButton(Buttons.Balance),
            new KeyboardButton(Buttons.Transfer),
        },
        new []
        {
            new KeyboardButton(Buttons.TransactionsHistory),
            new KeyboardButton(Buttons.CharacterName)
        }
    })
    { ResizeKeyboard = true };

    public static ReplyMarkup CancelMenu =>
    new ReplyKeyboardMarkup(new[]
    {
        new []
        {
            new KeyboardButton(Buttons.Cancel),
        }
    })
    { ResizeKeyboard = true };

    public static ReplyMarkup ConfirmMenu =>
    new InlineKeyboardMarkup(new[]
    {
        new []
        {
            InlineKeyboardButton.WithCallbackData(Buttons.Confirm, "confirm"),
            InlineKeyboardButton.WithCallbackData(Buttons.Cancel, "cancel")
        }
    });

    public static ReplyMarkup ChooseCustomerMenu(IEnumerable<Customer> customers)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var customer in customers)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{customer.ToString()}",
                    $"{customer.Id}")
            });
        }
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(Buttons.Cancel, "cancel")
        });
        return new InlineKeyboardMarkup(buttons);
    }
}
