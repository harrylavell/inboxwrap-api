using System.IdentityModel.Tokens.Jwt;
using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Reponses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface IProviderService
{
    Task<Result<string>> GenerateAzureConsentUrl(string userId);
    
    Task<Result> SetupConnectedAccount(string code, string state);
}

public class ProviderService : IProviderService
{
    private const string AZURE_AUTHORIZE_URI = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    private const string AZURE_SCOPES_URI = "https://graph.microsoft.com/";

    private readonly IMicrosoftAzureClient _azureClient;
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connectedAccounts;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IMicrosoftAzureClient azureClient, IUserRepository users, IConnectedAccountRepository connectedAccounts, ISecretsManagerClient secretsManager, ILogger<ProviderService> logger)
    {
        _azureClient = azureClient;
        _users = users;
        _connectedAccounts = connectedAccounts;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<Result<string>> GenerateAzureConsentUrl(string userId)
    {
        string scopes = "Mail.Read Mail.ReadWrite offline_access openid profile email";
        string state = $"{userId}&{Guid.NewGuid().ToString()}";

        string clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
        string redirectUri = await _secretsManager.GetSecretAsync("AzureAdRedirectUri");

        string authUrl = AZURE_AUTHORIZE_URI +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_mode=query" +
            $"&scope={Uri.EscapeDataString(AZURE_SCOPES_URI+scopes)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Result<string>.Ok(authUrl);
    }

    public async Task<Result> SetupConnectedAccount(string code, string state)
    {
        string clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
        string clientSecret = await _secretsManager.GetSecretAsync("AzureAdClientSecret");
        string redirectUri = await _secretsManager.GetSecretAsync("AzureAdRedirectUri");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)
                || string.IsNullOrWhiteSpace(redirectUri))
        {
            _logger.LogError("Missing Azure secret configuration.");
            return Result.Fail("Configuration error occurred.");
        }

        // Extract user ID from state
        string[] parts = state.Split('&');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid userId))
        {
            _logger.LogWarning("Invalid state information: {State}", state);
            return Result.Fail("Invalid state information.");
        }

        Dictionary<string, string> tokenParameters = new()
        {
            { "client_id", clientId },
            { "scope", AZURE_SCOPES_URI + "Mail.Read Mail.ReadWrite offline_access openid profile email" },
            { "code", code },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" },
            { "client_secret", clientSecret },
        };

        // Retrieve the authorization token from Microsoft
        TokenResponse? token = await _azureClient.GetAuthorizationToken(tokenParameters);
        if (token?.IdToken == null || string.IsNullOrWhiteSpace(token.AccessToken)
                || string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            _logger.LogWarning("Failed to retrieve authorization token.");
            return Result.Fail("Unable to connect to your Microsoft account at this time.");
        }

        // Extract id_token info
        IdTokenInfo idTokenInfo;
        try
        {
            idTokenInfo = ExtractIdTokenInfo(token.IdToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract id_token info.");
            return Result.Fail("Invalid login information received.");
        }

        User? user = await _users.GetByIdAsync(userId);
        if (user == null)
        {
            return Result.Fail("Associated user not found.");
        }

        ConnectedAccount connectedAccount = new()
        {
            UserId = user.Id,
            User = user,
            ProviderUserId = idTokenInfo.Sub,
            Provider = Providers.Microsoft.ToString(),
            Name = idTokenInfo.Name,
            Email = idTokenInfo.Email,
            AccessToken = token.AccessToken!, // TODO: Encrypt value before DB insert
            RefreshToken = token.RefreshToken!, // TODO: Encrypt value before DB insert
            AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn),
        };

        await _connectedAccounts.AddAsync(connectedAccount);

        if (!await _connectedAccounts.SaveChangesAsync())
        {
            _logger.LogWarning("Failed to save connected account for user {UserId}", user.Id);
            return Result.Fail("Unable to save connected account.");
        }

        return Result.Ok();
    }

    private IdTokenInfo ExtractIdTokenInfo(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentNullException();
        }

        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken token = handler.ReadJwtToken(idToken);

        string sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value!;
        string name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "";
        string email = token.Claims.FirstOrDefault(c =>
            c.Type == "email" || c.Type == "preferred_username")?.Value!;

        if (string.IsNullOrWhiteSpace(sub))
        {
            _logger.LogError("Missing 'sub' claim in id_token.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogError("Failed to extract email from id_token.");
        }

        return new IdTokenInfo()
        {
            Sub = sub,
            Name = name,
            Email = email,
        };
    }
}
