using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents the payload to create a transaction.
/// </summary>
public sealed class TransactionCreateRequest
{
    /// <summary>
    /// Gets or sets the transaction type (<c>Income</c> or <c>Expense</c>).
    /// </summary>
    [Required]
    [RegularExpression("^(Income|Expense)$", ErrorMessage = "Type must be either Income or Expense.")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction date.
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets optional notes.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}