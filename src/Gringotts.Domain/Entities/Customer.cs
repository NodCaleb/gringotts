namespace Gringotts.Domain.Entities;

public class Customer
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PersonalName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
