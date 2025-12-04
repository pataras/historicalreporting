using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace HistoricalReporting.McpServer.Tools;

[McpServerToolType]
public class OrganisationTool
{
    private readonly ApplicationDbContext _dbContext;

    public OrganisationTool(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [McpServerTool("get_organisations")]
    [Description("Returns all organisations in the system with their IDs and names. Use this to identify which organisations exist and to get the correct organisation ID for queries.")]
    public async Task<string> GetOrganisationsAsync()
    {
        var organisations = await _dbContext.Organisations
            .AsNoTracking()
            .Select(o => new OrganisationInfo
            {
                Id = o.Id,
                Name = o.Name,
                DepartmentCount = o.Departments.Count,
                UserCount = o.Users.Count,
                ManagerCount = o.Managers.Count
            })
            .OrderBy(o => o.Name)
            .ToListAsync();

        var result = new OrganisationListResult
        {
            Organisations = organisations,
            TotalCount = organisations.Count,
            Notes = new List<string>
            {
                "Use the Id field when constructing WHERE clauses for OrganisationId filters.",
                "Each organisation has its own set of departments, users, and managers.",
                "Managers can only access data within their assigned organisation."
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool("search_organisation")]
    [Description("Searches for an organisation by name. Returns matching organisations with their IDs.")]
    public async Task<string> SearchOrganisationAsync(
        [Description("The search term (organisation name or partial name)")] string searchTerm)
    {
        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var organisations = await _dbContext.Organisations
            .AsNoTracking()
            .Where(o => o.Name.ToLower().Contains(normalizedSearch))
            .Select(o => new OrganisationSearchResult
            {
                Id = o.Id,
                Name = o.Name,
                DepartmentCount = o.Departments.Count,
                UserCount = o.Users.Count,
                MatchType = o.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? "Exact" : "Partial"
            })
            .OrderByDescending(o => o.MatchType == "Exact")
            .ThenBy(o => o.Name)
            .ToListAsync();

        var result = new OrganisationSearchResponse
        {
            SearchTerm = searchTerm,
            Matches = organisations,
            TotalMatches = organisations.Count,
            Suggestion = organisations.Count == 0
                ? $"No organisations found matching '{searchTerm}'. Use get_organisations to see all available organisations."
                : organisations.Count == 1
                    ? $"Found match: {organisations[0].Name} (ID: {organisations[0].Id})"
                    : $"Found {organisations.Count} matches. Use the most relevant organisation ID in your query."
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool("get_organisation_details")]
    [Description("Returns detailed information about a specific organisation, including its departments and statistics.")]
    public async Task<string> GetOrganisationDetailsAsync(
        [Description("The organisation ID")] Guid organisationId)
    {
        var organisation = await _dbContext.Organisations
            .AsNoTracking()
            .Where(o => o.Id == organisationId)
            .Select(o => new OrganisationDetails
            {
                Id = o.Id,
                Name = o.Name,
                Departments = o.Departments.Select(d => new DepartmentSummary
                {
                    Id = d.Id,
                    Name = d.Name,
                    UserCount = d.Users.Count
                }).OrderBy(d => d.Name).ToList(),
                TotalDepartments = o.Departments.Count,
                TotalUsers = o.Users.Count,
                TotalManagers = o.Managers.Count
            })
            .FirstOrDefaultAsync();

        if (organisation == null)
        {
            return JsonSerializer.Serialize(new { Error = $"Organisation with ID {organisationId} not found." });
        }

        return JsonSerializer.Serialize(organisation, new JsonSerializerOptions { WriteIndented = true });
    }
}

public class OrganisationInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DepartmentCount { get; set; }
    public int UserCount { get; set; }
    public int ManagerCount { get; set; }
}

public class OrganisationListResult
{
    public List<OrganisationInfo> Organisations { get; set; } = new();
    public int TotalCount { get; set; }
    public List<string> Notes { get; set; } = new();
}

public class OrganisationSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DepartmentCount { get; set; }
    public int UserCount { get; set; }
    public string MatchType { get; set; } = string.Empty;
}

public class OrganisationSearchResponse
{
    public string SearchTerm { get; set; } = string.Empty;
    public List<OrganisationSearchResult> Matches { get; set; } = new();
    public int TotalMatches { get; set; }
    public string Suggestion { get; set; } = string.Empty;
}

public class OrganisationDetails
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<DepartmentSummary> Departments { get; set; } = new();
    public int TotalDepartments { get; set; }
    public int TotalUsers { get; set; }
    public int TotalManagers { get; set; }
}

public class DepartmentSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
