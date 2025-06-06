using System.Text.Json;
using InboxWrap.Clients;
using InboxWrap.Constants;
using InboxWrap.Infrastructure.Queues;
using InboxWrap.Models;
using InboxWrap.Models.Jobs;
using InboxWrap.Models.Responses;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface ISummaryGenerationService
{
    Task SummarizeNextEmailAsync(CancellationToken ct);

    Task SummarizeEmailAsync(SummarizeEmailJob job, CancellationToken ct);
}

public class SummaryGenerationService : ISummaryGenerationService
{
    private readonly IConnectedAccountRepository _connected;
    private readonly ISummaryRepository _summaries;
    private readonly IGroqClient _client;
    private readonly ISummaryQueue _queue;
    private readonly ILogger<EmailFetchService> _logger;

    public SummaryGenerationService(IConnectedAccountRepository connected, ISummaryRepository summaries,
            IGroqClient client, ISummaryQueue queue, ILogger<EmailFetchService> logger)
    {
        _connected = connected;
        _summaries = summaries;
        _client = client;
        _queue = queue;
        _logger = logger;
    }

    public async Task SummarizeNextEmailAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        SummarizeEmailJob job = await _queue.DequeueAsync(ct);

        _logger.LogInformation($"Summarizing email {job.EmailId} for user {job.UserId}");
        await SummarizeEmailAsync(job, ct);
    }

    public async Task SummarizeEmailAsync(SummarizeEmailJob job, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        GroqResponse? response = await _client.GenerateEmailSummary(job.Subject, job.Body, ct);

        if (response == null)
        {
            _logger.LogError("Groq response is null or empty.");
            return;
        }

        string? responseContent = response.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            _logger.LogError("Groq response content is null or empty.");
            return;
        }

        SummaryContent? summaryContent;
        try
        {
            summaryContent = JsonSerializer.Deserialize<SummaryContent>(responseContent!);
            if (summaryContent == null)
            {        
                _logger.LogError($"Deserialization of summary content returned null. Raw content: {responseContent}");
                return;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Deserialization of summary content returned null. Raw content: {responseContent}");
            return;
        }

        Summary summary = new()
        {
            Id = Guid.NewGuid(),
            UserId = job.UserId,
            ConnectedAccountId = job.ConnectedAccountId,
            Source = job.Source,
            Content = summaryContent,
            Metadata = new SummaryMetadata()
            {
                Subject = job.Subject,
                Link = job.Link,
                ExternalMessageId = job.EmailId,
            },
            GenerationMetadata = new SummaryGenerationMetadata()
            {
                Provider = GenProviders.Groq,
                RequestId = response.XGroq.Id,
                InputTokens = response.Usage.PromptTokens,
                OutputTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens,
                TimeTaken = response.Usage.TotalTime
            },
            DeliveryMetadata = new SummaryDeliveryMetadata()
            {
                Status = DeliveryStatuses.Pending
            }
        };

        await _summaries.AddAsync(summary);
        await _summaries.SaveChangesAsync();
    }
}
