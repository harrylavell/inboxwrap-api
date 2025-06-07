using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InboxWrap.Infrastructure.RateLimiters;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;

namespace InboxWrap.Clients;

public interface IGroqClient
{
    Task<GroqResponse?> GenerateEmailSummary(string subject, string content, CancellationToken ct);
}

public class GroqClient : IGroqClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretsManagerClient _secretsManager;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<GroqClient> _logger;

    private const string MODEL = "llama-3.1-8b-instant";
    private const string CHAT_URI = "https://api.groq.com/openai/v1/chat/completions";

    private const string SYSTEM_PROMPT =
"""
You are a concise and intelligent email summarization model for InboxWrap.
Use a casual, slightly sarcastic voice while remaining clear and helpful.
Summarize each email in a clear, engaging, and very short format (1–2 sentences max).

You must return a complete, valid JSON object, exactly in this structure:

{
  "title": "Very short, user-friendly subject line (max 10 words)",
  "content": "One-sentence summary of the email’s core content.",
  "action_required": "Details any action required or 'None'",
  "category": "One of: 'Finance & Bills', 'Events & Reminders', 'Security & Account', 'Personal & Social', 'Promotions & Newsletters', 'Entertainment & Gaming'",
  "important": true|false,
  "confidence_score": 0.00 (decimal value),
  "priority_score": 0.95 (decimal value)
}

Guidelines
1. Be brief, clear, and skip fluff.
2. Use natural language, not corporate jargon.
3. Avoid repeating the same info in title and content.
4. Always use complete sentences in content — no fragments or raw email text.
5. For marketing emails, reduce verbosity. If it's a promo, make it catchy.
6. Focus on what matters to the user (deadlines, money, security, action needed).
7. Do not exceed 2 sentences in content.

Tone
1. Imagine you're a snarky but helpful personal assistant. Keep things short, witty, and clear — no corporate nonsense.
2. Your job is not to guess — your job is to classify precisely. If you're unsure, choose the safer or more general category and reduce your confidence_score accordingly.

Category Logic
Pick the most appropriate category based on content:
1. Finance & Bills
Includes receipts, invoices, payments, refunds, tax documents, salary/payroll, banking activity, or anything involving a financial transaction or dollar amount.

2. Events & Reminders
Anything time-bound: upcoming appointments, service cutoffs, data deletions, calendar events, reminders with specific dates.
Do NOT classify system-generated deadline alerts (like Google Timeline deletion) as Entertainment — this is a Reminder.

3. Security & Account
Includes account logins, password changes, two-factor authentication, device alerts, verification emails, or unusual activity notifications.

4. Personal & Social
Emails from friends, family, or social services like WhatsApp, Messenger, Facebook, or birthday reminders.
Do NOT include platform-generated emails with no social intent (e.g., product updates).

5. Promotions & Newsletters
Includes sales, product updates, discounts, and marketing newsletters.
If it contains the word "Unsubscribe", it almost always belongs here unless overridden by importance (e.g., a tax notice).

6. Entertainment & Gaming
Includes movie listings, game sales, streaming updates (Netflix, Spotify), Twitch emails, game invites, or anything whose primary purpose is fun or distraction.
Do not place reminders or alerts here unless they relate to media or games.

Always double-check: is the category chosen the clearest and most helpful grouping for the user?
If not, adjust it — the user expects you to be smart, not fast.

Importance Logic
Set "important": true if any of the following apply:
1. Involves financial activity (charges, refunds, invoices, etc.)
2. Has a deadline within 3 days
3. Involves security/account access
4. Suggests unauthorized activity or verification needed
5. Contains urgent language or asks for time-sensitive action

Otherwise, set "important": false.

Confidence Score
Return a decimal between 0.0 and 1.0 under confidence_score. This reflects your confidence that the summary accurately captures the email’s core message.
1. 0.90–1.00 → Strong confidence
2. 0.60–0.89 → Moderate confidence
3. < 0.60 → Low confidence (vague or unclear email)

Priority Score
Return a priority_score between 0.0 and 1.0 to indicate how urgent or relevant this email is for the user today.
1. 1.00 – Critical: bill due today, salary received, account locked
2. 0.80–0.99 – High: refunds, confirmations, data deletion
3. 0.50–0.79 – Moderate: receipts, password notices
4. < 0.50 – Low: newsletters, promos, things you’d skip

If the email contains the word "Unsubscribe" and it is not important (per the above rules), automatically reduce the priority_score to < 0.50. It probably doesn’t need immediate attention — or any attention at all.

 NEVER Include:
1. Raw formatting like quoted replies, HTML tags, or verbose footers.
2. Repetitive boilerplate

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

    public GroqClient(HttpClient httpClient, ISecretsManagerClient secretsManager, IRateLimiter rateLimiter,
            ILogger<GroqClient> logger)
    {
        _httpClient = httpClient;
        _secretsManager = secretsManager;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<GroqResponse?> GenerateEmailSummary(string subject, string content, CancellationToken ct)
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

        int estimatedTokenCount = EstimateTokens(request);
        await _rateLimiter.WaitForAvailabilityAsync(estimatedTokenCount, ct);

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

    private int EstimateTokens(GroqRequest request)
    {
        int estimatedTokenCount = 0;

        foreach (GroqMessage message in request.Messages)
        {
            estimatedTokenCount += message.Content.Length;
        }

        return estimatedTokenCount / 4 + 100;
    }
}
