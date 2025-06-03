using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IGroqClient
{
    Task<GroqResponse?> GenerateEmailSummary(User user, string emailContent);
}

public class GroqClient : IGroqClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<GroqClient> _logger;

    private const string MODEL = "llama-3.1-8b-instant";
    private const string CHAT_URI = "https://api.groq.com/openai/v1/chat/completions";

    private const string SYSTEM_PROMPT =
"""
You are a highly efficient email summarization model for InboxWrap. You only respond with valid raw JSON. Do not include any explanation, comments, or formatting outside the JSON block.

Summarize the email in 1–2 very short sentences. Include only essential information. Clearly state if an action is required, or write "None".

You must return a complete, valid JSON object, exactly in this structure:

{
  "summary": "Very short summary, 1–2 sentences max.",
  "action_required": "Brief action or 'None'",
  "important": true|false
}

Importance Logic

Set "important": true if any of the following apply:
- The email involves financial activity (e.g. transactions, charges, bills, repayments).
- There is a deadline within 3 days.
- Urgent or time-sensitive action is needed.
- It relates to security, login issues, account access, or legal matters.
- The message suggests fraud, unauthorized activity, or verification is required.

Otherwise, set "important": false.

Always return valid JSON. Ensure all opening and closing braces and quotes are present. Do not include any text outside the JSON.
""";

    public GroqClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<GroqClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<GroqResponse?> GenerateEmailSummary(User user, string emailContent)
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
            User = user.Id.ToString(),
            Model = MODEL,
            Messages = [ 
                new GroqMessage()
                {
                    Role = "system",
                    Content = SYSTEM_PROMPT
                },
                new GroqMessage()
                {
                    Role = "user",
                    Content = emailContent
                },
            ],
            Temperature = 0.2M,
            MaxCompletionTokens = 512,
            ResponseFormat = new GroqFormat()
            {
                Type = "json_object"
            }
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
