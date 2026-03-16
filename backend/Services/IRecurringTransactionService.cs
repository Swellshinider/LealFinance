namespace LealFinance.Api.Services;

/// <summary>
/// Generates due transactions from recurring schedules.
/// </summary>
public interface IRecurringTransactionService
{
    /// <summary>
    /// Generates all due recurring transactions across all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of generated transactions.</returns>
    Task<int> GenerateDueTransactionsForAllUsersAsync(CancellationToken cancellationToken);
}
