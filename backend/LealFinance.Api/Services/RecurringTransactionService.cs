using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Security;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LealFinance.Api.Services;

/// <summary>
/// Generates standard transactions from active recurring schedules.
/// </summary>
public sealed class RecurringTransactionService(LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService) : IRecurringTransactionService
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
        var usersById = await dbContext.Users.ToDictionaryAsync(user => user.Id, cancellationToken);

        foreach (var recurring in recurringItems)
        {
            if (!usersById.TryGetValue(recurring.UserId, out var user))
            {
                continue;
            }

            var recurringPayload = DecryptRecurringPayload(recurring, user);

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
                    ? recurringPayload.Name.Trim()
                    : $"{recurringPayload.Name.Trim()} {paymentNumber}/{recurring.MaxOccurrences.Value}";

                var notes = string.IsNullOrWhiteSpace(recurringPayload.Notes)
                    ? paymentLabel
                    : $"{paymentLabel} - {recurringPayload.Notes.Trim()}";

                var occurrenceDate = DateTime.SpecifyKind(recurring.NextOccurrenceDateUtc.Value.Date, DateTimeKind.Utc);

                var transactionPayload = new TransactionEncryptedPayload
                {
                    Type = recurringPayload.Type,
                    Amount = recurringPayload.Amount,
                    IncomeOrExpenseCategory = recurringPayload.Category,
                    Category = recurringPayload.Category,
                    Notes = notes
                };

                dbContext.Transactions.Add(new Transaction
                {
                    UserId = recurring.UserId,
                    RecurringTransactionId = recurring.Id,
                    RecurringSequenceNumber = paymentNumber,
                    Type = "Encrypted",
                    Amount = 0,
                    IncomeOrExpenseCategory = "Encrypted",
                    Category = "Encrypted",
                    Date = occurrenceDate,
                    Notes = null,
                    EncryptedPayload = EncryptPayload(transactionPayload, user),
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

    private string EncryptPayload<T>(T payload, User user)
        where T : class
    {
        var json = JsonSerializer.Serialize(payload);
        return dataEncryptionService.Encrypt(json, user.MasterPasswordHash, EnsureBase64Salt(user));
    }

    private RecurringEncryptedPayload DecryptRecurringPayload(RecurringTransaction recurring, User user)
    {
        if (!string.IsNullOrWhiteSpace(recurring.EncryptedPayload))
        {
            var json = dataEncryptionService.Decrypt(recurring.EncryptedPayload, user.MasterPasswordHash, EnsureBase64Salt(user));
            return JsonSerializer.Deserialize<RecurringEncryptedPayload>(json) ?? new RecurringEncryptedPayload();
        }

        return new RecurringEncryptedPayload
        {
            Name = recurring.Name,
            Type = recurring.Type,
            Amount = recurring.Amount,
            Category = recurring.Category,
            Notes = recurring.Notes
        };
    }

    private static string EnsureBase64Salt(User user)
    {
        if (string.IsNullOrWhiteSpace(user.DataEncryptionSalt))
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("default-user-salt"));
        }

        try
        {
            _ = Convert.FromBase64String(user.DataEncryptionSalt);
            return user.DataEncryptionSalt;
        }
        catch
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.DataEncryptionSalt));
        }
    }

    private sealed class RecurringEncryptedPayload
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    private sealed class TransactionEncryptedPayload
    {
        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string IncomeOrExpenseCategory { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
