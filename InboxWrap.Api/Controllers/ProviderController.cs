using System.Security.Claims;
using InboxWrap.Models;
using InboxWrap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class ProviderController : ControllerBase
{
    private readonly IProviderService _providerService;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IProviderService providerService, ILogger<ProviderController> logger)
    {
        _providerService = providerService;
        _logger = logger;
    }

    [HttpGet("azure/authorize")]
    public async Task<IActionResult> AzureAuthorizeApplication()
    {
        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("Invalid token or user not found.");
            }

            Result<string> result = await _providerService.GenerateAzureConsentUrl(userId);

            if (result.Failure)
            {
                return StatusCode(500, result.Error);
            }

            return Ok(result.Value);
            //return Redirect(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [AllowAnonymous]
    [HttpGet("azure/callback")]
    public async Task<IActionResult> AzureCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code is missing.");
            }

            Result result = await _providerService.SetupConnectedAccount(code, state);

            if (result.Failure)
            {
                return StatusCode(500, result.Error);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
