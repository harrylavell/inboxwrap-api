using InboxWrap.Clients;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISecretsManagerClient _secretsManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISecretsManagerClient secretsManager, ILogger<AuthController> logger)
    {
        _secretsManager = secretsManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
    }
}
