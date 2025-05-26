using System.Text.Json.Serialization;

namespace InboxWrap.Models.Responses;

public class MicrosoftMailResponse
{
    [JsonPropertyName("@odata.context")]
    public string? Context { get; set; }

    [JsonPropertyName("value")]
    public List<Message> Messages { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

public class Message
{
    [JsonPropertyName("@odata.etag")]
    public string OdataEtag { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("createdDateTime")]
    public DateTime CreatedDateTime { get; set; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime LastModifiedDateTime { get; set; }

    [JsonPropertyName("changeKey")]
    public string ChangeKey { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; }

    [JsonPropertyName("receivedDateTime")]
    public DateTime ReceivedDateTime { get; set; }

    [JsonPropertyName("sentDateTime")]
    public DateTime SentDateTime { get; set; }

    [JsonPropertyName("hasAttachments")]
    public bool HasAttachments { get; set; }

    [JsonPropertyName("internetMessageId")]
    public string InternetMessageId { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("bodyPreview")]
    public string BodyPreview { get; set; } = string.Empty;

    [JsonPropertyName("importance")]
    public string Importance { get; set; } = string.Empty;

    [JsonPropertyName("parentFolderId")]
    public string ParentFolderId { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("conversationIndex")]
    public string ConversationIndex { get; set; } = string.Empty;

    [JsonPropertyName("isDeliveryReceiptRequested")]
    public object? IsDeliveryReceiptRequested { get; set; }

    [JsonPropertyName("isReadReceiptRequested")]
    public bool IsReadReceiptRequested { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; }

    [JsonPropertyName("webLink")]
    public string WebLink { get; set; } = string.Empty;

    [JsonPropertyName("inferenceClassification")]
    public string? InferenceClassification { get; set; }

    [JsonPropertyName("body")]
    public MessageBody Body { get; set; } = new();

    [JsonPropertyName("sender")]
    public EmailRecipient Sender { get; set; } = new();

    [JsonPropertyName("from")]
    public EmailRecipient? From { get; set; }

    [JsonPropertyName("toRecipients")]
    public List<EmailRecipient>? ToRecipients { get; set; }

    [JsonPropertyName("ccRecipients")]
    public List<EmailRecipient>? CcRecipients { get; set; }

    [JsonPropertyName("bccRecipients")]
    public List<EmailRecipient>? BccRecipients { get; set; }

    [JsonPropertyName("replyTo")]
    public List<EmailRecipient>? ReplyTo { get; set; }

    [JsonPropertyName("flag")]
    public MessageFlag? Flag { get; set; }
}

public class MessageBody
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class EmailAddress
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}

public class MessageFlag
{
    [JsonPropertyName("flagStatus")]
    public string FlagStatus { get; set; } = string.Empty;
}

public class EmailRecipient
{
    [JsonPropertyName("emailAddress")]
    public EmailAddress? EmailAddress { get; set; }
}
