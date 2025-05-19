using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid CustomerId { get; set; } // E.g., Stripe Customer ID

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public UserPreferences Preferences { get; set; } = new();
}

public class UserPreferences
{
    public string TimeZoneId { get; set; } = TimeZoneInfo.Utc.Id;
    
    public TimeOnly DeliveryTime { get; set; } = new TimeOnly(8, 0);

    public bool ShouldMarkEmailsAsRead { get; set; }
    
    public bool ShouldMarkImportantEmails { get; set; }
    
    public bool ShouldIgnoreMarketingEmails { get; set; }
}
