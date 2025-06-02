using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InboxWrap.Models;

public class Summary : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [Required]
    public string MessageId { get; set; } = string.Empty;
    
    [Required]
    public string Source { get; set; } = string.Empty;
    
    public bool IsDelivered { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public SummaryContent Content { get; set; } = new();
    
    public SummaryGenerationMetadata GenerationMetadata { get; set; } = new();

    public Summary() { }
}

public class SummaryContent
{
    [JsonPropertyName("summary")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("action_required")]
    public string ActionRequired { get; set; } = string.Empty;
    
    [JsonPropertyName("important")]
    public bool IsImportant { get; set; }
}

public class SummaryGenerationMetadata
{
    public string Provider { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public int InputTokens { get; set; }
    
    public int OutputTokens { get; set; }
    
    public double TimeTaken { get; set; }
}
