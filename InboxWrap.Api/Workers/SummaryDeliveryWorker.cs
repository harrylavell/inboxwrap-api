using InboxWrap.Services;

public class SummaryDeliveryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SummaryDeliveryWorker> _logger;

    public SummaryDeliveryWorker(IServiceProvider serviceProvider, ILogger<SummaryDeliveryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SummaryDeliveryWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var deliveryService = scope.ServiceProvider.GetRequiredService<ISummaryDeliveryService>();

            try
            {
                _logger.LogInformation("Running summary delivery task...");
                //await deliveryService.SendDueSummariesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during summary delivery.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Check every minute
            }
            catch (TaskCanceledException)
            {
                break; // Graceful shutdown
            }
        }

        _logger.LogInformation("SummaryDeliveryWorker is stopping.");
    }
}
