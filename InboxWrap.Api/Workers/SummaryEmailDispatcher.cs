using InboxWrap.Services;

namespace InboxWrap.Workers;

public class SummaryEmailDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SummaryEmailDispatcher> _logger;

    public SummaryEmailDispatcher(IServiceProvider serviceProvider, ILogger<SummaryEmailDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SummaryEmailDispatcher is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<ISummaryEmailDispatchService>();

                await dispatchService.DispatchEmailSummary(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch summaries.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("SummaryEmailDispatcher is stopping.");
    }
}
