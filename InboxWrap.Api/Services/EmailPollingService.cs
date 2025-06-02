using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface IEmailPollingService
{
    Task PollUsersForNewEmailsAsync(CancellationToken cancellationToken);
}

public class EmailPollingService : IEmailPollingService
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connectedAccounts;
    private readonly ISummaryRepository _summaries;
    private readonly IMicrosoftAzureClient _microsoftClient;
    private readonly IGroqClient _groqClient;
    private readonly ILogger<EmailPollingService> _logger;

    public EmailPollingService(IUserRepository users, IConnectedAccountRepository connectedAccounts,
            ISummaryRepository summaries, IMicrosoftAzureClient microsoftClient, IGroqClient groqClient,
            ILogger<EmailPollingService> logger)
    {
        _users = users;
        _connectedAccounts = connectedAccounts;
        _summaries = summaries;
        _microsoftClient = microsoftClient;
        _groqClient = groqClient;
        _logger = logger;
    }

    public async Task PollUsersForNewEmailsAsync(CancellationToken cancellationToken)
    {
        DateTime pollStartUtc = DateTime.UtcNow;
        //DateTime receivedCutoffUtc = pollStartUtc.AddMinutes(-5);
        DateTime receivedCutoffUtc = pollStartUtc.AddHours(-12);

        IEnumerable<User> users = _users.GetAll();

        foreach (User user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (ConnectedAccount connectedAccount in user.ConnectedAccounts)
            {
                string? accessToken = (connectedAccount.AccessTokenExpiryUtc <= DateTime.UtcNow)
                    ? accessToken = await UpdateAccessToken(user, connectedAccount)
                    : connectedAccount.AccessToken; // TODO: Encrypt/decrypt

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    // TODO: Improve error message
                    _logger.LogError("Skipping connected account as we can't retrieve a valid access token.");
                    continue;
                }

                if (connectedAccount.Provider == "Microsoft")
                {
                    // Calculate the date time IOS 8601 format as required by Graph API
                    string receivedDateTime = receivedCutoffUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                    MicrosoftMailResponse? mailResponse = await _microsoftClient.GetUnreadMail(accessToken, receivedDateTime);

                    if (mailResponse == null)
                    {
                        _logger.LogError("Unable to retrieve unread mail for connected account.");
                        continue;
                    }

                    IEnumerable<MicrosoftMessage> messages = mailResponse.Messages;
                    _logger.LogInformation($"Retrieved {messages.Count()} messages.");

                    foreach (MicrosoftMessage message in messages)
                    {
                        string messageId = message.Id;
                        string content = message.Body.Content;

                        // TODO: (UPDATER) Remove HTML
                        if (message.Body.ContentType == "html")
                        {
                            HtmlDocument doc = new();
                            doc.LoadHtml(message.Body.Content);
                            content = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
                        }
                        
                        // TODO: (UPDATER) Remove WhiteSpace and New Lines
                        content = Regex.Replace(content, @"\s{2,}", " ");
                        
                        // TODO: (UPDATER) Truncate to 1000 characters
                        if (content.Length > 1000)
                        {
                            content = content.Substring(0, 1000);
                        }

                        // Generate summary
                        GroqResponse? groq = await _groqClient.GenerateEmailSummary(content);

                        if (groq == null)
                        {
                            _logger.LogError("Groq response is null or empty.");
                            continue;
                        }

                        string? responseContent = groq.Choices.FirstOrDefault()?.Message.Content;

                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            _logger.LogError("Groq response content is null or empty.");
                            continue;
                        }

                        SummaryContent? summaryContent;
                        try
                        {
                            summaryContent = JsonSerializer.Deserialize<SummaryContent>(responseContent!);
                            if (summaryContent == null)
                            {        
                                _logger.LogError($"Deserialization of summary content returned null. Raw content: {responseContent}");
                                continue;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, $"Deserialization of summary content returned null. Raw content: {responseContent}");
                            continue;
                        }

                        Summary summary = new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            User = user,
                            MessageId = messageId,
                            Source = "Microsoft",
                            Content = summaryContent,
                            GenerationMetadata = new SummaryGenerationMetadata()
                            {
                                Provider = "Groq",
                                RequestId = groq.XGroq.Id,
                                InputTokens = groq.Usage.PromptTokens,
                                OutputTokens = groq.Usage.CompletionTokens,
                                TimeTaken = groq.Usage.TotalTime
                            }
                        };

                        await _summaries.AddAsync(summary);
                        await _summaries.SaveChangesAsync();
                    }
                }

                if (connectedAccount.Provider == "Google")
                {

                }
            }
        }
    }

    private async Task<string?> UpdateAccessToken(User user, ConnectedAccount connectedAccount)
    {
        string refreshToken = connectedAccount.RefreshToken; // TODO: Encrypt/decrypt

        if (connectedAccount.Provider == "Microsoft") // TODO: Fix casing
        {
            MicrosoftTokenResponse? tokenData = await _microsoftClient.RefreshToken(refreshToken);
            if (string.IsNullOrWhiteSpace(tokenData?.AccessToken) || string.IsNullOrWhiteSpace(tokenData?.RefreshToken))
            {
                _logger.LogError(""); // TODO: Add good error message
                return null;
            }

            connectedAccount.AccessToken = tokenData.AccessToken; // TODO: Encrypt/decrypt
            connectedAccount.RefreshToken = tokenData.RefreshToken; // TODO: Encrypt/decrypt
            connectedAccount.AccessTokenExpiryUtc = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

            _connectedAccounts.Update(connectedAccount);

            if (!await _connectedAccounts.SaveChangesAsync())
            {
                // TODO: Better logging to record which connected account also, stuctured maybe? LogTags, etc
                _logger.LogError("Failed to update connected account for user {UserId}", user.Id);
                return null;
            }

            // Update with new access token
            return tokenData.AccessToken;
        }

        if (connectedAccount.Provider == "Google")
        {

        }

        return null;
    }
}
