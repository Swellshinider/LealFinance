using LealFinance.Api.Common;
using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Models.Auth;
using LealFinance.Api.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LealFinance.Api.Endpoints;

/// <summary>
/// Provides authentication endpoint mappings.
/// </summary>
public static class AuthEndpoints
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

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

            var response = jwtTokenService.CreateToken(user);

            if (request.RememberMe)
            {
                var refreshToken = GenerateSecureRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.Add(RefreshTokenLifetime);

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = refreshTokenExpiry;
                await dbContext.SaveChangesAsync(cancellationToken);

                response.RefreshToken = refreshToken;
                response.RefreshTokenExpiresAtUtc = refreshTokenExpiry;
            }
            else
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(response);
        });

        authRoutes.MapPost("/refresh", async (RefreshTokenRequest request, LealFinanceDbContext dbContext, IJwtTokenService jwtTokenService, CancellationToken cancellationToken) =>
        {
            if (!request.TryValidate(out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(request.Token);
            }
            catch
            {
                return Results.Unauthorized();
            }

            if (jwtToken.ValidTo >= DateTime.UtcNow)
            {
                return Results.BadRequest(new { message = "Access token is still valid." });
            }

            try
            {
                var principal = jwtTokenService.GetPrincipalFromExpiredToken(request.Token);
                var userIdValue = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!int.TryParse(userIdValue, out var userId))
                {
                    return Results.Unauthorized();
                }

                var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
                if (user is null ||
                    string.IsNullOrWhiteSpace(user.RefreshToken) ||
                    user.RefreshTokenExpiryTime is null ||
                    user.RefreshTokenExpiryTime <= DateTime.UtcNow ||
                    !CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(user.RefreshToken),
                        System.Text.Encoding.UTF8.GetBytes(request.RefreshToken)))
                {
                    return Results.Unauthorized();
                }

                var newAuthResponse = jwtTokenService.CreateToken(user);
                var newRefreshToken = GenerateSecureRefreshToken();
                var newRefreshTokenExpiry = DateTime.UtcNow.Add(RefreshTokenLifetime);

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = newRefreshTokenExpiry;
                await dbContext.SaveChangesAsync(cancellationToken);

                newAuthResponse.RefreshToken = newRefreshToken;
                newAuthResponse.RefreshTokenExpiresAtUtc = newRefreshTokenExpiry;

                return Results.Ok(newAuthResponse);
            }
            catch (SecurityTokenException)
            {
                return Results.Unauthorized();
            }
        });

        return app;
    }

    private static string GenerateSecureRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
