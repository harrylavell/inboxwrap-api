using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface ISummaryEmailDispatcher
{
    Task DispatchEmailSummary(CancellationToken ct);

    //Task RunAsync(CancellationToken ct);
}

public class SummaryEmailDispatcher : ISummaryEmailDispatcher
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connected;
    private readonly ISummaryRepository _summaries;
    private readonly IPostmarkClient _client;
    private readonly ILogger<EmailFetchService> _logger;

    public SummaryEmailDispatcher(IUserRepository users, IConnectedAccountRepository connected, IPostmarkClient client,
            ISummaryRepository summaries, ILogger<EmailFetchService> logger)
    {
        _users = users;
        _connected = connected;
        _summaries = summaries;
        _client = client;
        _logger = logger;
    }

    public async Task DispatchEmailSummary(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Retrieve all users due for a summary
        //IEnumerable<User> users = _users.GetDueForSummary(DateTime.UtcNow);
        IEnumerable<User> users = _users.GetAll();

        foreach (User user in users)
        {
            ct.ThrowIfCancellationRequested();

            foreach (ConnectedAccount account in user.ConnectedAccounts)
            {
                List<SummaryContent> summaries = account.Summaries.Select(s => s.Content).ToList();

                summaries = summaries.OrderByDescending(s => s.PriorityScore).ToList();
                Console.WriteLine(summaries.Count());

                List<SummaryContent> topPicks = summaries.Take(3).ToList();
                List<SummaryContent> otherSummaries = summaries.Skip(3).ToList();

                DailySummaryTemplateModel templateModel = new()
                {
                    Subject = "test",
                    Date = DateTime.Now.ToString("MMMM d"),
                    IntroText = "This is an intro text",
                    TopPicks = topPicks,
                    OtherSummaries = otherSummaries
                };

                PostmarkResponse? response = await _client.SendSummaryEmail(templateModel);

                Console.WriteLine(response?.To);
                Console.WriteLine(response?.SubmittedAt);
                Console.WriteLine(response?.ErrorCode);
                Console.WriteLine(response?.MessageID.ToString());
                Console.WriteLine(response?.Message);

                if (response == null)
                {
                    // TODO: Handle this better
                    return;
                }

                foreach (Summary summary in user.Summaries)
                {
                    // Update the delivery metadata
                    summary.DeliveryMetadata = new SummaryDeliveryMetadata
                    {
                        Provider = EmailProviders.Postmark,
                        MessageId = response.MessageID,
                        Status = (response.ErrorCode != 0)
                            ? DeliveryStatuses.Failed
                            : DeliveryStatuses.Delivered,
                        ErrorMessage = (response.ErrorCode != 0)
                            ? response.Message
                            : string.Empty,
                        SentAtUtc = response.SubmittedAt,
                        AttemptCount = summary.DeliveryMetadata.AttemptCount += 1,
                    };
                    
                    // Summary was successfully delivered
                    if (summary.DeliveryMetadata.Status == DeliveryStatuses.Delivered)
                    {
                        summary.DeliveryStatus = DeliveryStatuses.Delivered;
                        summary.DeliveredAtUtc = response.SubmittedAt;
                        //summary.Content = null;
                        //summary.Metadata = null;
                    }
                    else
                    {
                        // TODO: Unhappy path, i.e., not delivered
                    }

                    _summaries.Update(summary);
                }

                await _summaries.SaveChangesAsync();
            }
        }
    }

    /*
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {

        }
    }
    */
}
