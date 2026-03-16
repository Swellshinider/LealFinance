using LealFinance.Api.Entities;
using LealFinance.Api.Models.Auth;

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
}
