using System.Text.Json.Serialization;

namespace InboxWrap.Models.Reponses;

public class SecretsResponse
{
    [JsonPropertyName("secrets")]
    public List<Secret>? Secrets { get; set; }

    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; set; }
}

public class Secret
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("latest_version")]
    public int LatestVersion { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("created_by_id")]
    public string? CreatedById { get; set; }

    [JsonPropertyName("sync_status")]
    public Dictionary<string, object>? SyncStatus { get; set; }

    [JsonPropertyName("static_version")]
    public StaticVersion? StaticVersion { get; set; }
}

public class StaticVersion
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("created_by_id")]
    public string? CreatedById { get; set; }
}

public class Pagination
{
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }

    [JsonPropertyName("previous_page_token")]
    public string? PreviousPageToken { get; set; }
}
