using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Interfaces;
using Gringotts.Shared.Enums;
using System.Text;

namespace Gringotts.BFF.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (IApiClient apiClient, HttpContext http, AuthRequest request) =>
        {
            if (request == null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var result = await apiClient.CheckAccessCodeAsync(request.UserName, request.AccessCode);

            if (result.Success)
            {
                // Create a simple auth cookie (value is base64 encoded username). In real app use secure tokens.
                var cookieValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(result.EmployeeId?.ToString() ?? request.UserName));
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = http.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(8),
                    Path = "/"
                };

                http.Response.Cookies.Append("gringotts_auth", cookieValue, options);

                return Results.Ok(new { message = "Authenticated" });
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
    }
}
