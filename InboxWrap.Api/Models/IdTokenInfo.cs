namespace InboxWrap.Models;

public class IdTokenInfo
{
    public string Sub { get; set; } = string.Empty;   // Unique Microsoft user ID

    public string Name { get; set; } = string.Empty;  // Full display name

    public string Email { get; set; } = string.Empty; // Email or preferred username
}
