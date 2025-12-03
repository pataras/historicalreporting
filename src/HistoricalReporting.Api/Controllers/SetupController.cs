using HistoricalReporting.Api.Hubs;
using HistoricalReporting.Core.Interfaces;
using HistoricalReporting.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly IDataSeedService _seedService;
    private readonly IHubContext<SeedProgressHub> _hubContext;
    private readonly ILogger<SetupController> _logger;

    private static bool _isSeeding;
    private static readonly object _seedLock = new();
    private static CancellationTokenSource? _seedCancellationTokenSource;

    public SetupController(
        IDataSeedService seedService,
        IHubContext<SeedProgressHub> hubContext,
        ILogger<SetupController> logger)
    {
        _seedService = seedService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedData([FromQuery] string? connectionId)
    {
        lock (_seedLock)
        {
            if (_isSeeding)
            {
                return Conflict(new { message = "Seeding is already in progress" });
            }
            _isSeeding = true;
            _seedCancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            _logger.LogInformation("Starting data seeding process");

            await _seedService.SeedDataAsync(
                async progress => await SendProgressUpdate(progress, connectionId),
                _seedCancellationTokenSource.Token);

            _logger.LogInformation("Data seeding completed successfully");
            return Ok(new { message = "Seeding completed successfully" });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Data seeding was cancelled");
            return Ok(new { message = "Seeding was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            return StatusCode(500, new { message = "An error occurred during seeding", error = ex.Message });
        }
        finally
        {
            lock (_seedLock)
            {
                _isSeeding = false;
                _seedCancellationTokenSource?.Dispose();
                _seedCancellationTokenSource = null;
            }
        }
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearData([FromQuery] string? connectionId)
    {
        lock (_seedLock)
        {
            if (_isSeeding)
            {
                return Conflict(new { message = "Cannot clear while seeding is in progress" });
            }
            _isSeeding = true;
        }

        try
        {
            _logger.LogInformation("Starting data clear process");

            await _seedService.ClearDataAsync(
                async progress => await SendProgressUpdate(progress, connectionId),
                CancellationToken.None);

            _logger.LogInformation("Data clear completed successfully");
            return Ok(new { message = "Data cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data clear");
            return StatusCode(500, new { message = "An error occurred during clearing", error = ex.Message });
        }
        finally
        {
            lock (_seedLock)
            {
                _isSeeding = false;
            }
        }
    }

    [HttpPost("cancel")]
    public IActionResult CancelSeeding()
    {
        lock (_seedLock)
        {
            if (!_isSeeding)
            {
                return BadRequest(new { message = "No seeding operation in progress" });
            }

            _seedCancellationTokenSource?.Cancel();
            return Ok(new { message = "Cancellation requested" });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { isSeeding = _isSeeding });
    }

    private async Task SendProgressUpdate(SeedProgress progress, string? connectionId)
    {
        if (!string.IsNullOrEmpty(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("SeedProgress", progress);
        }
        else
        {
            await _hubContext.Clients.All.SendAsync("SeedProgress", progress);
        }
    }
}
