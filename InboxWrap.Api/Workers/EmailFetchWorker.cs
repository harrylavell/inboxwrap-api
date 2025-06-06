using InboxWrap.Services;

namespace InboxWrap.Workers;

// TODO: Fill this class out (constant)
public class EmailFetchWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailFetchWorker> _logger;

    public EmailFetchWorker(IServiceProvider serviceProvider, ILogger<EmailFetchWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailFetchWorker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var fetchService = scope.ServiceProvider.GetRequiredService<IEmailFetchService>();

            try
            {
                //await pollingService.PollUsersForNewEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching emails.");
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

        _logger.LogInformation("EmailFetchWorker is stopping.");
    }
}
