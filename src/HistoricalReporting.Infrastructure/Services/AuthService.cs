using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HistoricalReporting.Infrastructure.Services;

/// <summary>
/// Authentication service for user login and JWT token generation
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Manager)
                .ThenInclude(m => m!.Organisation)
            .Include(u => u.Manager)
                .ThenInclude(m => m!.ManagedDepartments)
                    .ThenInclude(md => md.Department)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60);

        return new AuthResult
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ManagerId = user.ManagerId,
                OrganisationId = user.Manager?.OrganisationId,
                OrganisationName = user.Manager?.Organisation?.Name,
                ManagesAllDepartments = user.Manager?.ManagesAllDepartments ?? false,
                ManagedDepartments = user.Manager?.ManagedDepartments
                    .Where(md => md.Department != null)
                    .Select(md => new ManagedDepartmentInfo
                    {
                        Id = md.DepartmentId,
                        Name = md.Department!.Name
                    })
                    .ToList() ?? []
            }
        };
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        // Simple password verification using PBKDF2
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private string GenerateJwtToken(Core.Entities.User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "HistoricalReporting.Api";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "HistoricalReporting.Client";
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        // Add manager-specific claims if the user is a manager
        if (user.ManagerId.HasValue && user.Manager != null)
        {
            claims.Add(new Claim("manager_id", user.ManagerId.Value.ToString()));
            claims.Add(new Claim("organisation_id", user.Manager.OrganisationId.ToString()));
            claims.Add(new Claim("manages_all_departments", user.Manager.ManagesAllDepartments.ToString().ToLower()));

            // Add managed department IDs as claims
            foreach (var dept in user.Manager.ManagedDepartments)
            {
                claims.Add(new Claim("managed_department", dept.DepartmentId.ToString()));
            }
        }

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
