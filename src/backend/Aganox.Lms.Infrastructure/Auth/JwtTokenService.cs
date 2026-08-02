using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aganox.Lms.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aganox.Lms.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly IDateTimeProvider _clock;

    public JwtTokenService(IOptions<JwtSettings> settings, IDateTimeProvider clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public string CreateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        out DateTime expiresAt)
    {
        expiresAt = _clock.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: _clock.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
