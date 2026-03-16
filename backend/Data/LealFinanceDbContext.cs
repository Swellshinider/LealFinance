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

    /// <summary>
    /// Gets the transactions table.
    /// </summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

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

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.Date });
            entity.Property(transaction => transaction.Type).IsRequired().HasMaxLength(24);
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.IncomeOrExpenseCategory).IsRequired().HasMaxLength(100);
            entity.Property(transaction => transaction.Category).IsRequired().HasMaxLength(100);
            entity.Property(transaction => transaction.Date).IsRequired();
            entity.Property(transaction => transaction.Notes).HasMaxLength(500);
            entity.Property(transaction => transaction.CreatedAtUtc).IsRequired();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
