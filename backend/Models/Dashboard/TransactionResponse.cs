namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents transaction data returned to the client.
/// </summary>
public sealed class TransactionResponse
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets optional notes.
    /// </summary>
    public string? Notes { get; set; }
}