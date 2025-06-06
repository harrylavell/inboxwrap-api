using InboxWrap.Services;

namespace InboxWrap.Workers;

public class SummaryGenerationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SummaryGenerationWorker> _logger;
    private readonly SemaphoreSlim _throttle = new(5);

    public SummaryGenerationWorker(IServiceProvider serviceProvider, ILogger<SummaryGenerationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SummaryGenerationWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await _throttle.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryGenerationService>();

                    await summaryService.SummarizeNextEmailAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to summarize email.");
                }
                finally
                {
                    _throttle.Release();
                }
            }, stoppingToken);
        }

        _logger.LogInformation("SummaryGenerationWorker is stopping.");
    }
}
