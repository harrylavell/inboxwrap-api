using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models.Requests;

public class UserPreferencesRequest
{
    [Required]
    public string TimeZoneId { get; set; } = "America/New_York";

    [Required]
    [MinLength(1, ErrorMessage = "At least one delivery time must be specified.")]
    public List<string> DeliveryTimes { get; set; } = new() { "08:00" };

    public bool ShouldMarkEmailsAsRead { get; set; }

    public bool ShouldMarkImportantEmails { get; set; }

    public bool ShouldIgnoreMarketingEmails { get; set; }
}
