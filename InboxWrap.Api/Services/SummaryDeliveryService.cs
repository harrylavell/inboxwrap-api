using InboxWrap.Clients;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface ISummaryDeliveryService
{
    Task SendDueSummariesAsync(CancellationToken cancellationToken);
}

public class SummaryDeliveryService : ISummaryDeliveryService
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connectedAccounts;
    private readonly ISummaryRepository _summaries;
    private readonly IMicrosoftAzureClient _microsoftClient;
    private readonly IPostmarkClient _postmarkClient;
    private readonly ILogger<ISummaryDeliveryService> _logger;

    public SummaryDeliveryService(IUserRepository users, IConnectedAccountRepository connectedAccounts, ISummaryRepository summaries,
            IMicrosoftAzureClient microsoftClient, IPostmarkClient postmarkClient, ILogger<ISummaryDeliveryService> logger)
    {
        _users = users;
        _connectedAccounts = connectedAccounts;
        _summaries = summaries;
        _microsoftClient = microsoftClient;
        _postmarkClient = postmarkClient;
        _logger = logger;
    }

    public async Task SendDueSummariesAsync(CancellationToken cancellationToken)
    {
        // Retrieve all users due for a summary
        //IEnumerable<User> users = _users.GetDueForSummary(DateTime.UtcNow);
        IEnumerable<User> users = _users.GetAll();

        foreach (User user in users)
        {
            // TODO: Summaries need a ConnectedAccountId foreign key
            
            cancellationToken.ThrowIfCancellationRequested();

            List<SummaryContent> summaries = user.Summaries.Select(s => s.Content).ToList();

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

            PostmarkResponse? response = await _postmarkClient.SendSummaryEmail(templateModel);

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
