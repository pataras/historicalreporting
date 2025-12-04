using HistoricalReporting.Core.Models;

namespace HistoricalReporting.Core.Interfaces;

/// <summary>
/// Authentication service for user login and token generation
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    Task<AuthResult?> LoginAsync(string email, string password);

    /// <summary>
    /// Validates a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Hashes a password for storage
    /// </summary>
    string HashPassword(string password);
}

