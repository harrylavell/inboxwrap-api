using InboxWrap.Constants;
using InboxWrap.Models.Responses;

namespace InboxWrap.Models;

public class Mail
{
    public string Id { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; }

    public string Link { get; set; }
    
    public string Source { get; set; }

    public Mail(MicrosoftMessage message)
    {
        Id = message.Id;
        Subject = message.Subject;
        Body = message.Body.Content;
        Link = message.WebLink;
        Source = Providers.Microsoft;
    }
}
