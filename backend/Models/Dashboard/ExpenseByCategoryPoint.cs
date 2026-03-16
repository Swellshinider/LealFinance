namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents expense totals grouped by category for pie charts.
/// </summary>
public sealed class ExpenseByCategoryPoint
{
    /// <summary>
    /// Gets or sets the category label.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summed amount.
    /// </summary>
    public decimal TotalAmount { get; set; }
}