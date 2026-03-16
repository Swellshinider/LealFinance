using Microsoft.AspNetCore.Authorization;

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
        app.MapGet("/api/dashboard/summary", [Authorize] () =>
        {
            return Results.Ok(new
            {
                message = "Authenticated dashboard data loaded successfully.",
                generatedAtUtc = DateTime.UtcNow
            });
        });

        return app;
    }
}
