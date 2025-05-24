using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Module.Errors;
using InboxWrap.Repositories;
using InboxWrap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ITokenService tokenService, IUserRepository users, ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _users = users;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        Result<UserDto, AuthErrorCode> result = await _authService.Register(request.Email, request.Password);

        if (result.Failure)
        {
            return result.Error switch
            {
                AuthErrorCode.MissingEmailOrPassword => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AuthErrorCode.EmailInUse => Conflict(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AuthErrorCode.SaveFailed => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        return CreatedAtAction(nameof(Register), result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        Result<LoginResult, AuthErrorCode> result = _authService.Login(request.Email, request.Password);

        if (result.Failure)
        {
            return result.Error switch
            {
                AuthErrorCode.MissingEmailOrPassword => BadRequest(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AuthErrorCode.UserNotFound => NotFound(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                AuthErrorCode.InvalidCredentials => Unauthorized(new { code = result.Error.ToString(), message = result.Error.GetMessage() }),
                _ => StatusCode(500, new { code = result.Error.ToString(), message = result.Error.GetMessage() })
            };
        }

        if (result.Value?.Token == null)
        {
            _logger.LogError("An unknown error occurred during login for token check: {Email}", request.Email);
            return StatusCode(500, "An unknown error occurred");
        }

        Response.Cookies.Append("access_token", result.Value.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Only over HTTPS
            SameSite = SameSiteMode.Strict, // or Lax
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(result.Value.User);
    }
}
