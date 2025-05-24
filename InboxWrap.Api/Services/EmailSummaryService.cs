namespace InboxWrap.Services;

public interface IEmailSummaryService
{
    Task Run();
}

public class EmailSummaryService : IEmailSummaryService
{
    public Task Run()
    {
        // foreach user
            // check if user ready for summary based on their preferences
            
            // get all emails in past X hours between delivery times
            
            // pre-filter step
            
            // summarize via AI
            
            // post-filter step
            
            // send via postmark
        
        return Task.CompletedTask;
    }
}
