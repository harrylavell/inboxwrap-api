using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InboxWrap.Configuration;
using InboxWrap.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InboxWrap.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly JwtConfig _config;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IOptions<JwtConfig> config, ILogger<TokenService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        try
        {
            string jwtSecret = _config.Secret;

            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email.ToString())
            };

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtSecret));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://id.inboxwrap.com",
                audience: "user",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError("Token generation failed for user with email {Email}: {Message}", user.Email, ex.Message);
            return string.Empty;
        }
    }

}
