using InboxWrap.Models;

namespace InboxWrap.Repositories;

public interface IConnectedAccountRepository
{
    public Task<ConnectedAccount?> GetByIdAsync(Guid id);
    
    public IEnumerable<ConnectedAccount> GetAll();

    public Task AddAsync(ConnectedAccount connectedAccount);

    public void Update(ConnectedAccount connectedAccount);
    
    public void Delete(ConnectedAccount connectedAccount);
    
    public Task<bool> SaveChangesAsync();
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

    public IEnumerable<ConnectedAccount> GetAll() =>
        _db.ConnectedAccounts.ToList();

    public async Task AddAsync(ConnectedAccount connectedAccount) =>
        await _db.ConnectedAccounts.AddAsync(connectedAccount);

    public void Update(ConnectedAccount connectedAccount) =>
        _db.ConnectedAccounts.Update(connectedAccount);

    public void Delete(ConnectedAccount connectedAccount) =>
        _db.ConnectedAccounts.Remove(connectedAccount);

    public async Task<bool> SaveChangesAsync() =>
        await _db.SaveChangesAsync() > 0;
}
