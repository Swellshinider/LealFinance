namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents summary data displayed on the dashboard.
/// </summary>
public sealed class DashboardSummaryResponse
{
    /// <summary>
    /// Gets or sets the current computed account balance.
    /// </summary>
    public decimal TotalCurrentBalance { get; set; }

    /// <summary>
    /// Gets or sets the latest transactions for quick visualization.
    /// </summary>
    public IReadOnlyList<TransactionResponse> RecentTransactions { get; set; } = [];
}