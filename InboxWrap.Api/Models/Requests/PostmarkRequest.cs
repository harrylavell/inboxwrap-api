namespace InboxWrap.Models.Requests;

public interface IPostmarkTemplateModel { }

public class PostmarkRequest
{
    public string TemplateId { get; set; } = string.Empty;
    
    public IPostmarkTemplateModel? TemplateModel { get; set; }

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;
}

public class DailySummaryTemplateModel : IPostmarkTemplateModel
{

}
