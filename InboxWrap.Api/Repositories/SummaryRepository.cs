using InboxWrap.Models;

namespace InboxWrap.Repositories;

public interface ISummaryRepository
{
    Task<Summary?> GetByIdAsync(Guid id);
    
    IEnumerable<Summary> GetAll();
    
    IEnumerable<Summary> GetAllByUserId(Guid id);

    Task AddAsync(Summary summary);

    void Update(Summary summary);
    
    void Delete(Summary summary);

    Task<bool> SaveChangesAsync();
}

public class SummaryRepository : ISummaryRepository
{
    private readonly AppDbContext _db;

    public SummaryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Summary?> GetByIdAsync(Guid id) =>
        await _db.Summaries.FindAsync(id);

    public IEnumerable<Summary> GetAll() =>
        _db.Summaries.ToList();

    public IEnumerable<Summary> GetAllByUserId(Guid userId) =>
        _db.Summaries.Where(s => s.UserId == userId).ToList();

    public async Task AddAsync(Summary summary) =>
        await _db.Summaries.AddAsync(summary);

    public void Update(Summary summary) =>
        _db.Summaries.Update(summary);

    public void Delete(Summary summary) =>
        _db.Summaries.Remove(summary);
    
    public async Task<bool> SaveChangesAsync() =>
        await _db.SaveChangesAsync() > 0;
}
