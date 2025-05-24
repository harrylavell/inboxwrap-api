namespace InboxWrap.Workers;

public class EmailSummaryWorker : BackgroundService
{
    private readonly ILogger<EmailSummaryWorker> _logger;

    public EmailSummaryWorker(ILogger<EmailSummaryWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailSummaryWorker is starting.");

        int count = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
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
