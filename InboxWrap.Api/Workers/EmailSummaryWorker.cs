using InboxWrap.Services;

namespace InboxWrap.Workers;

public class EmailSummaryWorker : BackgroundService
{
    private readonly IEmailSummaryService _emailSummaryService;
    private readonly ILogger<EmailSummaryWorker> _logger;

    public EmailSummaryWorker(IEmailSummaryService emailSummaryService, ILogger<EmailSummaryWorker> logger)
    {
        _emailSummaryService = emailSummaryService;
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
                //await _emailSummaryService.Run();
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
