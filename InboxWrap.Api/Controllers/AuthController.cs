using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Repositories;
using InboxWrap.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ITokenService tokenService, IUserRepository users, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _users = users;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        User user = new(request.Email, request.Password);

        await _users.AddAsync(user);

        if (await _users.SaveChangesAsync())
            return Ok(_users.GetByEmail(request.Email));

        return StatusCode(StatusCodes.Status500InternalServerError);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // TODO: User validation

        User? user = _users.GetByEmail(request.Email);

        if (user is not null)
        {
            string? token = await _tokenService.Generate(user);

            if (!string.IsNullOrEmpty(token))
            {
                Response.Cookies.Append("access_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Only over HTTPS
                    SameSite = SameSiteMode.Strict, // or Lax
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });

                return Ok(new { user.Id, user.Email, user.Preferences });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        return NotFound();
    }
}
