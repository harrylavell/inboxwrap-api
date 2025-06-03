using System.Text.Json.Serialization;

namespace InboxWrap.Models.Responses;

public class PostmarkResponse
{
    [JsonPropertyName("To")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("SubmittedAt")]
    public DateTime SubmittedAt { get; set; }

    [JsonPropertyName("MessageID")]
    public string MessageID { get; set; } = string.Empty;

    [JsonPropertyName("ErrorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
}
