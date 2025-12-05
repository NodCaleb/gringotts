using System.Text.Json;
using Gringotts.Bot.Markup;
using Gringotts.Domain.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace Gringotts.Infrastructure.UnitTests;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    [Fact]
    public void MainMenu_SerializeDeserialize_RoundTrips()
    {
        var menu = Menus.MainMenu;
        var json = JsonSerializer.Serialize(menu, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<ReplyKeyboardMarkup>(json, JsonOpts);
        Assert.NotNull(deserialized);
        var originalElem = JsonSerializer.SerializeToElement(menu, JsonOpts);
        var deserializedElem = JsonSerializer.SerializeToElement(deserialized!, JsonOpts);
        Assert.Equal(originalElem.ToString(), deserializedElem.ToString());
    }

    [Fact]
    public void CancelMenu_SerializeDeserialize_RoundTrips()
    {
        var menu = Menus.CancelMenu;
        var json = JsonSerializer.Serialize(menu, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<ReplyKeyboardMarkup>(json, JsonOpts);
        Assert.NotNull(deserialized);
        var originalElem = JsonSerializer.SerializeToElement(menu, JsonOpts);
        var deserializedElem = JsonSerializer.SerializeToElement(deserialized!, JsonOpts);
        Assert.Equal(originalElem.ToString(), deserializedElem.ToString());
    }

    [Fact]
    public void ConfirmMenu_SerializeDeserialize_RoundTrips()
    {
        var menu = Menus.ConfirmMenu;
        var json = JsonSerializer.Serialize(menu, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<InlineKeyboardMarkup>(json, JsonOpts);
        Assert.NotNull(deserialized);
        var originalElem = JsonSerializer.SerializeToElement(menu, JsonOpts);
        var deserializedElem = JsonSerializer.SerializeToElement(deserialized!, JsonOpts);
        Assert.Equal(originalElem.ToString(), deserializedElem.ToString());
    }

    [Fact]
    public void ChooseCustomerMenu_SerializeDeserialize_RoundTrips()
    {
        var customers = new[] { new Customer { Id = 123, UserName = "@alice", PersonalName = "Alice" } };
        var menu = Menus.ChooseCustomerMenu(customers);
        var json = JsonSerializer.Serialize(menu, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<InlineKeyboardMarkup>(json, JsonOpts);
        Assert.NotNull(deserialized);
        var originalElem = JsonSerializer.SerializeToElement(menu, JsonOpts);
        var deserializedElem = JsonSerializer.SerializeToElement(deserialized!, JsonOpts);
        Assert.Equal(originalElem.ToString(), deserializedElem.ToString());
    }
}
