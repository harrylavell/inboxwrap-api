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
    
    public int EmailCount { get; set; } = 0;
    
    public List<SummaryItem>? FinanceAndBills { get; set; } = [];

    public List<SummaryItem>? EventsAndReminders { get; set; } = [];

    public List<SummaryItem>? SecurityAndAccount { get; set; } = [];

    public List<SummaryItem>? PersonalAndSocial { get; set; } = [];

    public List<SummaryItem>? PromotionsAndNewsletters { get; set; } = [];

    public int PromptionsAndNewslettersRemainder { get; set; } = 0;

    public List<SummaryItem>? EntertainmentAndGaming { get; set; } = [];

    public string PreferencesUrl { get; set; } = string.Empty;
    
    public string UnsubscribeUrl { get; set; } = string.Empty;
}
