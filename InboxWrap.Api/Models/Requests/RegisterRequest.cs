using System.ComponentModel.DataAnnotations;

namespace InboxWrap.Models.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email address is required."), EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(32, ErrorMessage = "Must be between 8 and 32 characters", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*\d)(?=.*[^a-zA-Z\d]).*$", ErrorMessage = "Password must include at least one digit and one special character")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [StringLength(32, ErrorMessage = "Must be between 8 and 32 characters", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*\d)(?=.*[^a-zA-Z\d]).*$", ErrorMessage = "Password must include at least one digit and one special character")]
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
