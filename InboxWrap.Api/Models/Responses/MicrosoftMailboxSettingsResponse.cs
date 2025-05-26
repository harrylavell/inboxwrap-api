using System.Text.Json.Serialization;

namespace InboxWrap.Models.Responses;

public class MicrosoftMailboxSettingsResponse
{
    [JsonPropertyName("timeZone")]
    public required string TimeZone { get; set; }

    [JsonPropertyName("language")]
    public Language? Language { get; set; }
}

public class Language
{
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}
