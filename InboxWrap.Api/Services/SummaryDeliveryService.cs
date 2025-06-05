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
    private readonly IMicrosoftAzureClient _microsoftClient;
    private readonly IPostmarkClient _postmarkClient;
    private readonly ILogger<ISummaryDeliveryService> _logger;

    public SummaryDeliveryService(IUserRepository users, IConnectedAccountRepository connectedAccounts,
            IMicrosoftAzureClient microsoftClient, IPostmarkClient postmarkClient, ILogger<ISummaryDeliveryService> logger)
    {
        _users = users;
        _connectedAccounts = connectedAccounts;
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

            Console.WriteLine(response?.MessageID.ToString());
            Console.WriteLine(response?.Message);
        }
    }
}
