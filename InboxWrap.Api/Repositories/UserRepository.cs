using InboxWrap.Models;

namespace InboxWrap.Repositories;

public interface IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id);
    
    public User? GetByEmailAddress(string emailAddress);

    public IEnumerable<User> GetAll();

    public Task AddAsync(User user);

    public void Update(User user);
    
    public void Delete(User user);

    public Task<bool> SaveChangesAsync();
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

    public User? GetByEmailAddress(string emailAddress) =>
        _db.Users.FirstOrDefault(u => u.EmailAddress == emailAddress);

    public IEnumerable<User> GetAll() =>
        _db.Users.ToList();

    public async Task AddAsync(User user) =>
        await _db.Users.AddAsync(user);

    public void Update(User user) =>
        _db.Users.Update(user);

    public void Delete(User user) =>
        _db.Users.Remove(user);

    public async Task<bool> SaveChangesAsync() =>
        await _db.SaveChangesAsync() > 0;
}
