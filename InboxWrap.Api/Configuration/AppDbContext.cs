using InboxWrap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class AppDbContext : DbContext
{
    public required DbSet<User> Users { get; set; }
    
    public required DbSet<ConnectedAccount> ConnectedAccounts { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Preferences, p => {
                p.ToJson();
            });

        modelBuilder.Entity<ConnectedAccount>()
            .HasOne(c => c.User)
            .WithMany(u => u.ConnectedAccounts)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConnectedAccount>()
            .HasIndex(c => c.ProviderUserId)
            .IsUnique();

        modelBuilder.Entity<ConnectedAccount>()
            .HasIndex(c => c.UserId);

        base.OnModelCreating(modelBuilder);
    }

    // Automatically handle CreatedAt and UpdatedAt for all models that inherit BaseEntity
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityEntry<BaseEntity>> entries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAtUtc = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
