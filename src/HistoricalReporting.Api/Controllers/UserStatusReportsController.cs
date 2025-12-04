using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserStatusReportsController : ControllerBase
{
    private readonly IUserStatusReportService _userStatusReportService;
    private readonly IRowLevelSecurityService _rlsService;
    private readonly ILogger<UserStatusReportsController> _logger;

    public UserStatusReportsController(
        IUserStatusReportService userStatusReportService,
        IRowLevelSecurityService rlsService,
        ILogger<UserStatusReportsController> logger)
    {
        _userStatusReportService = userStatusReportService;
        _rlsService = rlsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the monthly user status report for the authenticated manager.
    /// Row-level security is automatically applied based on the manager's department access.
    /// </summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyUserStatusReport()
    {
        var organisationId = _rlsService.GetCurrentOrganisationId();
        var managerId = _rlsService.GetCurrentManagerId();

        if (!organisationId.HasValue || !managerId.HasValue)
        {
            return Forbid("User is not associated with a manager role");
        }

        _logger.LogInformation(
            "Getting monthly user status report for manager {ManagerId} in organisation {OrganisationId}",
            managerId.Value, organisationId.Value);

        // Always use the authenticated user's manager ID - RLS enforced automatically
        var report = await _userStatusReportService.GetMonthlyUserStatusReportByManagerAsync(
            organisationId.Value, managerId.Value);

        if (report == null)
        {
            return NotFound("Report data not found");
        }

        return Ok(report);
    }

    /// <summary>
    /// Gets the monthly user status report for a specific organisation.
    /// Only returns data the authenticated manager has access to.
    /// </summary>
    [HttpGet("monthly/{organisationId:guid}")]
    public async Task<IActionResult> GetMonthlyUserStatusReportByOrganisation(Guid organisationId)
    {
        // Validate the user can access this organisation
        if (!await _rlsService.CanAccessOrganisationAsync(organisationId))
        {
            _logger.LogWarning(
                "Access denied: User attempted to access organisation {OrganisationId}", organisationId);
            return Forbid("You do not have access to this organisation");
        }

        var managerId = _rlsService.GetCurrentManagerId();
        if (!managerId.HasValue)
        {
            return Forbid("User is not associated with a manager role");
        }

        _logger.LogInformation(
            "Getting monthly user status report for organisation {OrganisationId} with RLS for manager {ManagerId}",
            organisationId, managerId.Value);

        // Always apply RLS by using the manager-filtered query
        var report = await _userStatusReportService.GetMonthlyUserStatusReportByManagerAsync(
            organisationId, managerId.Value);

        if (report == null)
        {
            return NotFound($"Organisation with ID {organisationId} not found");
        }

        return Ok(report);
    }

    /// <summary>
    /// Gets the monthly user status report filtered by manager.
    /// The requesting user can only view their own data (managerId must match authenticated user).
    /// </summary>
    [HttpGet("monthly/{organisationId:guid}/manager/{managerId:guid}")]
    public async Task<IActionResult> GetMonthlyUserStatusReportByManager(Guid organisationId, Guid managerId)
    {
        // Validate the user can access this organisation
        if (!await _rlsService.CanAccessOrganisationAsync(organisationId))
        {
            _logger.LogWarning(
                "Access denied: User attempted to access organisation {OrganisationId}", organisationId);
            return Forbid("You do not have access to this organisation");
        }

        // RLS: Users can only access their own manager data
        var currentManagerId = _rlsService.GetCurrentManagerId();
        if (!currentManagerId.HasValue || currentManagerId.Value != managerId)
        {
            _logger.LogWarning(
                "Access denied: Manager {CurrentManagerId} attempted to access data for manager {RequestedManagerId}",
                currentManagerId, managerId);
            return Forbid("You can only access your own department data");
        }

        _logger.LogInformation(
            "Getting monthly user status report for organisation {OrganisationId} and manager {ManagerId}",
            organisationId, managerId);

        var report = await _userStatusReportService.GetMonthlyUserStatusReportByManagerAsync(organisationId, managerId);

        if (report == null)
        {
            return NotFound($"Organisation with ID {organisationId} or Manager with ID {managerId} not found");
        }

        return Ok(report);
    }
}
