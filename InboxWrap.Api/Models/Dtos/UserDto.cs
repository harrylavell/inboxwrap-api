namespace InboxWrap.Models;

public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; }

    public UserPreferences Preferences { get; set; }

    public UserDto(User user)
    {
        Id = user.Id;
        Email = user.Email;
        Preferences = user.Preferences;
    }
}
