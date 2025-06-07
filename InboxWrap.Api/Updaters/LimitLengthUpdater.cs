using InboxWrap.Models;

namespace InboxWrap.Updaters;

public class LimitLengthUpdater : IMailUpdater
{
    private const int MAX_LENGTH = 1000;

    public List<Mail> Update(List<Mail> emails)
    {
        foreach (Mail email in emails)
        {
            email.Body = (email.Body.Length > MAX_LENGTH)
                ? email.Body = email.Body.Substring(0, MAX_LENGTH)
                : email.Body;
        }

        return emails;
    }
}
