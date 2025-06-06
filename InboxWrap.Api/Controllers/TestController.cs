using InboxWrap.Clients;
using InboxWrap.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailPollingService _pollingService;
    private readonly ISummaryDeliveryService _deliveryService;
    private readonly IEmailFetchService _fetchService;
    private readonly ISummaryGenerationService _generationService;
    private readonly ISummaryEmailDispatcher _dispatchService;
    private readonly ILogger<TestController> _logger;

    public TestController(IEmailPollingService pollingService, ISummaryDeliveryService deliveryService, IEmailFetchService fetchService,
            ISummaryGenerationService generationService, ISummaryEmailDispatcher dispatchService, ISecretsManagerClient client, ILogger<TestController> logger)
    {
        _pollingService = pollingService;
        _deliveryService = deliveryService;
        _fetchService = fetchService;
        _generationService = generationService;
        _dispatchService = dispatchService;
        _logger = logger;
    }

    [HttpGet("poll")]
    public async Task<IActionResult> Poll()
    {
        await _pollingService.PollUsersForNewEmailsAsync(CancellationToken.None);
        return Ok();
    }
    
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        await _deliveryService.SendDueSummariesAsync(CancellationToken.None);
        return Ok();
    }

    [HttpGet("fetch")]
    public async Task<IActionResult> Fetch()
    {
        await _fetchService.FetchEmailsAsync(CancellationToken.None);
        return Ok();
    }

    [HttpGet("generate")]
    public async Task<IActionResult> Generate()
    {
        await _generationService.SummarizeNextEmailAsync(CancellationToken.None);
        return Ok();
    }

    [HttpGet("dispatch")]
    public async Task<IActionResult> Dispatch()
    {
        await _dispatchService.DispatchEmailSummary(CancellationToken.None);
        return Ok();
    }
}
