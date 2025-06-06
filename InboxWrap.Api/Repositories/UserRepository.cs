using InboxWrap.Models;
using Microsoft.EntityFrameworkCore;

namespace InboxWrap.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    
    User? GetByEmail(string email);
    
    IEnumerable<User> GetAll();
    
    IEnumerable<User> GetDueForSummary(DateTime utcNow);

    Task AddAsync(User user);

    void Update(User user);
    
    void Delete(User user);
    
    bool ExistsByEmail(string email);

    Task<bool> SaveChangesAsync();
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _db.Users.FindAsync(id);

    public User? GetByEmail(string email) =>
        _db.Users.FirstOrDefault(u => u.Email == email);

    public IEnumerable<User> GetAll() =>
        _db.Users
            .Include(u => u.ConnectedAccounts)
            .Include(u => u.Summaries)
            .AsSplitQuery()
            .ToList();

    public IEnumerable<User> GetDueForSummary(DateTime utcNow) =>
        _db.Users
            .Include(u => u.ConnectedAccounts)
            .Include(u => u.Summaries)
            .AsSplitQuery()
            .Where(u => u.NextDeliveryUtc <= utcNow);

    public async Task AddAsync(User user) =>
        await _db.Users.AddAsync(user);

    public void Update(User user) =>
        _db.Users.Update(user);

    public void Delete(User user) =>
        _db.Users.Remove(user);

    public bool ExistsByEmail(string email) =>
        _db.Users.Any(u => u.Email == email);

    public async Task<bool> SaveChangesAsync() =>
        await _db.SaveChangesAsync() > 0;
}
