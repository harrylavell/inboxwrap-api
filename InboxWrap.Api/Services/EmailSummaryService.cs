using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface IEmailSummaryService
{
    Task Run();
}

public class EmailSummaryService : IEmailSummaryService
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connectedAccounts;
    private readonly IMicrosoftAzureClient _microsoftClient;
    private readonly ILogger<EmailSummaryService> _logger;

    public EmailSummaryService(IUserRepository users, IConnectedAccountRepository connectedAccounts,
            IMicrosoftAzureClient microsoftClient, ILogger<EmailSummaryService> logger)
    {
        _users = users;
        _connectedAccounts = connectedAccounts;
        _microsoftClient = microsoftClient;
        _logger = logger;
    }

    public async Task Run()
    {
        // Retrieve all users due for a summary
        //IEnumerable<User> users = _users.GetDueForSummary(DateTime.UtcNow);
        IEnumerable<User> users = _users.GetAll();


        foreach (User user in users)
        {
            Console.WriteLine(user.Email);

            foreach (ConnectedAccount connectedAccount in user.ConnectedAccounts)
            {
                Console.WriteLine("Provider: " + connectedAccount.Provider);
                Console.WriteLine("Provider User ID: " + connectedAccount.ProviderUserId);
                Console.WriteLine("Access Token: " + connectedAccount.AccessToken);
                Console.WriteLine("Refresh Token: " + connectedAccount.RefreshToken);

                string? accessToken = null;

                // Update connected account's access token if it's expired
                if (connectedAccount.AccessTokenExpiryUtc <= DateTime.UtcNow)
                {
                    accessToken = await UpdateAccessToken(user, connectedAccount);

                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        // TODO: Improve error message
                        _logger.LogError("Skipping connected account as we can't retrieve a valid access token.");
                        continue;
                    }
                }
                else
                {
                    // Use stored access token if it's still valid
                    accessToken = connectedAccount.AccessToken; // TODO: Encrypt/decrypt
                }

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
