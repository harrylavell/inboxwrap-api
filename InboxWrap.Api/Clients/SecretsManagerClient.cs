using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using InboxWrap.Configuration;
using InboxWrap.Models.Reponses;
using Microsoft.Extensions.Caching.Memory;

namespace InboxWrap.Clients;

public interface ISecretsManagerClient
{
    public Task<string> GetSecretAsync(string name);
    
    public Task<IEnumerable<Secret>?> GetSecretsAsync();
}

public class SecretsManagerClient : ISecretsManagerClient
{
    private readonly HashiCorpConfig _config;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretsManagerClient> _logger;

    private readonly string _clientId;
    private readonly string _clientSecret;

    private const string TOKEN_URI = "https://auth.idp.hashicorp.com/oauth2/token";
    private const string SECRETS_URI = "https://api.cloud.hashicorp.com/secrets/2023-11-28/organizations/4b42715d-23fc-49f4-a8c7-1f50cb403dd5/projects/33781194-6bad-4b92-93d5-868efeeb23fa/apps/api/secrets:open";

    public SecretsManagerClient(IOptions<HashiCorpConfig> config, HttpClient httpClient, IMemoryCache cache, ILogger<SecretsManagerClient> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _cache = cache;
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

    public async Task<string> GetSecretAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogError("You must specify the secret to be retrieved.");
            throw new ArgumentNullException(nameof(name), name);
        }

        IEnumerable<Secret>? secrets = await GetSecretsAsync();
        
        Secret secret = secrets!.Where(s => s.Name == name).FirstOrDefault()!;

        return secret.StaticVersion?.Value!;
    }

    public async Task<IEnumerable<Secret>?> GetSecretsAsync()
    {
        try
        {
            if (_cache.TryGetValue("secrets", out IEnumerable<Secret>? cachedSecrets))
            {
                _logger.LogInformation("Retrieved cached secrets");
                return cachedSecrets;
            }

            string? token = await GetTokenAsync();

            if (token == null)
            {
                _logger.LogError("No secrets retrieved due failed retrieval of access token.");
                return null;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, SECRETS_URI);

            using HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response != null)
            {
                string json = await response.Content.ReadAsStringAsync();
                SecretsResponse? secretsResponse = JsonSerializer.Deserialize<SecretsResponse>(json);

                if (secretsResponse == null || secretsResponse.Secrets == null)
                {
                    _logger.LogError("Failed to retrieve secrets from secrets manager.");
                    return null;
                }

                _cache.Set("secrets", secretsResponse.Secrets, TimeSpan.FromHours(24));

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

    private async Task<string?> GetTokenAsync()
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

            using HttpRequestMessage request = new(HttpMethod.Post, TOKEN_URI)
            {
                Content = new FormUrlEncodedContent(content)
            };

            using HttpResponseMessage response = await _httpClient.SendAsync(request);

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
