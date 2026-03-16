namespace LealFinance.Api.Endpoints;

/// <summary>
/// Provides root endpoint registration for the API.
/// </summary>
public static class EndpointMappings
{
    /// <summary>
    /// Maps all API endpoints.
    /// </summary>
    /// <param name="app">Route builder instance.</param>
    /// <returns>The same route builder.</returns>
    public static IEndpointRouteBuilder MapLealFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPingEndpoints();
        app.MapAuthEndpoints();
        app.MapDashboardEndpoints();

        return app;
    }
}
