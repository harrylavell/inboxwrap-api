using System.Text.Json;
using InboxWrap.Models.Reponses;

namespace InboxWrap.Clients;

public interface IMicrosoftAzureClient
{
    Task<TokenResponse?> GetAuthorizationToken(Dictionary<string, string> tokenParameters);
}

public class MicrosoftAzureClient : IMicrosoftAzureClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<MicrosoftAzureClient> _logger;

    private const string AZURE_TOKEN_URI = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

    public MicrosoftAzureClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<MicrosoftAzureClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<TokenResponse?> GetAuthorizationToken(Dictionary<string, string> tokenParameters)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(
                    AZURE_TOKEN_URI,
                    new FormUrlEncodedContent(tokenParameters));

            string? responseContent = await response.Content.ReadAsStringAsync();
            TokenResponse? tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve authorization token from Azure: {ex}", ex.Message);
            return null;
        }
    }
}
