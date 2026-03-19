namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents report datasets for the dashboard charts.
/// </summary>
public sealed class DashboardReportsResponse
{
    /// <summary>
    /// Gets or sets pie chart dataset of expenses by category.
    /// </summary>
    public IReadOnlyList<ExpenseByCategoryPoint> ExpensesByCategory { get; set; } = [];

    /// <summary>
    /// Gets or sets bar chart dataset of monthly income versus expense.
    /// </summary>
    public IReadOnlyList<MonthlyIncomeExpensePoint> MonthlyIncomeVsExpense { get; set; } = [];
}