using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LealFinance.Api.Services;

/// <summary>
/// Generates standard transactions from active recurring schedules.
/// </summary>
public sealed class RecurringTransactionService(LealFinanceDbContext dbContext) : IRecurringTransactionService
{
    /// <inheritdoc />
    public async Task<int> GenerateDueTransactionsForAllUsersAsync(CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var recurringItems = await dbContext.RecurringTransactions
            .Where(item => item.IsActive && item.NextOccurrenceDateUtc.HasValue && item.NextOccurrenceDateUtc.Value <= todayUtc)
            .OrderBy(item => item.NextOccurrenceDateUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        if (recurringItems.Count == 0)
        {
            return 0;
        }

        var generatedCount = 0;

        foreach (var recurring in recurringItems)
        {
            while (recurring.IsActive && recurring.NextOccurrenceDateUtc.HasValue && recurring.NextOccurrenceDateUtc.Value <= todayUtc)
            {
                var paymentNumber = recurring.StartPaymentNumber + recurring.GeneratedOccurrences;

                if (!recurring.IsInfinite && recurring.MaxOccurrences.HasValue && paymentNumber > recurring.MaxOccurrences.Value)
                {
                    recurring.IsActive = false;
                    recurring.NextOccurrenceDateUtc = null;
                    recurring.UpdatedAtUtc = DateTime.UtcNow;
                    break;
                }

                var paymentLabel = recurring.IsInfinite || !recurring.MaxOccurrences.HasValue
                    ? recurring.Name.Trim()
                    : $"{recurring.Name.Trim()} {paymentNumber}/{recurring.MaxOccurrences.Value}";

                var notes = string.IsNullOrWhiteSpace(recurring.Notes)
                    ? paymentLabel
                    : $"{paymentLabel} - {recurring.Notes.Trim()}";

                var occurrenceDate = DateTime.SpecifyKind(recurring.NextOccurrenceDateUtc.Value.Date, DateTimeKind.Utc);

                dbContext.Transactions.Add(new Transaction
                {
                    UserId = recurring.UserId,
                    RecurringTransactionId = recurring.Id,
                    RecurringSequenceNumber = paymentNumber,
                    Type = recurring.Type,
                    Amount = recurring.Amount,
                    IncomeOrExpenseCategory = recurring.Category,
                    Category = recurring.Category,
                    Date = occurrenceDate,
                    Notes = notes,
                    CreatedAtUtc = DateTime.UtcNow
                });

                recurring.GeneratedOccurrences += 1;
                generatedCount += 1;

                var nextPaymentNumber = recurring.StartPaymentNumber + recurring.GeneratedOccurrences;
                if (!recurring.IsInfinite && recurring.MaxOccurrences.HasValue && nextPaymentNumber > recurring.MaxOccurrences.Value)
                {
                    recurring.IsActive = false;
                    recurring.NextOccurrenceDateUtc = null;
                }
                else
                {
                    recurring.NextOccurrenceDateUtc = DateTime.SpecifyKind(
                        AddByUnit(occurrenceDate, recurring.FrequencyUnit, recurring.FrequencyInterval).Date,
                        DateTimeKind.Utc);
                }

                recurring.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return generatedCount;
    }

    private static DateTime AddByUnit(DateTime date, string frequencyUnit, int interval)
    {
        return frequencyUnit switch
        {
            "Day" => date.AddDays(interval),
            "Week" => date.AddDays(interval * 7),
            "Month" => date.AddMonths(interval),
            "Year" => date.AddYears(interval),
            _ => throw new InvalidOperationException($"Unsupported recurring frequency unit: {frequencyUnit}")
        };
    }
}
