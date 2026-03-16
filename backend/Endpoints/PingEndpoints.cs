namespace LealFinance.Api.Endpoints;

/// <summary>
/// Provides ping endpoint mappings.
/// </summary>
public static class PingEndpoints
{
    /// <summary>
    /// Maps ping endpoints.
    /// </summary>
    /// <param name="app">Route builder instance.</param>
    /// <returns>The same route builder.</returns>
    public static IEndpointRouteBuilder MapPingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ping", () => Results.Json(new { message = "Pong from LealFinance API!" }));
        return app;
    }
}
