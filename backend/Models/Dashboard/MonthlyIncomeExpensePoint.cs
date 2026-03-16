namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents monthly aggregated income and expense totals.
/// </summary>
public sealed class MonthlyIncomeExpensePoint
{
    /// <summary>
    /// Gets or sets the month label in <c>yyyy-MM</c> format.
    /// </summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total income amount for the month.
    /// </summary>
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Gets or sets the total expense amount for the month.
    /// </summary>
    public decimal TotalExpense { get; set; }
}