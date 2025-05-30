using System.Net.Http.Headers;
using System.Text.Json;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IMicrosoftAzureClient
{
    Task<MicrosoftTokenResponse?> GetToken(string authorizationCode);

    Task<MicrosoftTokenResponse?> RefreshToken(string refreshToken);

    Task<MicrosoftMailboxSettingsResponse?> GetMailboxSettings(string accessToken);

    Task<MicrosoftMailResponse?> GetUnreadMail(string accessToken, string receivedDateTime);
}

public class MicrosoftAzureClient : IMicrosoftAzureClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<MicrosoftAzureClient> _logger;

    private const string TOKEN_URI = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

    public MicrosoftAzureClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<MicrosoftAzureClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<MicrosoftTokenResponse?> GetToken(string authorizationCode)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            throw new ArgumentException("Authorization code is required.", nameof(authorizationCode));
        }

        string clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
        string clientSecret = await _secretsManager.GetSecretAsync("AzureAdClientSecret");
        string redirectUri = await _secretsManager.GetSecretAsync("AzureAdRedirectUri");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)
                || string.IsNullOrWhiteSpace(redirectUri))
        {
            _logger.LogError("Missing Azure secret configuration.");
            throw new InvalidOperationException("Configuration error occurred.");
        }

        Dictionary<string, string> parameters = new()
        {
            { "scope", "https://graph.microsoft.com/Mail.Read Mail.ReadWrite MailboxSettings.Read offline_access openid profile email" },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" },
            { "code", authorizationCode },
        };

        FormUrlEncodedContent content = new(parameters);

        HttpResponseMessage response = await _httpClient.PostAsync(TOKEN_URI, content);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve Microsoft token. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            MicrosoftTokenResponse? tokenData = JsonSerializer.Deserialize<MicrosoftTokenResponse>(json);

            if (tokenData == null)
            {
                _logger.LogError("Deserialization of token response returned null.");
            }

            return tokenData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Microsoft token response.");
            return null;
        }
    }

    public async Task<MicrosoftTokenResponse?> RefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        string clientId = await _secretsManager.GetSecretAsync("AzureAdClientId");
        string clientSecret = await _secretsManager.GetSecretAsync("AzureAdClientSecret");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogError("Missing Azure secret configuration.");
            throw new InvalidOperationException("Configuration error occurred.");
        }

        Dictionary<string, string> parameters = new()
        {
            { "scope", "https://graph.microsoft.com/Mail.Read Mail.ReadWrite MailboxSettings.Read offline_access openid profile email" },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
        };

        FormUrlEncodedContent content = new(parameters);

        HttpResponseMessage response = await _httpClient.PostAsync(TOKEN_URI, content);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh Microsoft token. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            MicrosoftTokenResponse? tokenData = JsonSerializer.Deserialize<MicrosoftTokenResponse>(json);

            if (tokenData == null)
            {
                _logger.LogError("Deserialization of token response returned null.");
            }

            return tokenData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Microsoft token response.");
            return null;
        }
    }

    public async Task<MicrosoftMailboxSettingsResponse?> GetMailboxSettings(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("access token is required.", nameof(accessToken));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        string uri = "https://graph.microsoft.com/v1.0/me/mailboxSettings";

        HttpResponseMessage response = await _httpClient.GetAsync(uri);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve Microsoft mailbox settings. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            MicrosoftMailboxSettingsResponse? mailData = JsonSerializer.Deserialize<MicrosoftMailboxSettingsResponse>(json);

            if (mailData == null)
            {
                _logger.LogError("Deserialization of mailbox settings response returned null.");
            }

            return mailData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Microsoft mailbox settings response.");
            return null;
        }
    }

    public async Task<MicrosoftMailResponse?> GetUnreadMail(string accessToken, string receivedDateTime)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("access token is required.", nameof(accessToken));
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        string uri = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages";
        uri += $"?$filter=isRead eq false and receivedDateTime ge {receivedDateTime}&top=250";

        HttpResponseMessage response = await _httpClient.GetAsync(uri);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve Microsoft mail. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            MicrosoftMailResponse? mailData = JsonSerializer.Deserialize<MicrosoftMailResponse>(json);

            if (mailData == null)
            {
                _logger.LogError("Deserialization of mail response returned null.");
            }

            return mailData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Microsoft mail response.");
            return null;
        }
    }
}
