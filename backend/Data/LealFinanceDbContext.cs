using LealFinance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LealFinance.Api.Data;

/// <summary>
/// EF Core database context for LealFinance.
/// </summary>
public sealed class LealFinanceDbContext(DbContextOptions<LealFinanceDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).IsRequired().HasMaxLength(256);
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.Property(user => user.RefreshToken).HasMaxLength(512);
            entity.Property(user => user.RefreshTokenExpiryTime);
        });

        base.OnModelCreating(modelBuilder);
    }
}
