using InboxWrap.Clients;
using InboxWrap.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailSummaryService _emailSummaryService;
    private readonly ILogger<TestController> _logger;

    public TestController(IEmailSummaryService emailSummaryService, ISecretsManagerClient client, ILogger<TestController> logger)
    {
        _emailSummaryService = emailSummaryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await _emailSummaryService.Run();
        return Ok();
    }
}
