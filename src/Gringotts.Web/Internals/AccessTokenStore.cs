using Gringotts.Contracts.Interfaces;

namespace Gringotts.Web.Internals;

internal class AccessTokenStore(IBffClient bffClient)
{
    private string? _token;

    public string? Get()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        try
        {
            // Attempt to refresh token synchronously. RefreshAsync returns AuthResult.
            var result = bffClient.RefreshAsync().GetAwaiter().GetResult();
            if (result != null && result.Success && !string.IsNullOrEmpty(result.AccessToken))
            {
                _token = result.AccessToken;
            }
        }
        catch
        {
            // ignore refresh failures, return null
        }

        return _token;
    }

    public void Set(string? token) => _token = token;
    public bool HasToken => !string.IsNullOrEmpty(Get());
}