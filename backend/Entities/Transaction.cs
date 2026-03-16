using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Entities;

/// <summary>
/// Represents a financial transaction belonging to a user.
/// </summary>
public sealed class Transaction
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the owner user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the originating recurring transaction identifier when system-generated.
    /// </summary>
    public int? RecurringTransactionId { get; set; }

    /// <summary>
    /// Gets or sets the recurring payment sequence number when system-generated.
    /// </summary>
    public int? RecurringSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction type (<c>Income</c> or <c>Expense</c>).
    /// </summary>
    [Required]
    [MaxLength(24)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the user-defined income-or-expense category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string IncomeOrExpenseCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom transaction category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction date in UTC.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets optional transaction notes.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}