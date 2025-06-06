using InboxWrap.Services;

namespace InboxWrap.Workers;

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
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fetchService = scope.ServiceProvider.GetRequiredService<IEmailFetchService>();

                await fetchService.FetchEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch emails.");
            }

            // Minor delay as to not overwhelm the DB and services
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("EmailFetchWorker is stopping.");
    }
}
