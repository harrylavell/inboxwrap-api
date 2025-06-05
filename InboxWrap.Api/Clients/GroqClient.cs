using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IGroqClient
{
    Task<GroqResponse?> GenerateEmailSummary(User user, string subject, string content);
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
You are a highly efficient email summarization model for InboxWrap.

You only respond with valid raw JSON. Do not include any explanation, comments, or formatting outside the JSON block.

Summarize the email in 1–2 very short sentences. Include only essential information. Clearly state if an action is required, or write "None".

You must return a complete, valid JSON object, exactly in this structure:

{
  "title": "Title for the summary.",
  "content": "Very short summary, 1–2 sentences max.",
  "action_required": "Details any action required.",
  "category": "One of: 'Finance & Bills', 'Events & Reminders', 'Security & Account', 'Personal & Social', 'Promotions & Newsletters'",
  "important": true|false
  "confidence_score": 0.00 (decimal value)
  "priority_score": 0.95 (decimal value)
}

Category Logic
Pick the most appropriate category based on content:
1. Finance & Bills: Payments, receipts, subscriptions, bank stuff, anything with a $.
2. Events & Reminders: Appointments, calendar invites, flight reminders, countdowns to awkward family dinners.
3. Security & Account: Login alerts, password changes, account verifications, “someone tried to hack you” vibes.
4. Personal & Social: Emails from people who know you exist—friends, family, or social platforms.
5. Promotions & Newsletters: Shameless self-promo, deals, product updates, and borderline spam (but in a fun way).

Importance Logic
Set "important": true if any of the following apply:
1. The email involves financial activity (e.g. transactions, charges, bills, repayments).
2. There is a deadline within 3 days.
3. Urgent or time-sensitive action is needed.
4. It relates to security, login issues, account access, or legal matters.
5. The message suggests fraud, unauthorized activity, or verification is required.

Otherwise, set "important": false.

Confidence Score
Return a decimal between 0.0 and 1.0 under confidence_score. This reflects your confidence that the summary accurately captures the email’s core message.
1. 0.90–1.00 → Strong confidence
2. 0.60–0.89 → Moderate confidence
3. < 0.60 → Low confidence (vague or unclear email)

Priority Score
Return a priority_score between 0.0 and 1.0 to indicate how urgent or relevant this email is for the user today.
1. 1.00 → Critical (e.g. bill due today, salary received, security alert)
2. 0.80–0.99 → High (e.g. payment confirmation, refund issued)
3. 0.50–0.79 → Moderate (e.g. receipt, account notice)
4. < 0.50 → Low (e.g. newsletter, promo, no action needed)

If the email contains the word "Unsubscribe" and it is not important (per the above rules), automatically reduce the priority_score to < 0.50. It probably doesn’t need immediate attention — or any attention at all.

Always return valid JSON.
Do not include any extra text, formatting, or comments outside the JSON object.
All keys and string values must be properly quoted. All decimal values must be between 0.0 and 1.0.
""";

    // Professional
    //private const string TONE_PROMPT = "Use clear, concise, and neutral language. Avoid slang, contractions, or humor. Prioritize clarity and a respectful tone suitable for business communication.";

    // Fun
    private const string TONE_PROMPT = "Use witty, playful language with a touch of sarcasm. Embrace informal expressions, clever phrasing, and a lighthearted tone. Be entertaining, but still communicate the key point.";

    // Casual
    //private const string TONE_PROMPT = "Use a friendly, conversational tone that feels relaxed but still clear and respectful. Avoid corporate jargon and overly formal phrasing, but don’t veer into sarcasm or silliness. Write like a smart friend explaining something simply.";

    public GroqClient(HttpClient httpClient, ISecretsManagerClient secretsManager, ILogger<GroqClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _logger = logger;
    }

    public async Task<GroqResponse?> GenerateEmailSummary(User user, string subject, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

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
                    Content = $"Email Subject: {subject}"
                },
                new GroqMessage()
                {
                    Role = "user",
                    Content = content
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
        StringContent jsonContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(CHAT_URI, jsonContent);
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
