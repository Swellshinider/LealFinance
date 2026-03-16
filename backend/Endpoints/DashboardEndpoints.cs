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
}
