using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LealFinance.Api.Common;
using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

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
        var dashboardRoutes = app.MapGroup("/api/dashboard").RequireAuthorization();

        dashboardRoutes.MapGet("/summary", async (HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var userTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .ToListAsync(cancellationToken);

            var totalCurrentBalance = userTransactions.Sum(transaction =>
                string.Equals(transaction.Type, "Income", StringComparison.OrdinalIgnoreCase)
                    ? transaction.Amount
                    : -transaction.Amount);

            var recentTransactions = userTransactions
                .OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .Take(5)
                .Select(MapTransaction)
                .ToList();

            return Results.Ok(new DashboardSummaryResponse
            {
                TotalCurrentBalance = totalCurrentBalance,
                RecentTransactions = recentTransactions
            });
        });

        dashboardRoutes.MapGet("/transactions", async (HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var transactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.CreatedAtUtc)
                .Select(transaction => new TransactionResponse
                {
                    Id = transaction.Id,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    Category = transaction.Category,
                    Date = transaction.Date,
                    Notes = transaction.Notes
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(transactions);
        });

        dashboardRoutes.MapPost("/transactions", async (TransactionCreateRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var transaction = new Transaction
            {
                UserId = userId,
                Type = request.Type.Trim(),
                Amount = request.Amount,
                IncomeOrExpenseCategory = request.Category.Trim(),
                Category = request.Category.Trim(),
                Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/dashboard/transactions/{transaction.Id}", MapTransaction(transaction));
        });

        dashboardRoutes.MapPut("/transactions/{transactionId:int}", async (int transactionId, TransactionUpdateRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var transaction = await dbContext.Transactions
                .SingleOrDefaultAsync(entity => entity.Id == transactionId && entity.UserId == userId, cancellationToken);

            if (transaction is null)
            {
                return Results.NotFound(new { message = "Transaction not found." });
            }

            transaction.Type = request.Type.Trim();
            transaction.Amount = request.Amount;
            transaction.IncomeOrExpenseCategory = request.Category.Trim();
            transaction.Category = request.Category.Trim();
            transaction.Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
            transaction.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(MapTransaction(transaction));
        });

        dashboardRoutes.MapDelete("/transactions/{transactionId:int}", async (int transactionId, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
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

        dashboardRoutes.MapGet("/reports", async (HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var userTransactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId)
                .ToListAsync(cancellationToken);

            var expensesByCategory = userTransactions
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
                .GroupBy(transaction => new { transaction.Date.Year, transaction.Date.Month })
                .Select(group => new MonthlyIncomeExpensePoint
                {
                    Month = $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                    TotalIncome = group
                        .Where(transaction => string.Equals(transaction.Type, "Income", StringComparison.OrdinalIgnoreCase))
                        .Sum(transaction => transaction.Amount),
                    TotalExpense = group
                        .Where(transaction => string.Equals(transaction.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                        .Sum(transaction => transaction.Amount)
                })
                .OrderBy(item => item.Month)
                .ToList();

            return Results.Ok(new DashboardReportsResponse
            {
                ExpensesByCategory = expensesByCategory,
                MonthlyIncomeVsExpense = monthlyIncomeVsExpense
            });
        });

        dashboardRoutes.MapGet("/recurring", async (HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var recurringItems = await dbContext.RecurringTransactions
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.NextOccurrenceDateUtc)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            return Results.Ok(recurringItems.Select(MapRecurring));
        });

        dashboardRoutes.MapPost("/recurring", async (RecurringTransactionUpsertRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var startDateUtc = NormalizeUtcDate(request.StartDate);

            var recurring = new RecurringTransaction
            {
                UserId = userId,
                Name = request.Name.Trim(),
                Type = request.Type.Trim(),
                Amount = request.Amount,
                Category = request.Category.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
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

            return Results.Created($"/api/dashboard/recurring/{recurring.Id}", MapRecurring(recurring));
        });

        dashboardRoutes.MapPut("/recurring/{recurringId:int}", async (int recurringId, RecurringTransactionUpsertRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var recurring = await dbContext.RecurringTransactions
                .SingleOrDefaultAsync(item => item.Id == recurringId && item.UserId == userId, cancellationToken);

            if (recurring is null)
            {
                return Results.NotFound(new { message = "Recurring transaction not found." });
            }

            recurring.Name = request.Name.Trim();
            recurring.Type = request.Type.Trim();
            recurring.Amount = request.Amount;
            recurring.Category = request.Category.Trim();
            recurring.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
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

            return Results.Ok(MapRecurring(recurring));
        });

        dashboardRoutes.MapDelete("/recurring/{recurringId:int}", async (int recurringId, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
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

    private static TransactionResponse MapTransaction(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Category = transaction.Category,
            Date = transaction.Date,
            Notes = transaction.Notes
        };
    }

    private static RecurringTransactionResponse MapRecurring(RecurringTransaction recurring)
    {
        var nextPaymentNumber = recurring.StartPaymentNumber + recurring.GeneratedOccurrences;
        int? remainingPayments = recurring.IsInfinite || !recurring.MaxOccurrences.HasValue
            ? null
            : Math.Max(0, recurring.MaxOccurrences.Value - nextPaymentNumber + 1);

        return new RecurringTransactionResponse
        {
            Id = recurring.Id,
            Name = recurring.Name,
            Type = recurring.Type,
            Amount = recurring.Amount,
            Category = recurring.Category,
            Notes = recurring.Notes,
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
}
