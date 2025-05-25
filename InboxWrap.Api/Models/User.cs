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

    public DateTime? NextDeliveryUtc { get; set; } = null;

    public User() {  }
}

public class UserPreferences
{
    public string TimeZoneId { get; set; } = "America/New_York";
    
    public List<string> DeliveryTimes { get; set; } = [ "8:00" ];

    public bool ShouldMarkEmailsAsRead { get; set; }
    
    public bool ShouldMarkImportantEmails { get; set; }
    
    public bool ShouldIgnoreMarketingEmails { get; set; }

    public UserPreferences() { }
}
