using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IGroqClient
{
    Task<GroqResponse?> GenerateEmailSummary(string emailContent);
}

public class GroqClient : IGroqClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<GroqClient> _logger;

    private const string BASE_MODEL = "llama3-8b-8192";
    private const string CHAT_URI = "https://api.groq.com/openai/v1/chat/completions";

    public GroqClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<GroqClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<GroqResponse?> GenerateEmailSummary(string emailContent)
    {
        if (string.IsNullOrWhiteSpace(emailContent))
        {
            throw new ArgumentException("Email content is required.", nameof(emailContent));
        }

        string apiKey = await _secretsManager.GetSecretAsync("GroqApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Missing Groq secret configuration.");
            throw new InvalidOperationException("Configuration error occurred.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Prepare request content
        GroqRequest request = new()
        {
            Model = BASE_MODEL,
            Messages = [ 
                new GroqMessage() {
                    Role = "system",
                    Content = ""
                },
                new GroqMessage() {
                    Role = "user",
                    Content = emailContent
                },
            ],
            Temperature = 0.2M,
            MaxCompletionTokens = 512
        };

        string jsonRequest = JsonSerializer.Serialize(request);
        StringContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(CHAT_URI, content);
        string json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to generate email summary. Status: {Status}. Response: {Body}", response.StatusCode, json);
            return null;
        }

        try
        {
            GroqResponse? groqData = JsonSerializer.Deserialize<GroqResponse>(json);

            if (groqData == null)
            {
                _logger.LogError("Deserialization of Groq response returned null.");
            }

            return groqData;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Groq response.");
            return null;
        }
    }
}
