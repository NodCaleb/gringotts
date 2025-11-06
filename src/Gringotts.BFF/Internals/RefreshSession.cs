namespace Gringotts.BFF.Internals;

internal class RefreshSession
{
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public required string Role { get; init; }
    public DateTimeOffset IssuedUtc { get; init; }
    public DateTimeOffset ExpiresUtc { get; init; }
    public DateTimeOffset? RevokedUtc { get; set; }
}
