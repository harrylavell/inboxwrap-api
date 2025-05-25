using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models.Requests;

public class DeliveryTimesUpdateRequest
{
    [Required(ErrorMessage = "At least one delivery time is required.")]
    [MinLength(1, ErrorMessage = "At least one delivery time is required.")]
    [MaxLength(3, ErrorMessage = "You cannot have more than three delivery times.")]
    public List<string> DeliveryTimes { get; set; } = new();
}
