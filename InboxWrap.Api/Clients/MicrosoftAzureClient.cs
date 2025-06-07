using System.Net.Http.Headers;
using System.Text.Json;
using InboxWrap.Models;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IMicrosoftAzureClient
{
    Task<Result<MicrosoftTokenResponse>> GetToken(string authorizationCode);

    Task<Result<MicrosoftTokenResponse>> RefreshToken(string refreshToken);

    Task<Result<MicrosoftMailboxSettingsResponse>> GetMailboxSettings(string accessToken);

    Task<Result<MicrosoftMailResponse>> GetUnreadMail(string accessToken, string receivedDateTime);
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

    public async Task<Result<MicrosoftTokenResponse>> GetToken(string authorizationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode, nameof(authorizationCode));

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

        return await ParseJsonResponse<MicrosoftTokenResponse>(response, "Microsoft Token");
    }

    public async Task<Result<MicrosoftTokenResponse>> RefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

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

        return await ParseJsonResponse<MicrosoftTokenResponse>(response, "Microsoft Token");
    }

    public async Task<Result<MicrosoftMailboxSettingsResponse>> GetMailboxSettings(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));

        string uri = "https://graph.microsoft.com/v1.0/me/mailboxSettings";

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await _httpClient.SendAsync(request);

        return await ParseJsonResponse<MicrosoftMailboxSettingsResponse>(response, "Mailbox Settings");
    }

    public async Task<Result<MicrosoftMailResponse>> GetUnreadMail(string accessToken, string receivedDateTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(receivedDateTime, nameof(receivedDateTime));

        string uri = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages";
        uri += $"?$filter=isRead eq false and receivedDateTime ge {receivedDateTime}&top=250";
        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await _httpClient.SendAsync(request);

        return await ParseJsonResponse<MicrosoftMailResponse>(response, "Unread Mail");
    }

    private async Task<Result<T>> ParseJsonResponse<T>(HttpResponseMessage response, string context) where T : class
    {
        ArgumentNullException.ThrowIfNull(response, nameof(response));
        ArgumentException.ThrowIfNullOrWhiteSpace(context, nameof(context));

        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to retrieve {Context}. Status: {Status}. Response: {Body}", context, response.StatusCode, json);
            return Result<T>.Fail($"Request failed with status {(int)response.StatusCode} ({response.StatusCode}).");
        }

        try
        {
            T? data = JsonSerializer.Deserialize<T>(json);
            if (data == null)
            {
                _logger.LogError("Deserialization of {Context} returned null.", context);
                return Result<T>.Fail("Deserialization failed.");
            }

            return Result<T>.Ok(data);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize {Context} response.", context);
            return Result<T>.Fail("Deserialization failed.");
        }
    }
}
