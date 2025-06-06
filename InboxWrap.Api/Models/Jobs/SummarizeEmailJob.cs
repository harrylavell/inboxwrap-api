namespace InboxWrap.Models.Jobs;

public class SummarizeEmailJob
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }

    public Guid ConnectedAccountId { get; set; }

    public string EmailId { get; set; } = string.Empty;
    
    public string Subject { get; set; } = string.Empty;
    
    public string Body { get; set; } = string.Empty;
    
    public string Link { get; set; } = string.Empty;
    
    public string Source { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Id} : {ConnectedAccountId} - {Subject}";
    }
}
