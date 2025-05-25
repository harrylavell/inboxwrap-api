using InboxWrap.Helpers;
using InboxWrap.Models;
using InboxWrap.Models.Requests;
using InboxWrap.Module.Errors;
using InboxWrap.Repositories;

namespace InboxWrap.Services;

public interface IAccountService
{
    Task<Result<UserPreferences, AccountErrorCode>> GetPreferences(string userId);

    Task<Result<UserPreferences, AccountErrorCode>> UpdatePreferences(string userId, UserPreferencesRequest preferences);
    
    Task<Result<UserPreferences, AccountErrorCode>> UpdateTimezone(string userId, TimezoneUpdateRequest request);
    
    Task<Result<UserPreferences, AccountErrorCode>> UpdateDeliveryTimes(string userId, DeliveryTimesUpdateRequest request);
}

public class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IUserRepository users, ILogger<AccountService> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<Result<UserPreferences, AccountErrorCode>> GetPreferences(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingUserId);
        }

        User? user = await _users.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.UserNotFound);
        }

        return Result<UserPreferences, AccountErrorCode>.Ok(user.Preferences);
    }


    public async Task<Result<UserPreferences, AccountErrorCode>> UpdatePreferences(string userId,
            UserPreferencesRequest preferences)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingUserId);
        }

        if (preferences == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingUserPreferences);
        }

        User? user = await _users.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.UserNotFound);
        }

        // Check timezone to ensure it's valid
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZoneId);
            user.Preferences.TimeZoneId = preferences.TimeZoneId;
        }
        catch
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.InvalidTimezone);
        }

        // Check delivery times to ensure all are valid
        foreach (string deliveryTime in preferences.DeliveryTimes)
        {
            if (!TimeOnly.TryParse(deliveryTime, out _))
            {
                return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.InvalidDeliveryTimes);
            }
        }

        user.Preferences.DeliveryTimes = preferences.DeliveryTimes;
        user.Preferences.ShouldMarkEmailsAsRead = preferences.ShouldMarkEmailsAsRead;
        user.Preferences.ShouldMarkImportantEmails = preferences.ShouldMarkImportantEmails;
        user.Preferences.ShouldIgnoreMarketingEmails = preferences.ShouldIgnoreMarketingEmails;
        user.NextDeliveryUtc = DeliveryTimeCalculator.CalculateNextDeliveryUtc(user.Preferences);

        _users.Update(user);

        if (!await _users.SaveChangesAsync())
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.SaveFailed);
        }

        return Result<UserPreferences, AccountErrorCode>.Ok(user.Preferences);
    }

    public async Task<Result<UserPreferences, AccountErrorCode>> UpdateTimezone(string userId,
            TimezoneUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingUserId);
        }

        if (request == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingTimezone);
        }

        User? user = await _users.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.UserNotFound);
        }

        // Check timezone to ensure it's valid
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
            user.Preferences.TimeZoneId = request.TimeZoneId;
        }
        catch
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.InvalidTimezone);
        }

        user.NextDeliveryUtc = DeliveryTimeCalculator.CalculateNextDeliveryUtc(user.Preferences);

        _users.Update(user);

        if (!await _users.SaveChangesAsync())
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.SaveFailed);
        }

        return Result<UserPreferences, AccountErrorCode>.Ok(user.Preferences);
    }
    
    public async Task<Result<UserPreferences, AccountErrorCode>> UpdateDeliveryTimes(string userId,
            DeliveryTimesUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingUserId);
        }

        if (request == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.MissingDeliveryTimes);
        }

        User? user = await _users.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.UserNotFound);
        }

        // Check delivery times to ensure all are valid
        foreach (string deliveryTime in request.DeliveryTimes)
        {
            if (!TimeOnly.TryParse(deliveryTime, out _))
            {
                return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.InvalidDeliveryTimes);
            }
        }

        user.Preferences.DeliveryTimes = request.DeliveryTimes;
        user.NextDeliveryUtc = DeliveryTimeCalculator.CalculateNextDeliveryUtc(user.Preferences);

        _users.Update(user);

        if (!await _users.SaveChangesAsync())
        {
            return Result<UserPreferences, AccountErrorCode>.Fail(AccountErrorCode.SaveFailed);
        }

        return Result<UserPreferences, AccountErrorCode>.Ok(user.Preferences);

    }
}
