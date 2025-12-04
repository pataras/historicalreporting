using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace HistoricalReporting.AI.Plugins;

public class EntityLookupPlugin
{
    private readonly ApplicationDbContext _dbContext;

    public EntityLookupPlugin(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction("get_organisations")]
    [Description("Returns all organisations with their IDs and names. Use to get organisation IDs for queries.")]
    public async Task<string> GetOrganisationsAsync()
    {
        var organisations = await _dbContext.Organisations
            .AsNoTracking()
            .Select(o => new
            {
                o.Id,
                o.Name,
                DepartmentCount = o.Departments.Count,
                UserCount = o.Users.Count
            })
            .OrderBy(o => o.Name)
            .ToListAsync();

        return JsonSerializer.Serialize(new
        {
            Organisations = organisations,
            TotalCount = organisations.Count
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("search_organisation")]
    [Description("Searches for an organisation by name. Returns matching organisations with IDs.")]
    public async Task<string> SearchOrganisationAsync(
        [Description("The search term (organisation name or partial name)")] string searchTerm)
    {
        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var matches = await _dbContext.Organisations
            .AsNoTracking()
            .Where(o => o.Name.ToLower().Contains(normalizedSearch))
            .Select(o => new { o.Id, o.Name })
            .ToListAsync();

        return JsonSerializer.Serialize(new
        {
            SearchTerm = searchTerm,
            Matches = matches,
            Suggestion = matches.Count == 1
                ? $"Use OrganisationId = '{matches[0].Id}' in your query"
                : matches.Count == 0
                    ? "No matches found. Use get_organisations to see all."
                    : "Multiple matches found. Please be more specific."
        });
    }

    [KernelFunction("get_departments")]
    [Description("Returns all departments with names and IDs. Use to map department names to IDs for queries.")]
    public async Task<string> GetDepartmentsAsync(
        [Description("Optional organisation ID to filter departments")] string? organisationId = null)
    {
        var query = _dbContext.Departments
            .Include(d => d.Organisation)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(organisationId) && Guid.TryParse(organisationId, out var orgId))
        {
            query = query.Where(d => d.OrganisationId == orgId);
        }

        var departments = await query
            .Select(d => new
            {
                d.Id,
                d.Name,
                Aliases = GetDepartmentAliases(d.Name),
                d.OrganisationId,
                OrganisationName = d.Organisation != null ? d.Organisation.Name : null
            })
            .OrderBy(d => d.OrganisationName)
            .ThenBy(d => d.Name)
            .ToListAsync();

        return JsonSerializer.Serialize(new
        {
            Departments = departments,
            TotalCount = departments.Count,
            Note = "Use the Id field for DepartmentId in WHERE clauses. Aliases are common abbreviations."
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("search_department")]
    [Description("Searches for a department by name or alias (like 'IT', 'HR', 'Finance'). Returns matching department IDs.")]
    public async Task<string> SearchDepartmentAsync(
        [Description("The search term (department name or alias like 'IT', 'HR')")] string searchTerm,
        [Description("Optional organisation ID to filter search")] string? organisationId = null)
    {
        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var query = _dbContext.Departments
            .Include(d => d.Organisation)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(organisationId) && Guid.TryParse(organisationId, out var orgId))
        {
            query = query.Where(d => d.OrganisationId == orgId);
        }

        var allDepartments = await query.ToListAsync();

        var matches = allDepartments
            .Select(d => new
            {
                d.Id,
                d.Name,
                Aliases = GetDepartmentAliases(d.Name),
                d.OrganisationId,
                OrganisationName = d.Organisation?.Name
            })
            .Where(d =>
                d.Name.ToLowerInvariant().Contains(normalizedSearch) ||
                d.Aliases.Any(a => a.ToLowerInvariant().Contains(normalizedSearch)))
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Aliases,
                d.OrganisationId,
                d.OrganisationName,
                IsExactMatch = d.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                              d.Aliases.Any(a => a.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(d => d.IsExactMatch)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            SearchTerm = searchTerm,
            Matches = matches,
            Suggestion = matches.Count == 1
                ? $"Use DepartmentId = '{matches[0].Id}' for '{matches[0].Name}'"
                : matches.Count == 0
                    ? $"No department matching '{searchTerm}'. Use get_departments to see all."
                    : $"Found {matches.Count} matches. Use the most relevant DepartmentId."
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private static List<string> GetDepartmentAliases(string departmentName)
    {
        var aliases = new List<string>();

        var aliasMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Information Technology", new List<string> { "IT", "Tech", "Technology", "IS" } },
            { "Human Resources", new List<string> { "HR", "People", "Personnel", "Talent" } },
            { "Finance", new List<string> { "Fin", "Accounting", "Accounts" } },
            { "Sales", new List<string> { "Sales Team", "Revenue", "BD", "Business Development" } },
            { "Marketing", new List<string> { "Mktg", "Brand", "Growth" } },
            { "Operations", new List<string> { "Ops" } },
            { "Engineering", new List<string> { "Eng", "Dev", "Development", "R&D" } },
            { "Customer Service", new List<string> { "CS", "Support", "Help Desk" } },
            { "Legal", new List<string> { "Legal Team", "Compliance" } },
            { "Administration", new List<string> { "Admin" } },
            { "Quality Assurance", new List<string> { "QA", "Quality", "Testing" } },
            { "Product", new List<string> { "PM", "Product Management" } },
            { "Design", new List<string> { "UX", "UI", "Creative" } },
            { "Data", new List<string> { "Analytics", "BI", "Data Science" } },
            { "Security", new List<string> { "InfoSec", "Cybersecurity" } }
        };

        if (aliasMap.TryGetValue(departmentName, out var exactAliases))
        {
            aliases.AddRange(exactAliases);
        }
        else
        {
            foreach (var kvp in aliasMap)
            {
                if (departmentName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains(departmentName, StringComparison.OrdinalIgnoreCase))
                {
                    aliases.AddRange(kvp.Value);
                    break;
                }
            }
        }

        // Generate acronym
        var words = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            var acronym = string.Join("", words.Select(w => char.ToUpperInvariant(w[0])));
            if (!aliases.Contains(acronym) && acronym.Length >= 2)
            {
                aliases.Add(acronym);
            }
        }

        return aliases.Distinct().ToList();
    }
}
