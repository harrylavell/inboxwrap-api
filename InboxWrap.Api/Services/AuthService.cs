using InboxWrap.Models;
using InboxWrap.Module.Errors;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public record LoginResult(UserDto User, string Token);

public interface IAuthService
{
    Task<Result<UserDto, AuthErrorCode>> Register(string email, string password);
    
    Result<LoginResult, AuthErrorCode> Login(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository users, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _users = users;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<UserDto, AuthErrorCode>> Register(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<UserDto, AuthErrorCode>.Fail(AuthErrorCode.MissingEmailOrPassword);
        }
        
        // Check to see if the email address already belongs to a user.
        if (_users.ExistsByEmail(email))
        {
            return Result<UserDto, AuthErrorCode>.Fail(AuthErrorCode.EmailInUse);
        }

        User user = new()
        {
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
        };

        await _users.AddAsync(user);

        if (!await _users.SaveChangesAsync())
        {
            _logger.LogWarning("Failed to persist new user to database for email {Email}", email);
            return Result<UserDto, AuthErrorCode>.Fail(AuthErrorCode.SaveFailed);
        }
        
        return Result<UserDto, AuthErrorCode>.Ok(new UserDto(user));
    }

    public Result<LoginResult, AuthErrorCode> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<LoginResult, AuthErrorCode>.Fail(AuthErrorCode.MissingEmailOrPassword);
        }

        User? user = _users.GetByEmail(email);

        if (user == null)
        {
            return Result<LoginResult, AuthErrorCode>.Fail(AuthErrorCode.UserNotFound);
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

        if (!isValid)
        {
            _logger.LogInformation("Failed login attempt for {Email}", email);
            return Result<LoginResult, AuthErrorCode>.Fail(AuthErrorCode.InvalidCredentials);
        }

        UserDto userDto = new UserDto(user);
        string token = _tokenService.GenerateToken(user);

        LoginResult result = new(userDto, token);

        return Result<LoginResult, AuthErrorCode>.Ok(result);
    }
}
