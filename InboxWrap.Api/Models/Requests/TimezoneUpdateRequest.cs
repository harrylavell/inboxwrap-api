using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models.Requests;

public class TimezoneUpdateRequest
{
    [Required(ErrorMessage = "TimeZoneId is required.")]
    [MinLength(3, ErrorMessage = "TimeZoneId must be at least 3 characters.")]
    public string TimeZoneId { get; set; } = string.Empty;
}
