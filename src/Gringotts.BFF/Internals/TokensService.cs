using Gringotts.BFF.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Gringotts.BFF.Internals;

internal class TokensService
{
    private readonly JwtOptions _opt;
    public TokensService(JwtOptions opt) => _opt = opt;

    public (string access, RefreshSession refresh) CreateTokenPair(Guid userId, string username, string role)
    {
        var now = DateTimeOffset.UtcNow;

        // Access token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _opt.Issuer,
            Audience = _opt.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_opt.AccessMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

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

        return (tokenHandler.WriteToken(token), refresh);
    }
}