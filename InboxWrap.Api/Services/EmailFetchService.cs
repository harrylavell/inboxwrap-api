using System.Globalization;
using InboxWrap.Clients;
using InboxWrap.Constants;
using InboxWrap.Infrastructure.Queues;
using InboxWrap.Models;
using InboxWrap.Models.Jobs;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;
using InboxWrap.Updaters;

namespace InboxWrap.Services;

public interface IEmailFetchService
{
    Task FetchEmailsAsync(CancellationToken ct);
}

public class EmailFetchService : IEmailFetchService
{
    private readonly IConnectedAccountRepository _connected;
    private readonly IMicrosoftAzureClient _client;
    private readonly ISummaryQueue _summaryQueue;
    private readonly IEnumerable<IMailUpdater> _updaters;
    private readonly ILogger<EmailFetchService> _logger;

    public EmailFetchService(IConnectedAccountRepository connected, IMicrosoftAzureClient client,
            ISummaryQueue summaryQueue, IEnumerable<IMailUpdater> updaters, ILogger<EmailFetchService> logger)
    {
        _connected = connected;
        _client = client;
        _summaryQueue = summaryQueue;
        _updaters = updaters;
        _logger = logger;
    }

    public async Task FetchEmailsAsync(CancellationToken ct)
    {
        DateTime fetchStartUtc = DateTime.UtcNow;
        DateTime lastFetchedCutoffUtc = fetchStartUtc.AddMinutes(-5);
        //DateTime lastFetchedCutoffUtc = fetchStartUtc.AddDays(-5);
        IEnumerable<ConnectedAccount> dueForFetch = _connected.GetDueForFetch(lastFetchedCutoffUtc);

        if (!dueForFetch.Any())
        {
            // Back off for a short time if there's nothing to do
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return;
        }

        foreach (ConnectedAccount account in dueForFetch)
        {
            ct.ThrowIfCancellationRequested();
            await UpdateFetchLockUntilAsync(account);

            string? accessToken = (account.AccessTokenExpiryUtc <= DateTime.UtcNow)
                ? await UpdateAccessToken(account)
                : account.AccessToken; // TODO: Encrypt/decrypt

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Skipping connected account as we can't retrieve a valid access token.");
                continue;
            }

            List<Mail> emails = [];

            if (account.Provider == Providers.Microsoft)
            {
                // Calculate the date time IOS 8601 format as required by Graph API.
                string receivedDateTime = lastFetchedCutoffUtc
                    .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                Result<MicrosoftMailResponse> result = await _client.GetUnreadMail(accessToken, receivedDateTime);

                if (result.Failure || result.Value == null)
                {
                    _logger.LogError("Unread mail retrieval failed: {Error}", result.Error);
                    continue;
                }

                // Map each Microsoft message to a generic Mail object
                result.Value.Messages.ForEach(message => emails.Add(new Mail(message)));
            }
            else if (account.Provider == Providers.Google)
            {
                _logger.LogWarning("Google provider not yet implemented for account: {AccountId}", account.Id);
                await Task.CompletedTask;
            }
            else
            {
                _logger.LogWarning("Unknown provider '{Provider}' for account: {AccountId}", account.Provider, account.Id);
                await Task.CompletedTask;
            }

            // Update each email via an updater
            foreach (IMailUpdater updater in _updaters)
            {
                emails = updater.Update(emails);
            }

            // Iterate through each email and push to summary generation queue.
            foreach (Mail email in emails)
            {
                SummarizeEmailJob job = new()
                {
                    Id = Guid.NewGuid(),
                    UserId = account.UserId,
                    ConnectedAccountId = account.Id,
                    EmailId = email.Id,
                    Subject = email.Subject,
                    Body = email.Body,
                    Link = email.Link,
                    Source = email.Source
                };

                try
                {
                    Console.WriteLine(job.ToString());
                    await _summaryQueue.EnqueueAsync(job);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue summarization job for account {AccountId}, email {EmailId}",
                            account.Id, email.Id);
                }
            }

            await UpdateLastFetchedAtAsync(account);
            _logger.LogInformation("Fetched and queued {Count} emails for account {AccountId}", emails.Count, account.Id);
        }

    }

    private async Task<string?> UpdateAccessToken(ConnectedAccount account)
    {
        string refreshToken = account.RefreshToken; // TODO: Encrypt/decrypt

        if (account.Provider == Providers.Microsoft) // TODO: Fix casing
        {
            Result<MicrosoftTokenResponse> result = await _client.RefreshToken(refreshToken);

            if (result.Failure || result.Value == null)
            {
                _logger.LogError("Token refresh failed: {Error}", result.Error);
                return null;
            }

            MicrosoftTokenResponse token = result.Value;

            if (string.IsNullOrWhiteSpace(token.AccessToken) || string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                _logger.LogError("Access token or refresh token not retrieved.");
                return null;
            }

            account.AccessToken = token.AccessToken; // TODO: Encrypt/decrypt
            account.RefreshToken = token.RefreshToken; // TODO: Encrypt/decrypt
            account.AccessTokenExpiryUtc = DateTime.UtcNow.AddSeconds(token.ExpiresIn);

            _connected.Update(account);

            if (!await _connected.SaveChangesAsync())
            {
                // TODO: Better logging to record which connected account also, stuctured maybe? LogTags, etc
                _logger.LogError("Failed to update connected account: {ConnectedAccount}", account.Id);
                return null;
            }

            // Update with new access token
            return token.AccessToken;
        }
        else if (account.Provider == Providers.Google)
        {
            _logger.LogWarning("Google refresh token handling not yet implemented for account: {AccountId}",
                    account.Id);
        }
        else
        {
            _logger.LogWarning("Unknown provider '{Provider}' refresh token handling for account: {AccountId}",
                    account.Provider, account.Id);
        }

        return null;
    }

    private async Task UpdateFetchLockUntilAsync(ConnectedAccount account)
    {
        // Set a 5 minute lock on the connected account to prevent race conditions/overlapping
        account.FetchLockUntilUtc = DateTime.UtcNow.AddMinutes(5);
        _connected.Update(account);

        if (!await _connected.SaveChangesAsync())
        {
            _logger.LogError($"Failed to update FetchLockUntilUtc for connected account: {account.Id}");
        }
    }

    private async Task UpdateLastFetchedAtAsync(ConnectedAccount account)
    {
        // Update the connected account's last fetched time
        account.LastFetchedAtUtc = DateTime.UtcNow;
        _connected.Update(account);

        if (!await _connected.SaveChangesAsync())
        {
            _logger.LogError($"Failed to update LastFetchedAtUtc for connected account: {account.Id}");
        }
    }
}
