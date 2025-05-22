using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InboxWrap.Clients;
using InboxWrap.Models;
using Microsoft.IdentityModel.Tokens;

namespace InboxWrap.Services;

public interface ITokenService
{
    Task<string?> Generate(User user);
}

public class TokenService : ITokenService
{
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<TokenService> _logger;

    public TokenService(ISecretsManagerClient secretsManager, ILogger<TokenService> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<string?> Generate(User user)
    {
        string? jwtSecret = await _secretsManager.GetSecretAsync("JwtSecretKey");

        if (jwtSecret is null)
        {
            _logger.LogError("Unable to generate JSON Web Token (JWT).");
            return null;
        }

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
}
