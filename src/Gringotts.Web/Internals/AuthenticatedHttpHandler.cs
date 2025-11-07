using Gringotts.Contracts.Responses;
using System.Text.Json;
using System.Threading;

namespace Gringotts.Web.Internals;


internal class AuthenticatedHttpHandler : DelegatingHandler
{
    private readonly AccessTokenStore _store;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthenticatedHttpHandler(AccessTokenStore store, IHttpClientFactory httpClientFactory)
    {
        _store = store; _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_store.HasToken)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _store.Get());

        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var ok = await TryRefreshAsync(ct);
            if (ok)
            {
                // retry once
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _store.Get());
                response.Dispose();
                return await base.SendAsync(request, ct);
            }
        }
        return response;
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("GringottsAuthClient");
        var res = await client.PostAsync("/auth/refresh", content: null, ct);
        if (res.IsSuccessStatusCode) 
        {
            var authResp = await res.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, ct).ConfigureAwait(false);
            if (authResp is not null)
            {
                _store.Set(authResp.AccessToken);
                return true;
            }
        }        

        _store.Set(null);
        return false;
    }
}
