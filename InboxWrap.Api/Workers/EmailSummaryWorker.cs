using InboxWrap.Services;

namespace InboxWrap.Workers;

public class EmailSummaryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailSummaryWorker> _logger;

    public EmailSummaryWorker(IServiceProvider serviceProvider, ILogger<EmailSummaryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailSummaryWorker is starting.");

        using IServiceScope scope = _serviceProvider.CreateScope();
        var emailSummaryService = scope.ServiceProvider.GetRequiredService<IEmailSummaryService>();

        int count = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //await emailSummaryService.Run();

                Console.WriteLine($"EmailSummaryWorker: {count}");
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email summary worker process.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("EmailSummaryWorker is stopping.");
    }
}
