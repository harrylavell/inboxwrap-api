using InboxWrap.Models;

namespace InboxWrap.Updaters;

public interface IMailUpdater
{
    List<Mail> Update(List<Mail> emails); 
}
