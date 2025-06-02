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
    private readonly ILogger<TestController> _logger;

    public TestController(IEmailPollingService pollingService, ISummaryDeliveryService deliveryService, 
            ISecretsManagerClient client, ILogger<TestController> logger)
    {
        _pollingService = pollingService;
        _deliveryService = deliveryService;
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
}
