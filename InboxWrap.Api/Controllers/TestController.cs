using InboxWrap.Clients;
using InboxWrap.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailFetchService _fetchService;
    private readonly ISummaryGenerationService _generationService;
    private readonly ISummaryEmailDispatchService _dispatchService;
    private readonly ILogger<TestController> _logger;

    public TestController(IEmailFetchService fetchService, ISummaryGenerationService generationService,
            ISummaryEmailDispatchService dispatchService, ISecretsManagerClient client, ILogger<TestController> logger)
    {
        _fetchService = fetchService;
        _generationService = generationService;
        _dispatchService = dispatchService;
        _logger = logger;
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
