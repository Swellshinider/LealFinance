using LealFinance.Api.Common;
using LealFinance.Api.Data;
using LealFinance.Api.Entities;
using LealFinance.Api.Models.Auth;
using LealFinance.Api.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        var authRoutes = app.MapGroup("/api/auth").RequireRateLimiting("AuthFixed");
        var authProtectedRoutes = authRoutes.MapGroup(string.Empty).RequireAuthorization();

        authRoutes.MapPost("/register", async (RegisterRequest request, LealFinanceDbContext dbContext, IPasswordHasher passwordHasher, IDataEncryptionService dataEncryptionService, ITwoFactorService twoFactorService, CancellationToken cancellationToken) =>
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

            var passwordHash = passwordHasher.HashPassword(request.Password);
            var dataEncryptionSalt = dataEncryptionService.GenerateUserSalt();
            var twoFactorSecret = twoFactorService.GenerateSharedSecret();

            var user = new User
            {
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),
                PasswordHash = passwordHash,
                MasterPasswordHash = passwordHash,
                DataEncryptionSalt = dataEncryptionSalt,
                TwoFactorSecret = dataEncryptionService.Encrypt(twoFactorSecret, passwordHash, dataEncryptionSalt),
                IsTwoFactorEnabled = false,
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
            response.RequiresTwoFactorSetup = !user.IsTwoFactorEnabled;

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
                newAuthResponse.RequiresTwoFactorSetup = !user.IsTwoFactorEnabled;
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

        authProtectedRoutes.MapGet("/profile", async (HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            return Results.Ok(new ProfileResponse
            {
                FullName = user.FullName,
                Email = user.Email,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            });
        });

        authProtectedRoutes.MapPut("/profile", async (UpdateProfileRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, CancellationToken cancellationToken) =>
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

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var emailInUse = await dbContext.Users.AnyAsync(
                entity => entity.Email == normalizedEmail && entity.Id != userId,
                cancellationToken);

            if (emailInUse)
            {
                return Results.Conflict(new { message = "A user with this e-mail already exists." });
            }

            user.FullName = request.FullName.Trim();
            user.Email = normalizedEmail;
            user.ProfilePhotoUrl = string.IsNullOrWhiteSpace(request.ProfilePhotoUrl)
                ? null
                : request.ProfilePhotoUrl.Trim();

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new ProfileResponse
            {
                FullName = user.FullName,
                Email = user.Email,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            });
        });

        authProtectedRoutes.MapGet("/2fa/setup", async (HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, ITwoFactorService twoFactorService, CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound(new { message = "User profile not found." });
            }

            var sharedSecret = ResolveOrCreateTwoFactorSecret(user, dataEncryptionService, twoFactorService);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new TwoFactorSetupResponse
            {
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                ManualEntryKey = sharedSecret,
                OtpAuthUri = twoFactorService.BuildOtpAuthUri("LealFinance", user.Email, sharedSecret)
            });
        });

        authProtectedRoutes.MapPost("/2fa/enable", async (EnableTwoFactorRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IDataEncryptionService dataEncryptionService, ITwoFactorService twoFactorService, CancellationToken cancellationToken) =>
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

            var sharedSecret = ResolveOrCreateTwoFactorSecret(user, dataEncryptionService, twoFactorService);
            if (!twoFactorService.VerifyCode(sharedSecret, request.VerificationCode))
            {
                return Results.BadRequest(new { message = "The verification code is invalid or expired." });
            }

            user.IsTwoFactorEnabled = true;
            user.TwoFactorEnabledAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { message = "Authenticator app linked successfully." });
        });

        authProtectedRoutes.MapPost("/profile/recover-password", async (RecoverPasswordRequest request, HttpContext httpContext, LealFinanceDbContext dbContext, IPasswordHasher passwordHasher, IDataEncryptionService dataEncryptionService, ITwoFactorService twoFactorService, CancellationToken cancellationToken) =>
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
                return Results.BadRequest(new { message = "Enable authenticator-based 2FA before recovering your password." });
            }

            if (passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                return Results.BadRequest(new { message = "New password must be different from your current password." });
            }

            var sharedSecret = ResolveOrCreateTwoFactorSecret(user, dataEncryptionService, twoFactorService);
            if (!twoFactorService.VerifyCode(sharedSecret, request.VerificationCode))
            {
                return Results.BadRequest(new { message = "The verification code is invalid or expired." });
            }

            user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { message = "Password recovered successfully. Please use the new password in your next login." });
        });

        return app;
    }

    private static string ResolveOrCreateTwoFactorSecret(User user, IDataEncryptionService dataEncryptionService, ITwoFactorService twoFactorService)
    {
        var userSalt = GetOrCreateDataEncryptionSalt(user, dataEncryptionService);

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            var generated = twoFactorService.GenerateSharedSecret();
            user.TwoFactorSecret = dataEncryptionService.Encrypt(generated, user.MasterPasswordHash, userSalt);
            return generated;
        }

        return dataEncryptionService.Decrypt(user.TwoFactorSecret, user.MasterPasswordHash, userSalt);
    }

    private static string GetOrCreateDataEncryptionSalt(User user, IDataEncryptionService dataEncryptionService)
    {
        if (!string.IsNullOrWhiteSpace(user.DataEncryptionSalt))
        {
            try
            {
                _ = Convert.FromBase64String(user.DataEncryptionSalt);
                return user.DataEncryptionSalt;
            }
            catch
            {
                user.DataEncryptionSalt = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.DataEncryptionSalt));
                return user.DataEncryptionSalt;
            }
        }

        user.DataEncryptionSalt = dataEncryptionService.GenerateUserSalt();
        return user.DataEncryptionSalt;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId)
    {
        var userIdValue = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out userId);
    }

    private static string GenerateSecureRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
