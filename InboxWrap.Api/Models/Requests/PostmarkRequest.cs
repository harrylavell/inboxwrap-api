namespace InboxWrap.Models.Requests;

public abstract class PostmarkTemplateModel { }

public class PostmarkRequest
{
    public string TemplateId { get; set; } = string.Empty;
    
    public PostmarkTemplateModel? TemplateModel { get; set; }

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;
}

public class DailySummaryTemplateModel : PostmarkTemplateModel
{
    public string Subject { get; set; } = string.Empty;
    
    public string Date { get; set; } = string.Empty;
    
    public string IntroText { get; set; } = string.Empty;
    
    public List<SummaryContent> TopPicks { get; set; } = [];
    
    public List<SummaryContent> ImportantSummaries { get; set; } = [];
    
    public List<SummaryContent> OtherSummaries { get; set; } = [];

    public string PreferencesUrl { get; set; } = string.Empty;
    
    public string UnsubscribeUrl { get; set; } = string.Empty;
}
