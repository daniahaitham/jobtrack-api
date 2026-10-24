using JobTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Data;

/// <summary>The bridge between our C# classes and the database tables.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Email).IsUnique(); // two accounts can't share an email
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<JobApplication>(e =>
        {
            e.Property(a => a.Company).HasMaxLength(200).IsRequired();
            e.Property(a => a.Role).HasMaxLength(200).IsRequired();
            e.Property(a => a.Link).HasMaxLength(500);
            e.Property(a => a.Notes).HasMaxLength(2000);

            // Store the status as readable text ("Interview") instead of a number.
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            // Deleting a user deletes their applications.
            e.HasOne(a => a.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
