using Internova.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Internova.Infrastructure.Data;

/// <summary>
/// EF Core database context for Internova targeting Azure SQL Server.
/// Table mapping aligned with dbo.Users created by DatabaseInitializer.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                  .UseIdentityColumn();

            entity.Property(u => u.FullName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(320);

            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("UX_Users_Email");

            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(u => u.CreatedAt)
                  .HasColumnType("datetime2")
                  .HasDefaultValueSql("SYSUTCDATETIME()");

            // Enforce Role constraint to match DB CHECK constraint
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role",
                "Role IN ('Student','Company','Admin')"));
        });
    }
}
