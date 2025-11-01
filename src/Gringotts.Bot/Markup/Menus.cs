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
            new KeyboardButton(Buttons.NewPayment),
        },
        new []
        {
            new KeyboardButton(Buttons.TransactionsHistory),
            new KeyboardButton(Buttons.CharacterName),
        }
    })
    { ResizeKeyboard = true };
}
