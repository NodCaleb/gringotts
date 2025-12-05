namespace Gringotts.Contracts.Commands;

public record TelegramSendCommand
(
    long ChatId,
    string Message,
    string MenuMarkupJson = "",
    string MenuMarkupType = "ReplyKeyboard"
);
