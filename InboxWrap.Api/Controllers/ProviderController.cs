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
    private readonly IMicrosoftProviderService _microsoft;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IMicrosoftProviderService microsoft, ILogger<ProviderController> logger)
    {
        _microsoft = microsoft;
        _logger = logger;
    }

    [HttpGet("microsoft/authorize")]
    public async Task<IActionResult> MicrosoftAuthorize()
    {
        try
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("Invalid token or user not found.");
            }

            Result<string> result = await _microsoft.GenerateAzureConsentUrl(userId);

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
    [HttpGet("microsoft/callback")]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Authorization code is missing.");
            }

            Result result = await _microsoft.SetupConnectedAccount(code, state);

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
