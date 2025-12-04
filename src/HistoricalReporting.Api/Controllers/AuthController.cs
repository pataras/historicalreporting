using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICurrentUserContext currentUserContext,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        _logger.LogInformation("Successful login for user: {UserId}", result.User.Id);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            UserId = _currentUserContext.UserId,
            ManagerId = _currentUserContext.ManagerId,
            OrganisationId = _currentUserContext.OrganisationId,
            IsManager = _currentUserContext.IsManager
        });
    }
}
