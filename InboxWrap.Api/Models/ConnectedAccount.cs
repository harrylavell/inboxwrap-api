using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InboxWrap.Models;

public enum Providers
{
    Microsoft,
    Google
}

public class ConnectedAccount : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string ProviderUserId { get; set; } = string.Empty;
    
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    [Required]
    public string Provider { get; set; } = string.Empty;

    [Required]
    public string? Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public DateTime AccessTokenExpiryUtc { get; set; }

    public bool IsRevoked { get; set; } = false;

    public ConnectedAccount() { }
}
