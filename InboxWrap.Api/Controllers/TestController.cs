using InboxWrap.Clients;
using InboxWrap.Models.Reponses;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TestController : ControllerBase
{
    private readonly ISecretsManagerClient _client;
    private readonly ILogger<TestController> _logger;

    public TestController(ISecretsManagerClient client, ILogger<TestController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        IEnumerable<Secret>? secrets = await _client.GetSecretsAsync();

        return Ok(secrets);
    }
}
