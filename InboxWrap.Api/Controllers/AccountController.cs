using System.Security.Claims;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Module.Errors;
using InboxWrap.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Invalid token or user not found.");
        }

        Result<UserPreferences, AccountErrorCode> result = await _accountService.GetPreferences(userId);

        if (result.Failure)
        {
            return result.Error switch
            {
                AccountErrorCode.MissingUserId => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.UserNotFound => NotFound(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.SaveFailed => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        return Ok(result.Value);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesRequest updated)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Invalid token or user not found.");
        }

        Result<UserPreferences, AccountErrorCode> result = await _accountService.UpdatePreferences(userId, updated);

        if (result.Failure)
        {
            return result.Error switch
            {
                AccountErrorCode.MissingUserId => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.MissingUserPreferences => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.InvalidTimezone => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.InvalidDeliveryTimes => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.UserNotFound => NotFound(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.SaveFailed => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        return Ok(result.Value);
    }

    [HttpPatch("preferences/timezone")]
    public async Task<IActionResult> UpdateTimezone([FromBody] TimezoneUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Invalid token or user not found.");
        }

        Result<UserPreferences, AccountErrorCode> result = await _accountService.UpdateTimezone(userId, request);

        if (result.Failure)
        {
            return result.Error switch
            {
                AccountErrorCode.MissingUserId => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.MissingTimezone => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.InvalidTimezone => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.UserNotFound => NotFound(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.SaveFailed => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        return Ok(result.Value);
    }

    [HttpPatch("preferences/delivery-times")]
    public async Task<IActionResult> UpdateDeliveryTimes([FromBody] DeliveryTimesUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized("Invalid token or user not found.");
        }

        Result<UserPreferences, AccountErrorCode> result = await _accountService.UpdateDeliveryTimes(userId, request);

        if (result.Failure)
        {
            return result.Error switch
            {
                AccountErrorCode.MissingUserId => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.MissingDeliveryTimes => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.InvalidDeliveryTimes => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.UserNotFound => NotFound(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AccountErrorCode.SaveFailed => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        return Ok(result.Value);
    }
}
