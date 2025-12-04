using System.ComponentModel.DataAnnotations;

namespace HistoricalReporting.Core.Models;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
