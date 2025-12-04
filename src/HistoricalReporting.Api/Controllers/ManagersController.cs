using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManagersController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly ILogger<ManagersController> _logger;

    public ManagersController(
        IManagerService managerService,
        ILogger<ManagersController> logger)
    {
        _managerService = managerService;
        _logger = logger;
    }

    [HttpGet("organisation/{organisationId:guid}")]
    public async Task<IActionResult> GetManagersByOrganisation(Guid organisationId)
    {
        _logger.LogInformation("Getting managers for organisation {OrganisationId}", organisationId);

        var managers = await _managerService.GetManagersByOrganisationAsync(organisationId);

        return Ok(managers);
    }

    [HttpGet("{managerId:guid}")]
    public async Task<IActionResult> GetManager(Guid managerId)
    {
        _logger.LogInformation("Getting manager {ManagerId}", managerId);

        var manager = await _managerService.GetManagerByIdAsync(managerId);

        if (manager == null)
        {
            return NotFound($"Manager with ID {managerId} not found");
        }

        return Ok(manager);
    }
}
