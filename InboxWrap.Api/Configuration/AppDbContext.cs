using System.Text.Json;
using InboxWrap.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public required DbSet<User> Users { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Preferences, p => {
                p.ToJson();
            });

        base.OnModelCreating(modelBuilder);
    }
}
