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

    /// <summary>
    /// Gets the recurring transactions table.
    /// </summary>
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).IsRequired().HasMaxLength(256);
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(120);
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.Property(user => user.RefreshToken).HasMaxLength(512);
            entity.Property(user => user.RefreshTokenExpiryTime);
            entity.Property(user => user.ProfilePhotoUrl).HasMaxLength(10000000);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.Date });
            entity.HasIndex(transaction => new { transaction.RecurringTransactionId, transaction.RecurringSequenceNumber }).IsUnique();
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
            entity.HasOne<RecurringTransaction>()
                .WithMany()
                .HasForeignKey(transaction => transaction.RecurringTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.HasKey(recurring => recurring.Id);
            entity.HasIndex(recurring => new { recurring.UserId, recurring.IsActive, recurring.NextOccurrenceDateUtc });
            entity.Property(recurring => recurring.Name).IsRequired().HasMaxLength(120);
            entity.Property(recurring => recurring.Type).IsRequired().HasMaxLength(24);
            entity.Property(recurring => recurring.Amount).HasPrecision(18, 2);
            entity.Property(recurring => recurring.Category).IsRequired().HasMaxLength(100);
            entity.Property(recurring => recurring.Notes).HasMaxLength(500);
            entity.Property(recurring => recurring.StartDateUtc).IsRequired();
            entity.Property(recurring => recurring.FrequencyUnit).IsRequired().HasMaxLength(16);
            entity.Property(recurring => recurring.FrequencyInterval).IsRequired();
            entity.Property(recurring => recurring.StartPaymentNumber).IsRequired();
            entity.Property(recurring => recurring.GeneratedOccurrences).IsRequired();
            entity.Property(recurring => recurring.IsInfinite).IsRequired();
            entity.Property(recurring => recurring.IsActive).IsRequired();
            entity.Property(recurring => recurring.CreatedAtUtc).IsRequired();
            entity.Property(recurring => recurring.UpdatedAtUtc).IsRequired();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(recurring => recurring.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
