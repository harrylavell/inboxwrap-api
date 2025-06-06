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
    public Guid ConnectedAccountId { get; set; }

    [ForeignKey("ConnectedAccountId")]
    public ConnectedAccount ConnectedAccount { get; set; } = null!;

    [Required]
    public string Source { get; set; } = string.Empty;
    
    public string DeliveryStatus { get; set; } = DeliveryStatuses.Pending;

    public DateTime? DeliveredAtUtc { get; set; }

    public SummaryContent Content { get; set; } = new();
    
    public SummaryMetadata Metadata { get; set; } = new();
    
    public SummaryGenerationMetadata GenerationMetadata { get; set; } = new();
    
    public SummaryDeliveryMetadata DeliveryMetadata { get; set; } = new();

    public Summary() { }
}

public class SummaryContent
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("action_required")]
    public string ActionRequired { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("important")]
    public bool IsImportant { get; set; }

    [JsonPropertyName("confidence_score")]
    public decimal ConfidenceScore { get; set; }
    
    [JsonPropertyName("priority_score")]
    public decimal PriorityScore { get; set; }
}

public class SummaryMetadata
{
    public string Link { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string ExternalMessageId { get; set; } = string.Empty;
}

public class SummaryGenerationMetadata
{
    public string Provider { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public int InputTokens { get; set; }
    
    public int OutputTokens { get; set; }
    
    public int TotalTokens { get; set; }
    
    public double TimeTaken { get; set; }
}

public class SummaryDeliveryMetadata
{
    public string Provider { get; set; } = string.Empty;

    public string Channel { get; set; } = "email";

    public string MessageId { get; set; } = string.Empty;

    public string Status { get; set; } = DeliveryStatuses.Pending;

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime? SentAtUtc { get; set; }

    public int AttemptCount { get; set; } = 0;
}

public static class DeliveryStatuses
{
    public const string Pending = "pending";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
}

public static class EmailProviders
{
    public const string Postmark = "postmark";
}
