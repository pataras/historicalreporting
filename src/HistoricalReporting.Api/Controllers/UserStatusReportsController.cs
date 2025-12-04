using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserStatusReportsController : ControllerBase
{
    private readonly IUserStatusReportService _userStatusReportService;
    private readonly ILogger<UserStatusReportsController> _logger;

    public UserStatusReportsController(
        IUserStatusReportService userStatusReportService,
        ILogger<UserStatusReportsController> logger)
    {
        _userStatusReportService = userStatusReportService;
        _logger = logger;
    }

    [HttpGet("monthly/{organisationId:guid}")]
    public async Task<IActionResult> GetMonthlyUserStatusReport(Guid organisationId)
    {
        _logger.LogInformation("Getting monthly user status report for organisation {OrganisationId}", organisationId);

        var report = await _userStatusReportService.GetMonthlyUserStatusReportAsync(organisationId);

        if (report == null)
        {
            return NotFound($"Organisation with ID {organisationId} not found");
        }

        return Ok(report);
    }
}
