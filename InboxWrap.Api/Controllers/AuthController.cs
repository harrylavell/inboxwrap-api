using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace InboxWrap.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository users, ILogger<AuthController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        User user = new(request.EmailAddress, request.Password);

        await _users.AddAsync(user);

        if (await _users.SaveChangesAsync())
            return Ok(_users.GetByEmailAddress(request.EmailAddress));

        return StatusCode(StatusCodes.Status500InternalServerError);
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok();
    }
}
