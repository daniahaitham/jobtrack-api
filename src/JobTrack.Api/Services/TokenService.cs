using System.Security.Claims;
using System.Text;
using JobTrack.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace JobTrack.Api.Services;

/// <summary>
/// Creates JWT tokens. A JWT is a signed "ticket" the client sends with each request
/// (Authorization: Bearer ...) to prove who it is, so we don't ask for the password every time.
/// </summary>
public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("uid", user.Id.ToString()),
                new Claim("email", user.Email)
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }
}
