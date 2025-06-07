using System.Net;
using HtmlAgilityPack;
using InboxWrap.Models;

namespace InboxWrap.Updaters;

public class StripHtmlUpdater : IMailUpdater
{
    public List<Mail> Update(List<Mail> emails)
    {
        foreach (Mail email in emails)
        {
            HtmlDocument doc = new();
            doc.LoadHtml(email.Body);
            email.Body = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        }

        return emails;
    }
}
