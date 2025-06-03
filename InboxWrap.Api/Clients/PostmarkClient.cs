using System.Text;
using System.Text.Json;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IPostmarkClient
{
    Task<PostmarkResponse?> SendSummaryEmail();
}

public class PostmarkClient : IPostmarkClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<PostmarkClient> _logger;

    private const string TEMPLATE_URI = "https://api.postmarkapp.com/email/withTemplate";

    public PostmarkClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<PostmarkClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<PostmarkResponse?> SendSummaryEmail()
    {
        string apiToken = await _secretsManager.GetSecretAsync("PostmarkApiToken");

        if (string.IsNullOrWhiteSpace(apiToken))
        {
            _logger.LogError("Missing Postmark secret configuration.");
            throw new InvalidOperationException("Configuration error occurred.");
        }
        
        _httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", apiToken);

        // Prepare request content
        PostmarkRequest request = new()
        {
            TemplateId = "40277619",
            TemplateModel = new DailySummaryTemplateModel()
            {

            },
            From = "testing@inboxwrap.com",
            To = "hallan@outlook.com.au",
        };

        string jsonRequest = JsonSerializer.Serialize(request);
        StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(TEMPLATE_URI, content);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send summary email. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            PostmarkResponse? postmarkData = JsonSerializer.Deserialize<PostmarkResponse>(json);

            if (postmarkData == null)
            {
                _logger.LogError("Deserialization of Postmark response returned null.");
            }

            return postmarkData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Postmark response.");
            return null;
        }
    }
}
