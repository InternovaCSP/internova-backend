using Internova.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Internova.Infrastructure.Data;

/// <summary>
/// EF Core database context for Internova targeting local MySQL.
/// Table mapping aligned with Users table created by DatabaseInitializer.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Map to singular [User] table in dbo schema
            entity.ToTable("User", "dbo", t => t.HasCheckConstraint(
                "CK_Users_Role",
                "role IN ('Student', 'Company', 'Admin', 'Faculty', 'Organizer')"));

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                  .HasColumnName("user_id");

            entity.Property(u => u.FullName)
                  .HasColumnName("full_name")
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(u => u.Email)
                  .HasColumnName("email")
                  .IsRequired()
                  .HasMaxLength(255);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .HasColumnName("password_hash")
                  .IsRequired();

            entity.Property(u => u.Role)
                  .HasColumnName("role")
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("GETDATE()");
        });
    }
}
