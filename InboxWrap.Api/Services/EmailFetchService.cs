using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
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
                ? accessToken = await UpdateAccessToken(account)
                : account.AccessToken; // TODO: Encrypt/decrypt

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogError("Skipping connected account as we can't retrieve a valid access token.");
                continue;
            }

            List<Mail> emails = [];

            if (account.Provider == Providers.Microsoft)
            {
                // Calculate the date time IOS 8601 format as required by Graph API
                string receivedDateTime = lastFetchedCutoffUtc
                    .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

                MicrosoftMailResponse? response = await _client.GetUnreadMail(accessToken, receivedDateTime);

                if (response == null)
                {
                    _logger.LogError("Unable to retrieve unread mail for connected account.");
                    continue;
                }

                // Map each Microsoft message to a generic Mail object
                response.Messages.ForEach(message => emails.Add(new Mail(message)));
            }

            if (account.Provider == Providers.Google)
            {

            }

            // Update each email via an updater
            foreach (IMailUpdater updater in _updaters)
            {
                emails = updater.Update(emails);
            }

            // Iterate through each email and parse it through the updaters
            // before pushing it to the summary generation queue
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

                Console.WriteLine(job.ToString());
                await _summaryQueue.EnqueueAsync(job);
            }

            await UpdateLastFetchedAtAsync(account);
        }
    }

    private async Task<string?> UpdateAccessToken(ConnectedAccount account)
    {
        string refreshToken = account.RefreshToken; // TODO: Encrypt/decrypt

        if (account.Provider == Providers.Microsoft) // TODO: Fix casing
        {
            MicrosoftTokenResponse? tokenData = await _client.RefreshToken(refreshToken);
            if (string.IsNullOrWhiteSpace(tokenData?.AccessToken) || string.IsNullOrWhiteSpace(tokenData?.RefreshToken))
            {
                _logger.LogError(""); // TODO: Add good error message
                return null;
            }

            account.AccessToken = tokenData.AccessToken; // TODO: Encrypt/decrypt
            account.RefreshToken = tokenData.RefreshToken; // TODO: Encrypt/decrypt
            account.AccessTokenExpiryUtc = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

            _connected.Update(account);

            if (!await _connected.SaveChangesAsync())
            {
                // TODO: Better logging to record which connected account also, stuctured maybe? LogTags, etc
                _logger.LogError("Failed to update connected account: {ConnectedAccount}", account.Id);
                return null;
            }

            // Update with new access token
            return tokenData.AccessToken;
        }

        if (account.Provider == Providers.Google)
        {

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
