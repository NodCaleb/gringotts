namespace Gringotts.Web.Internals;

internal class AccessTokenStore
{
    private string? _token;
    public string? Get() => _token;
    public void Set(string? token) => _token = token;
    public bool HasToken => !string.IsNullOrEmpty(_token);
}