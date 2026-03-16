using LealFinance.Api.Entities;
using LealFinance.Api.Models.Auth;
using System.Security.Claims;

namespace LealFinance.Api.Security;

/// <summary>
/// Defines JWT creation behavior.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates an authentication response for a user.
    /// </summary>
    /// <param name="user">Authenticated user.</param>
    /// <returns>Signed token payload.</returns>
    AuthResponse CreateToken(User user);

    /// <summary>
    /// Gets a principal from an expired JWT while still validating signature and issuer settings.
    /// </summary>
    /// <param name="token">Expired JWT value.</param>
    /// <returns>Validated claims principal.</returns>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
