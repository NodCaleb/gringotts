namespace Gringotts.BFF.Configuration;

public record JwtOptions
{
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public string SigningKey { get; init; } = default!;  // store in Key Vault; 256-bit+
    public int AccessMinutes { get; init; } = 10;
    public int RefreshDays { get; init; } = 30;
}
