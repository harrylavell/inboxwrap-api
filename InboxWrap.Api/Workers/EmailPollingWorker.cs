using InboxWrap.Services;

namespace InboxWrap.Workers;

public class EmailPollingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailPollingWorker> _logger;

    public EmailPollingWorker(IServiceProvider serviceProvider, ILogger<EmailPollingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailPollingWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var pollingService = scope.ServiceProvider.GetRequiredService<IEmailPollingService>();

            try
            {
                _logger.LogInformation("Running email polling task...");
                await pollingService.PollUsersForNewEmailsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling emails.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Graceful shutdown
                break;
            }
        }

        _logger.LogInformation("EmailPollingWorker is stopping.");
    }
}
