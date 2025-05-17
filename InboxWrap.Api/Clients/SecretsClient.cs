using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using InboxWrap.Configuration;
using InboxWrap.Models.Reponses;

namespace InboxWrap.Clients;

public class SecretsClient
{
    private readonly HashiCorpConfig _config;
    private readonly ILogger<SecretsClient> _logger;

    private readonly string _clientId;
    private readonly string _clientSecret;

    private const string TOKEN_URL = "https://auth.idp.hashicorp.com/oauth2/token";
    private const string SECRETS_URL = "https://api.cloud.hashicorp.com/secrets/2023-11-28/organizations/4b42715d-23fc-49f4-a8c7-1f50cb403dd5/projects/33781194-6bad-4b92-93d5-868efeeb23fa/apps/api/secrets:open";

    public SecretsClient(IOptions<HashiCorpConfig> config, ILogger<SecretsClient> logger)
    {
        _config = config.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_config.ClientId))
        {
            throw new ArgumentNullException("Failed to retrieve ClientId from configuration.");
        }

        if (string.IsNullOrEmpty(_config.ClientSecret))
        {
            throw new ArgumentNullException("Failed to retrieve ClientSecret from configuration.");
        }

        _clientId = _config.ClientId;
        _clientSecret = _config.ClientSecret;
    }

    public async Task<Secret?> GetSecret(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogError("You must specify the secret to be retrieved.");
            return null;
        }

        IEnumerable<Secret>? secrets = await GetSecrets();
        
        return secrets?.Where(s => s.Name == name).FirstOrDefault();
    }

    public async Task<IEnumerable<Secret>?> GetSecrets()
    {
        try
        {
            string? token = await GetToken();

            if (token == null)
            {
                _logger.LogError("No secrets retrieved due failed retrieval of access token.");
                return null;
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, SECRETS_URL);

            using HttpResponseMessage response = await client.SendAsync(request);

            if (response != null)
            {
                string json = await response.Content.ReadAsStringAsync();
                SecretsResponse? secretsResponse = JsonSerializer.Deserialize<SecretsResponse>(json);

                if (secretsResponse == null || secretsResponse.Secrets == null)
                {
                    _logger.LogError("Failed to retrieve secrets from secrets manager.");
                    return null;
                }

                return secretsResponse.Secrets;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred retrieving secrets from secrets manager: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetToken()
    {
        try
        {
            string token = string.Empty;

            List<KeyValuePair<string, string>> content = [
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("audience", "https://api.hashicorp.cloud"),
            ];

            using HttpClient client = new HttpClient();

            using HttpRequestMessage request = new(HttpMethod.Post, TOKEN_URL)
            {
                Content = new FormUrlEncodedContent(content)
            };

            using HttpResponseMessage response = await client.SendAsync(request);

            if (response != null)
            {
                string json = await response.Content.ReadAsStringAsync();
                AccessTokenResponse? tokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(json);

                if (tokenResponse == null)
                {
                    _logger.LogError("Failed to retrieve access token from secrets manager.");
                    return null;
                }

                token = tokenResponse.AccessToken;
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred retrieving access token from secrets manager: {ex.Message}");
            return null;
        }
    }

}
