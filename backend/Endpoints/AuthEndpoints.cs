using LealFinance.Api.Common;
using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Models.Auth;
using LealFinance.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace LealFinance.Api.Endpoints;

/// <summary>
/// Provides authentication endpoint mappings.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints.
    /// </summary>
    /// <param name="app">Route builder instance.</param>
    /// <returns>The same route builder.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var authRoutes = app.MapGroup("/api/auth");

        authRoutes.MapPost("/register", async (RegisterRequest request, LealFinanceDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var userAlreadyExists = await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
            if (userAlreadyExists)
            {
                return Results.Conflict(new { message = "A user with this e-mail already exists." });
            }

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = passwordHasher.HashPassword(request.Password),
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { message = "User registered successfully." });
        });

        authRoutes.MapPost("/login", async (LoginRequest request, LealFinanceDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Email == normalizedEmail, cancellationToken);
            if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var token = jwtTokenService.CreateToken(user);
            return Results.Ok(token);
        });

        return app;
    }
}
