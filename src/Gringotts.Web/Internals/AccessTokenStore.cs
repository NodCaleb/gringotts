using Gringotts.Contracts.Interfaces;

namespace Gringotts.Web.Internals;

internal class AccessTokenStore(IBffClient bffClient)
{
    private string? _token;

    public async Task<string?> GetAsync()
    {
        if (!string.IsNullOrEmpty(_token))
            return _token;

        try
        {
            var result = await bffClient.RefreshAsync().ConfigureAwait(false);
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

    public async Task<bool> HasTokenAsync()
    {
        var t = await GetAsync().ConfigureAwait(false);
        return !string.IsNullOrEmpty(t);
    }
}