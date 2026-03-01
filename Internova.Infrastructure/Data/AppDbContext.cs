using Internova.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Internova.Infrastructure.Data;

/// <summary>
/// EF Core database context for Internova.
/// Configures table mappings, constraints, and indexes.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);

            entity.Property(u => u.FullName)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            // Enforce uniqueness on email at the database level.
            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.ContactNumber)
                  .HasMaxLength(20);

            entity.Property(u => u.CreatedAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
