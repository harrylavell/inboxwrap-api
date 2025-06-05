using InboxWrap.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class AppDbContext : DbContext
{
    public required DbSet<User> Users { get; set; }
    
    public required DbSet<ConnectedAccount> ConnectedAccounts { get; set; }
    
    public required DbSet<Summary> Summaries { get; set; }

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

        modelBuilder.Entity<Summary>()
            .OwnsOne(u => u.Content, p => {
                p.ToJson();
            });

        modelBuilder.Entity<Summary>()
            .OwnsOne(u => u.Metadata, p => {
                p.ToJson();
            });
        
        modelBuilder.Entity<Summary>()
            .OwnsOne(u => u.GenerationMetadata, p => {
                p.ToJson();
            });

        modelBuilder.Entity<Summary>()
            .OwnsOne(u => u.DeliveryMetadata, p => {
                p.ToJson();
            });

        modelBuilder.Entity<Summary>()
            .HasOne(s => s.User)
            .WithMany(u => u.Summaries)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Summary>()
            .HasOne(s => s.ConnectedAccount)
            .WithMany(c => c.Summaries)
            .HasForeignKey(s => s.ConnectedAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Summary>()
            .HasIndex(s => s.UserId);
        
        modelBuilder.Entity<Summary>()
            .HasIndex(s => s.ConnectedAccountId);

        modelBuilder.Entity<Summary>()
            .HasIndex(s => s.DeliveryStatus);

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
