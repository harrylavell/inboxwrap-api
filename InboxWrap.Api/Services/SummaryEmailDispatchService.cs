using InboxWrap.Clients;
using InboxWrap.Constants;
using InboxWrap.Helpers;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface ISummaryEmailDispatchService
{
    Task DispatchEmailSummary(CancellationToken ct);
}

public class SummaryEmailDispatchService : ISummaryEmailDispatchService
{
    private readonly IUserRepository _users;
    private readonly IConnectedAccountRepository _connected;
    private readonly ISummaryRepository _summaries;
    private readonly IPostmarkClient _client;
    private readonly ILogger<EmailFetchService> _logger;

    public SummaryEmailDispatchService(IUserRepository users, IConnectedAccountRepository connected, IPostmarkClient client,
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
        IEnumerable<User> users = _users.GetDueForSummary(DateTime.UtcNow);
        //IEnumerable<User> users = _users.GetAll();

        foreach (User user in users)
        {
            ct.ThrowIfCancellationRequested();

            foreach (ConnectedAccount account in user.ConnectedAccounts)
            {
                // Retrieve all summaries that are ready for delivery for this
                // connected account.
                List<Summary> summaries = account.Summaries
                    .Where(s => s.DeliveryStatus == DeliveryStatuses.Pending)
                    .OrderByDescending(s => s.Content.PriorityScore)
                    .ToList();

                // Perform mapping to Postmark-friendly object
                List<SummaryItem> items = [];
                foreach (Summary summary in summaries)
                {
                    items.Add(new SummaryItem(summary));
                }
                
                List<SummaryItem> financeAndBills = items.Where(i => i.Category == "Finance & Bills").ToList();
                List<SummaryItem> eventsAndReminders = items.Where(i => i.Category == "Events & Reminders").ToList();
                List<SummaryItem> securityAndAccount = items.Where(i => i.Category == "Security & Account").ToList();
                List<SummaryItem> personalAndSocial = items.Where(i => i.Category == "Personal & Social").ToList();
                List<SummaryItem> entertainmentAndGaming = items.Where(i => i.Category == "Entertainment & Gaming").ToList();
                List<SummaryItem> promotionsAndNewsletters = items.Where(i => i.Category == "Promotions & Newsletters").ToList();
                DailySummaryTemplateModel templateModel = new()
                {
                    Subject = $"Your InboxWrap Summary - {DateTime.Now.ToString("MMMM d")}",
                    Date = DateTime.Now.ToString("MMMM d, yyyy"),
                    IntroText = "Another day, another avalanche of emails. We read them so you don’t have to. Here’s the good stuff.",
                    EmailCount = items.Count(),
                    FinanceAndBills = financeAndBills.Any() ? financeAndBills : null,
                    EventsAndReminders = eventsAndReminders.Any() ? eventsAndReminders : null,
                    SecurityAndAccount = securityAndAccount.Any() ? securityAndAccount : null,
                    PersonalAndSocial = personalAndSocial.Any() ? personalAndSocial : null,
                    EntertainmentAndGaming = entertainmentAndGaming.Any() ? entertainmentAndGaming : null,
                    PromotionsAndNewsletters = promotionsAndNewsletters.Any()
                        ? CapAndInsertTruncation(promotionsAndNewsletters, 2, "promotional") : null,
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
                    continue;
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
                        AttemptCount = summary.DeliveryMetadata.AttemptCount + 1,
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

            // Ensure the next delivery time is kept up to date
            user.NextDeliveryUtc = DeliveryTimeCalculator.CalculateNextDeliveryUtc(user.Preferences);
            _users.Update(user);
            await _users.SaveChangesAsync();
        }
    }

    private List<SummaryItem> CapAndInsertTruncation(List<SummaryItem> items, int cap, string emailType)
    {
        if (items.Count <= cap)
        {
            return items;
        }

        int truncatedCount = items.Count - cap;
        List<SummaryItem> visibleItems = items.Take(cap).ToList();

        visibleItems.Add(new SummaryItem()
        {
            Title = $"+ {truncatedCount} more {emailType} emails we didn’t bother showing you.",
            Content = string.Empty,
        });

        return visibleItems;
    }
}
