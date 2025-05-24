using InboxWrap.Models;

namespace InboxWrap.Repositories;

public interface IConnectedAccountRepository
{
    Task<ConnectedAccount?> GetByIdAsync(Guid id);

    ConnectedAccount? GetByProviderUserId(string providerUserId);
    
    IEnumerable<ConnectedAccount> GetAll();

    Task AddAsync(ConnectedAccount connectedAccount);

    void Update(ConnectedAccount connectedAccount);
    
    void Delete(ConnectedAccount connectedAccount);

    bool ExistsByProviderUserId(string providerUserId);
    
    Task<bool> SaveChangesAsync();
}

public class ConnectedAccountRepository : IConnectedAccountRepository
{
    private readonly AppDbContext _db;

    public ConnectedAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ConnectedAccount?> GetByIdAsync(Guid id) =>
        await _db.ConnectedAccounts.FindAsync(id);

    public ConnectedAccount? GetByProviderUserId(string providerUserId) =>
        _db.ConnectedAccounts.FirstOrDefault(c => c.ProviderUserId == providerUserId);

    public IEnumerable<ConnectedAccount> GetAll() =>
        _db.ConnectedAccounts.ToList();

    public async Task AddAsync(ConnectedAccount connectedAccount) =>
        await _db.ConnectedAccounts.AddAsync(connectedAccount);

    public void Update(ConnectedAccount connectedAccount) =>
        _db.ConnectedAccounts.Update(connectedAccount);

    public void Delete(ConnectedAccount connectedAccount) =>
        _db.ConnectedAccounts.Remove(connectedAccount);

    public bool ExistsByProviderUserId(string providerUserId) =>
        _db.ConnectedAccounts.Any(c => c.ProviderUserId == providerUserId);

    public async Task<bool> SaveChangesAsync() =>
        await _db.SaveChangesAsync() > 0;
}
