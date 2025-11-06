using Gringotts.BFF.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Gringotts.BFF.Internals;

internal class TokensService
{
    private readonly JwtOptions _opt;
    public TokensService(JwtOptions opt) => _opt = opt;

    public (string access, RefreshSession refresh) CreateTokenPair(string userId, string username, string role)
    {
        var now = DateTimeOffset.UtcNow;

        // Access token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_opt.AccessMinutes).UtcDateTime,
            signingCredentials: creds);

        var access = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

        // Refresh token (opaque random)
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refresh = new RefreshSession
        {
            Token = refreshToken,
            UserId = userId,
            Username = username,
            Role = role,
            IssuedUtc = now,
            ExpiresUtc = now.AddDays(_opt.RefreshDays)
        };

        return (access, refresh);
    }
}