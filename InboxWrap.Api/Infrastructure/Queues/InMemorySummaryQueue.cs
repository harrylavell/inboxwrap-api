using System.Threading.Channels;
using InboxWrap.Models.Jobs;

namespace InboxWrap.Infrastructure.Queues;

public class InMemorySummaryQueue : ISummaryQueue
{
    private readonly Channel<SummarizeEmailJob> _queue;

    public InMemorySummaryQueue()
    {
        var options = new BoundedChannelOptions(10000)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<SummarizeEmailJob>(options);
    }

    public async Task EnqueueAsync(SummarizeEmailJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async Task<SummarizeEmailJob> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
