using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Interfaces;
using Gringotts.Shared.Enums;
using Gringotts.BFF.Internals;
using Gringotts.Contracts.Responses;

namespace Gringotts.BFF.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (IApiClient apiClient, HttpContext http, AuthRequest request, ICache cache, TokensService tokensService) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var result = await apiClient.CheckAccessCodeAsync(request.UserName, request.AccessCode);

            if (result.Success && result.EmployeeId.HasValue)
            {
                var (accessToken, refreshSession) = tokensService.CreateTokenPair(result.EmployeeId.Value, request.UserName, "Admin");

                // persist refresh session (rotation-safe)
                await cache.SetAsync(refreshSession.Token, refreshSession, refreshSession.ExpiresUtc - DateTimeOffset.UtcNow);

                // set refresh cookie (HttpOnly)
                http.Response.Cookies.Append("gr_rf", refreshSession.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict, // Strict/Lax; Strict is safest
                    Expires = refreshSession.ExpiresUtc
                });

                var resp = new AuthResponse
                {
                    ErrorCode = ErrorCode.None,
                    AccessToken = accessToken,
                    EmployeeId = result.EmployeeId
                };

                return Results.Ok(resp);
            }

            // Map API error codes to HTTP responses
            return result.ErrorCode switch
            {
                ErrorCode.ValidationError => Results.BadRequest(new { error = result.ErrorMessage }),
                ErrorCode.EmployeeNotFound => Results.NotFound(new { error = result.ErrorMessage }),
                ErrorCode.AuthenticationFailed => Results.Unauthorized(),
                _ => Results.Problem(detail: result.ErrorMessage.FirstOrDefault() ?? "An error occurred.")
            };

        }).WithName("BffLogin");

        app.MapPost("/auth/refresh", async (HttpContext http, TokensService tokensService, ICache cache) =>
        {
            if (!http.Request.Cookies.TryGetValue("gr_rf", out var oldToken))
                return Results.Unauthorized();

            var refreshSession = await cache.GetAsync<RefreshSession>(oldToken);
            if (refreshSession is null || refreshSession.RevokedUtc is not null || refreshSession.ExpiresUtc < DateTimeOffset.UtcNow)
                return Results.Unauthorized();

            // rotate refresh token
            var (access, newRefreshSession) = tokensService.CreateTokenPair(refreshSession.UserId, refreshSession.Username, refreshSession.Role);

            await cache.RemoveAsync(oldToken);
            await cache.SetAsync(newRefreshSession.Token, newRefreshSession, newRefreshSession.ExpiresUtc - DateTimeOffset.UtcNow);

            http.Response.Cookies.Append("gr_rf", newRefreshSession.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshSession.ExpiresUtc
            });

            var authResp = new AuthResponse
            {
                ErrorCode = ErrorCode.None,
                AccessToken = access,
                EmployeeId = refreshSession.UserId
            };

            return Results.Ok(authResp);

        }).WithName("BffRefresh");

        app.MapPost("/auth/logout", async (HttpContext http, ICache cache) =>
        {
            if (http.Request.Cookies.TryGetValue("gr_rf", out var token))
            {
                await cache.RemoveAsync(token);
                http.Response.Cookies.Delete("gr_rf", new CookieOptions { Secure = true, SameSite = SameSiteMode.Strict });
            }
            return Results.Ok();

        }).WithName("BffLogout");
    }
}
