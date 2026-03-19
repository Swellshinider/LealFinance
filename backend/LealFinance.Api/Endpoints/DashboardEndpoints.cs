using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LealFinance.Api.Common;
using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Models.Dashboard;
using LealFinance.Api.Security;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LealFinance.Api.Endpoints;

/// <summary>
/// Provides dashboard endpoint mappings.
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Maps dashboard endpoints.
    /// </summary>
    /// <param name="app">Route builder instance.</param>
    /// <returns>The same route builder.</returns>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var dashboardRoutes = app.MapGroup("/api/dashboard").RequireAuthorization().RequireRateLimiting("ApiFixed");

        dashboardRoutes.MapGet("/summary", async (HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var userTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .ToListAsync(cancellationToken);

            var projectedTransactions = userTransactions
                .Select(transaction => new ProjectedTransaction
                {
                    Transaction = transaction,
                    Payload = DecryptTransactionPayload(transaction, user, dataEncryptionService)
                })
                .ToList();

            var totalCurrentBalance = projectedTransactions.Sum(item =>
                string.Equals(item.Payload.Type, "Income", StringComparison.OrdinalIgnoreCase)
                    ? item.Payload.Amount
                    : -item.Payload.Amount);

            var recentTransactions = projectedTransactions
                .OrderByDescending(item => item.Transaction.Date)
                .ThenByDescending(item => item.Transaction.CreatedAtUtc)
                .Take(5)
                .Select(MapTransaction)
                .ToList();

            return Results.Ok(new DashboardSummaryResponse
            {
                TotalCurrentBalance = totalCurrentBalance,
                RecentTransactions = recentTransactions
            });
        });

        dashboardRoutes.MapGet("/transactions", async (HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var transactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var response = transactions
                .Select(transaction => MapTransaction(new ProjectedTransaction
                {
                    Transaction = transaction,
                    Payload = DecryptTransactionPayload(transaction, user, dataEncryptionService)
                }))
                .ToList();

            return Results.Ok(response);
        });

        dashboardRoutes.MapPost("/transactions", async (TransactionCreateRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var payload = new TransactionEncryptedPayload
            {
                Type = request.Type.Trim(),
                Amount = request.Amount,
                IncomeOrExpenseCategory = request.Category.Trim(),
                Category = request.Category.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            var transaction = new Transaction
            {
                UserId = userId,
                Type = "Encrypted",
                Amount = 0,
                IncomeOrExpenseCategory = "Encrypted",
                Category = "Encrypted",
                Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
                Notes = null,
                EncryptedPayload = EncryptPayload(payload, user, dataEncryptionService),
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/dashboard/transactions/{transaction.Id}", MapTransaction(new ProjectedTransaction
            {
                Transaction = transaction,
                Payload = payload
            }));
        });

        dashboardRoutes.MapPut("/transactions/{transactionId:int}", async (int transactionId, TransactionUpdateRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var transaction = await dbContext.Transactions
                .SingleOrDefaultAsync(entity => entity.Id == transactionId && entity.UserId == userId, cancellationToken);

            if (transaction is null)
            {
                return Results.NotFound(new { message = "Transaction not found." });
            }

            var payload = new TransactionEncryptedPayload
            {
                Type = request.Type.Trim(),
                Amount = request.Amount,
                IncomeOrExpenseCategory = request.Category.Trim(),
                Category = request.Category.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            transaction.Type = "Encrypted";
            transaction.Amount = 0;
            transaction.IncomeOrExpenseCategory = "Encrypted";
            transaction.Category = "Encrypted";
            transaction.Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
            transaction.Notes = null;
            transaction.EncryptedPayload = EncryptPayload(payload, user, dataEncryptionService);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MapTransaction(new ProjectedTransaction
            {
                Transaction = transaction,
                Payload = payload
            }));
        });

        dashboardRoutes.MapDelete("/transactions/{transactionId:int}", async (int transactionId, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var transaction = await dbContext.Transactions
                .SingleOrDefaultAsync(entity => entity.Id == transactionId && entity.UserId == userId, cancellationToken);

            if (transaction is null)
            {
                return Results.NotFound(new { message = "Transaction not found." });
            }

            dbContext.Transactions.Remove(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { message = "Transaction deleted successfully." });
        });

        dashboardRoutes.MapGet("/reports", async (HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var userTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .ToListAsync(cancellationToken);

            var projectedTransactions = userTransactions
                .Select(transaction => DecryptTransactionPayload(transaction, user, dataEncryptionService))
                .ToList();

            var expensesByCategory = projectedTransactions
                .Where(transaction => string.Equals(transaction.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                .GroupBy(transaction => transaction.Category)
                .Select(group => new ExpenseByCategoryPoint
                {
                    Category = group.Key,
                    TotalAmount = group.Sum(transaction => transaction.Amount)
                })
                .OrderByDescending(item => item.TotalAmount)
                .ToList();

            var monthlyIncomeVsExpense = userTransactions
                .Select((transaction, index) => new { Transaction = transaction, Payload = projectedTransactions[index] })
                .GroupBy(item => new { item.Transaction.Date.Year, item.Transaction.Date.Month })
                .Select(group => new MonthlyIncomeExpensePoint
                {
                    Month = $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                    TotalIncome = group
                        .Where(item => string.Equals(item.Payload.Type, "Income", StringComparison.OrdinalIgnoreCase))
                        .Sum(item => item.Payload.Amount),
                    TotalExpense = group
                        .Where(item => string.Equals(item.Payload.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                        .Sum(item => item.Payload.Amount)
                })
                .OrderBy(item => item.Month)
                .ToList();

            return Results.Ok(new DashboardReportsResponse
            {
                ExpensesByCategory = expensesByCategory,
                MonthlyIncomeVsExpense = monthlyIncomeVsExpense
            });
        });

        dashboardRoutes.MapGet("/recurring", async (HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var recurringItems = await dbContext.RecurringTransactions
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.NextOccurrenceDateUtc)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            return Results.Ok(recurringItems.Select(item => MapRecurring(item, DecryptRecurringPayload(item, user, dataEncryptionService))));
        });

        dashboardRoutes.MapPost("/recurring", async (RecurringTransactionUpsertRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var startDateUtc = NormalizeUtcDate(request.StartDate);
            var payload = new RecurringEncryptedPayload
            {
                Name = request.Name.Trim(),
                Type = request.Type.Trim(),
                Amount = request.Amount,
                Category = request.Category.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            var recurring = new RecurringTransaction
            {
                UserId = userId,
                Name = "Encrypted",
                Type = "Encrypted",
                Amount = 0,
                Category = "Encrypted",
                Notes = null,
                EncryptedPayload = EncryptPayload(payload, user, dataEncryptionService),
                StartDateUtc = startDateUtc,
                FrequencyUnit = request.FrequencyUnit.Trim(),
                FrequencyInterval = request.FrequencyInterval,
                IsInfinite = request.IsInfinite,
                MaxOccurrences = request.IsInfinite ? null : request.MaxOccurrences,
                StartPaymentNumber = request.StartPaymentNumber,
                GeneratedOccurrences = 0,
                NextOccurrenceDateUtc = startDateUtc,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            if (!recurring.IsInfinite && recurring.MaxOccurrences.HasValue && recurring.StartPaymentNumber > recurring.MaxOccurrences.Value)
            {
                recurring.IsActive = false;
                recurring.NextOccurrenceDateUtc = null;
            }

            dbContext.RecurringTransactions.Add(recurring);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/dashboard/recurring/{recurring.Id}", MapRecurring(recurring, payload));
        });

        dashboardRoutes.MapPut("/recurring/{recurringId:int}", async (int recurringId, RecurringTransactionUpsertRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var recurring = await dbContext.RecurringTransactions
                .SingleOrDefaultAsync(item => item.Id == recurringId && item.UserId == userId, cancellationToken);

            if (recurring is null)
            {
                return Results.NotFound(new { message = "Recurring transaction not found." });
            }

            var payload = new RecurringEncryptedPayload
            {
                Name = request.Name.Trim(),
                Type = request.Type.Trim(),
                Amount = request.Amount,
                Category = request.Category.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            recurring.Name = "Encrypted";
            recurring.Type = "Encrypted";
            recurring.Amount = 0;
            recurring.Category = "Encrypted";
            recurring.Notes = null;
            recurring.EncryptedPayload = EncryptPayload(payload, user, dataEncryptionService);
            recurring.StartDateUtc = NormalizeUtcDate(request.StartDate);
            recurring.FrequencyUnit = request.FrequencyUnit.Trim();
            recurring.FrequencyInterval = request.FrequencyInterval;
            recurring.IsInfinite = request.IsInfinite;
            recurring.MaxOccurrences = request.IsInfinite ? null : request.MaxOccurrences;
            recurring.StartPaymentNumber = request.StartPaymentNumber;

            if (recurring.IsActive)
            {
                if (!recurring.IsInfinite
                    && recurring.MaxOccurrences.HasValue
                    && recurring.StartPaymentNumber + recurring.GeneratedOccurrences > recurring.MaxOccurrences.Value)
                {
                    recurring.IsActive = false;
                    recurring.NextOccurrenceDateUtc = null;
                }
                else if (recurring.GeneratedOccurrences == 0)
                {
                    recurring.NextOccurrenceDateUtc = recurring.StartDateUtc;
                }
                else if (!recurring.NextOccurrenceDateUtc.HasValue)
                {
                    recurring.NextOccurrenceDateUtc = NormalizeUtcDate(DateTime.UtcNow);
                }
            }

            recurring.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MapRecurring(recurring, payload));
        });

        dashboardRoutes.MapDelete("/recurring/{recurringId:int}", async (int recurringId, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            if (!user.IsTwoFactorEnabled)
            {
                return Results.Forbid();
            }

            var recurring = await dbContext.RecurringTransactions
                .SingleOrDefaultAsync(item => item.Id == recurringId && item.UserId == userId, cancellationToken);

            if (recurring is null)
            {
                return Results.NotFound(new { message = "Recurring transaction not found." });
            }

            recurring.IsActive = false;
            recurring.NextOccurrenceDateUtc = null;
            recurring.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { message = "Recurring transaction canceled successfully." });
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId)
    {
        var userIdValue = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out userId);
    }

    private static TransactionResponse MapTransaction(ProjectedTransaction source)
    {
        return new TransactionResponse
        {
            Id = source.Transaction.Id,
            Type = source.Payload.Type,
            Amount = source.Payload.Amount,
            Category = source.Payload.Category,
            Date = source.Transaction.Date,
            Notes = source.Payload.Notes
        };
    }

    private static RecurringTransactionResponse MapRecurring(RecurringTransaction recurring, RecurringEncryptedPayload payload)
    {
        var nextPaymentNumber = recurring.StartPaymentNumber + recurring.GeneratedOccurrences;
        int? remainingPayments = recurring.IsInfinite || !recurring.MaxOccurrences.HasValue
            ? null
            : Math.Max(0, recurring.MaxOccurrences.Value - nextPaymentNumber + 1);

        return new RecurringTransactionResponse
        {
            Id = recurring.Id,
            Name = payload.Name,
            Type = payload.Type,
            Amount = payload.Amount,
            Category = payload.Category,
            Notes = payload.Notes,
            StartDate = recurring.StartDateUtc,
            FrequencyUnit = recurring.FrequencyUnit,
            FrequencyInterval = recurring.FrequencyInterval,
            IsInfinite = recurring.IsInfinite,
            MaxOccurrences = recurring.MaxOccurrences,
            StartPaymentNumber = recurring.StartPaymentNumber,
            GeneratedOccurrences = recurring.GeneratedOccurrences,
            NextOccurrenceDate = recurring.NextOccurrenceDateUtc,
            IsActive = recurring.IsActive,
            RemainingPayments = remainingPayments
        };
    }

    private static DateTime NormalizeUtcDate(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private static string EncryptPayload<T>(T payload, User user, IDataEncryptionService dataEncryptionService)
        where T : class
    {
        var json = JsonSerializer.Serialize(payload);
        return dataEncryptionService.Encrypt(json, user.MasterPasswordHash, EnsureBase64Salt(user));
    }

    private static TransactionEncryptedPayload DecryptTransactionPayload(Transaction transaction, User user, IDataEncryptionService dataEncryptionService)
    {
        if (!string.IsNullOrWhiteSpace(transaction.EncryptedPayload))
        {
            var json = dataEncryptionService.Decrypt(transaction.EncryptedPayload, user.MasterPasswordHash, EnsureBase64Salt(user));
            return JsonSerializer.Deserialize<TransactionEncryptedPayload>(json) ?? new TransactionEncryptedPayload();
        }

        return new TransactionEncryptedPayload
        {
            Type = transaction.Type,
            Amount = transaction.Amount,
            IncomeOrExpenseCategory = transaction.IncomeOrExpenseCategory,
            Category = transaction.Category,
            Notes = transaction.Notes
        };
    }

    private static RecurringEncryptedPayload DecryptRecurringPayload(RecurringTransaction recurring, User user, IDataEncryptionService dataEncryptionService)
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

    private sealed class TransactionEncryptedPayload
    {
        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string IncomeOrExpenseCategory { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    private sealed class RecurringEncryptedPayload
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Category { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    private sealed class ProjectedTransaction
    {
        public required Transaction Transaction { get; set; }

        public required TransactionEncryptedPayload Payload { get; set; }
    }
}
