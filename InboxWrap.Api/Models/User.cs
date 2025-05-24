using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models;

public class User : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid CustomerId { get; set; } // E.g., Stripe Customer ID

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public UserPreferences Preferences { get; set; } = new();

    public List<ConnectedAccount> ConnectedAccounts { get; set; } = [];

    public DateTime? NextDeliveryTimeUtc { get; set; }

    public User() { }
}

public class UserPreferences
{
    private string _timeZoneId = string.Empty;

    public string TimeZoneId => _timeZoneId;
    
    public List<string> DeliveryTimes { get; set; } = [ "8:00" ];

    public bool ShouldMarkEmailsAsRead { get; set; }
    
    public bool ShouldMarkImportantEmails { get; set; }
    
    public bool ShouldIgnoreMarketingEmails { get; set; }

    public UserPreferences()
    {
        SetTimeZone("America/New_York");
    }

    public void SetTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new ArgumentException("Time zone ID cannot be empty.");
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            _timeZoneId = timeZone;
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Invalid time zone data: {timeZone}");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException($"Corrupt time zone data: {timeZone}");
        }
    }
}
