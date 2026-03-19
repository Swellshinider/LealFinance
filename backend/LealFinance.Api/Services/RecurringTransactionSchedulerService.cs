using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LealFinance.Api.Services;

/// <summary>
/// Background worker that periodically materializes due recurring transactions.
/// </summary>
public sealed class RecurringTransactionSchedulerService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var recurringTransactionService = scope.ServiceProvider.GetRequiredService<IRecurringTransactionService>();

            try
            {
                await recurringTransactionService.GenerateDueTransactionsForAllUsersAsync(stoppingToken);
            }
            catch
            {
                // Intentionally swallowed: recurring generation retries automatically on next poll.
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
