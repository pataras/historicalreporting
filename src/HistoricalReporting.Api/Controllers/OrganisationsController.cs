using HistoricalReporting.Core.Entities;
using HistoricalReporting.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HistoricalReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganisationsController : ControllerBase
{
    private readonly IRepository<Organisation> _organisationRepository;
    private readonly ILogger<OrganisationsController> _logger;

    public OrganisationsController(
        IRepository<Organisation> organisationRepository,
        ILogger<OrganisationsController> logger)
    {
        _organisationRepository = organisationRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganisations()
    {
        _logger.LogInformation("Getting all organisations");
        var organisations = await _organisationRepository.GetAllAsync();
        return Ok(organisations.Select(o => new { o.Id, o.Name }));
    }
}
