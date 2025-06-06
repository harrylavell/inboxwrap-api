using System.Text.Json.Serialization;
using InboxWrap.Models.Responses;

namespace InboxWrap.Models.Requests;

public class GroqRequest
{
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<GroqMessage> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public decimal Temperature { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; }

    [JsonPropertyName("response_format")]
    public GroqFormat ResponseFormat { get; set; } = new();
}

public class GroqFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

