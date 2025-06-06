using InboxWrap.Models.Jobs;

namespace InboxWrap.Infrastructure.Queues;

public interface ISummaryQueue
{
    Task EnqueueAsync(SummarizeEmailJob job);
    Task<SummarizeEmailJob> DequeueAsync(CancellationToken ct);
}
