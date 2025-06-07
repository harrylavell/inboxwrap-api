using System.Text.RegularExpressions;
using InboxWrap.Models;

namespace InboxWrap.Updaters;

public class TrimWhitespaceUpdater : IMailUpdater
{   
    private static readonly Regex InvisibleCharRegex = new(@"[\u0000-\u001F\u007F-\u009F\u00AD\u034F\u061C\u115F\u1160\u17B4\u17B5\u180B-\u180D\u200B-\u200F\u202A-\u202E\u2060-\u206F\uFEFF]", RegexOptions.Compiled);
    private static readonly Regex LetterDigitSpacing = new(@"(?<=[A-Za-z])(?=\d)|(?<=\d)(?=[A-Za-z])", RegexOptions.Compiled);
    private static readonly Regex LetterDollarSpacing = new(@"(?<=[A-Za-z])(?=\$)", RegexOptions.Compiled);

    public List<Mail> Update(List<Mail> emails)
    {
        foreach (Mail email in emails)
        {
            if (string.IsNullOrWhiteSpace(email.Body))
            {
                continue;
            }

            // Replace invisible characters with a single space
            email.Body = InvisibleCharRegex.Replace(email.Body, " ");

            // Add spacing where missing
            email.Body = LetterDigitSpacing.Replace(email.Body, " ");
            email.Body = LetterDollarSpacing.Replace(email.Body, " ");

            // Collapse multiple spaces
            email.Body = Regex.Replace(email.Body, @"[\u00A0\s]{2,}", " ");

            // Trim leading/trailing whitespace
            email.Body = email.Body.Trim();
        }

        return emails;
    }
}
