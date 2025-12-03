using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports()
    {
        _logger.LogInformation("Getting all reports");
        var reports = await _reportService.GetAllReportsAsync();
        return Ok(reports);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        _logger.LogInformation("Getting report {ReportId}", id);
        var report = await _reportService.GetReportByIdAsync(id);

        if (report == null)
        {
            return NotFound();
        }

        return Ok(report);
    }
}
