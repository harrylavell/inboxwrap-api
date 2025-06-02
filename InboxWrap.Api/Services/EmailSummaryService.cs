using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface IEmailSummaryService
{
    Task PollUsersForNewEmailsAsync();

    Task SendScheduledSummariesAsync();
    
    Task Run();
}

public class EmailSummaryService : IEmailSummaryService
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connectedAccounts;
    private readonly IMicrosoftAzureClient _microsoftClient;
    private readonly IGroqClient _groqClient;
    private readonly ILogger<EmailSummaryService> _logger;

    public EmailSummaryService(IUserRepository users, IConnectedAccountRepository connectedAccounts,
            IMicrosoftAzureClient microsoftClient, IGroqClient groqClient, ILogger<EmailSummaryService> logger)
    {
        _users = users;
        _connectedAccounts = connectedAccounts;
        _microsoftClient = microsoftClient;
        _groqClient = groqClient;
        _logger = logger;
    }

    public Task PollUsersForNewEmailsAsync()
    {
        throw new NotImplementedException();
    }

    public Task SendScheduledSummariesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task Run()
    {
        // Retrieve all users due for a summary
        //IEnumerable<User> users = _users.GetDueForSummary(DateTime.UtcNow);
        IEnumerable<User> users = _users.GetAll();

        foreach (User user in users)
        {
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


                // Caluculate the date time 1 days ago in IOS 8601 format as required by Graph API
                string receivedDateTime = 
                    DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                MicrosoftMailResponse? mailResponse = await _microsoftClient.GetUnreadMail(accessToken, receivedDateTime);

                if (mailResponse == null)
                {
                    Console.WriteLine("Issue");
                    continue;
                }

                IEnumerable<MicrosoftMessage> mail = mailResponse.Messages;

                foreach (MicrosoftMessage message in mail)
                {
                    string content = message.Body.Content;

                    // TODO: Updater (remove html)
                    if (message.Body.ContentType == "html")
                    {
                        HtmlDocument doc = new();
                        doc.LoadHtml(message.Body.Content);
                        content = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);

                        //Console.WriteLine(content);
                        //Console.WriteLine();
                    }

                    // TODO: Remove whitespace and newlines
                    content = Regex.Replace(content, @"\s{2,}", " ");

                    // TODO: Truncate (limit to 1000 characters)
                    if (content.Length > 1000)
                    {
                        content = content.Substring(0, 1000);
                    }

                    //Console.WriteLine(content);
                    //
                    // TODO: Check if email has already been read before sending

                    GroqResponse? groq = await _groqClient.GenerateEmailSummary(content);

                    if (groq != null)
                    {
                        Console.WriteLine(groq.ToString());
                        Console.WriteLine(groq.Choices.First().Message.Content);
                    }

                    //return;
                }

                // TODO: Convert message to a Mail object for use with all Providers

                // get all emails in past X hours between delivery times

                // pre-filter step

                // summarize via AI

                // post-filter step

                // send via postmark
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

        return null;
    }
}
