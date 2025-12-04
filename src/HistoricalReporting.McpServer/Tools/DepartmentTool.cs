using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace HistoricalReporting.McpServer.Tools;

[McpServerToolType]
public class DepartmentTool
{
    private readonly ApplicationDbContext _dbContext;

    public DepartmentTool(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [McpServerTool("get_departments")]
    [Description("Returns all departments with their names, aliases, and organisation associations. Use this to map common abbreviations (like 'IT', 'HR', 'Finance') to their full department names in the database.")]
    public async Task<string> GetDepartmentsAsync(
        [Description("Optional organisation ID to filter departments")] Guid? organisationId = null)
    {
        var query = _dbContext.Departments
            .Include(d => d.Organisation)
            .AsNoTracking();

        if (organisationId.HasValue)
        {
            query = query.Where(d => d.OrganisationId == organisationId.Value);
        }

        var departments = await query
            .Select(d => new DepartmentInfo
            {
                Id = d.Id,
                Name = d.Name,
                Aliases = GetDepartmentAliases(d.Name),
                OrganisationId = d.OrganisationId,
                OrganisationName = d.Organisation != null ? d.Organisation.Name : null,
                ParentDepartmentId = d.ParentDepartmentId
            })
            .OrderBy(d => d.OrganisationName)
            .ThenBy(d => d.Name)
            .ToListAsync();

        var result = new DepartmentListResult
        {
            Departments = departments,
            TotalCount = departments.Count,
            Notes = new List<string>
            {
                "Use the Id field when constructing WHERE clauses for DepartmentId filters.",
                "Aliases are common abbreviations that users might use when referring to departments.",
                "Match user input against both Name and Aliases (case-insensitive) to find the correct department."
            }
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool("search_department")]
    [Description("Searches for a department by name or alias. Returns matching departments with their IDs. Use this when the user mentions a department by name or abbreviation.")]
    public async Task<string> SearchDepartmentAsync(
        [Description("The search term (department name or alias like 'IT', 'HR', 'Sales')")] string searchTerm,
        [Description("Optional organisation ID to filter search")] Guid? organisationId = null)
    {
        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

        var query = _dbContext.Departments
            .Include(d => d.Organisation)
            .AsNoTracking();

        if (organisationId.HasValue)
        {
            query = query.Where(d => d.OrganisationId == organisationId.Value);
        }

        var allDepartments = await query.ToListAsync();

        var matches = allDepartments
            .Select(d => new
            {
                Department = d,
                Aliases = GetDepartmentAliases(d.Name),
                NameMatch = d.Name.ToLowerInvariant().Contains(normalizedSearch),
                ExactNameMatch = d.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)
            })
            .Select(x => new
            {
                x.Department,
                x.Aliases,
                x.NameMatch,
                x.ExactNameMatch,
                AliasMatch = x.Aliases.Any(a => a.ToLowerInvariant().Contains(normalizedSearch)),
                ExactAliasMatch = x.Aliases.Any(a => a.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            })
            .Where(x => x.NameMatch || x.AliasMatch)
            .OrderByDescending(x => x.ExactNameMatch || x.ExactAliasMatch)
            .ThenByDescending(x => x.NameMatch)
            .Select(x => new DepartmentSearchResult
            {
                Id = x.Department.Id,
                Name = x.Department.Name,
                Aliases = x.Aliases,
                OrganisationId = x.Department.OrganisationId,
                OrganisationName = x.Department.Organisation?.Name,
                MatchType = x.ExactNameMatch ? "ExactName" :
                           x.ExactAliasMatch ? "ExactAlias" :
                           x.NameMatch ? "PartialName" : "PartialAlias"
            })
            .ToList();

        var result = new DepartmentSearchResponse
        {
            SearchTerm = searchTerm,
            Matches = matches,
            TotalMatches = matches.Count,
            Suggestion = matches.Count == 0
                ? $"No departments found matching '{searchTerm}'. Try a different term or use get_departments to see all available departments."
                : matches.Count == 1
                    ? $"Found exact match: {matches[0].Name} (ID: {matches[0].Id})"
                    : $"Found {matches.Count} matches. Use the most relevant department ID in your query."
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static List<string> GetDepartmentAliases(string departmentName)
    {
        var aliases = new List<string>();
        var normalizedName = departmentName.ToLowerInvariant();

        // Common department name mappings
        var aliasMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Information Technology", new List<string> { "IT", "Tech", "Technology", "InfoTech", "IS", "Information Systems" } },
            { "Human Resources", new List<string> { "HR", "People", "People Operations", "Personnel", "Talent" } },
            { "Finance", new List<string> { "Fin", "Accounting", "Accounts", "Financial Services" } },
            { "Sales", new List<string> { "Sales Team", "Revenue", "Business Development", "BD" } },
            { "Marketing", new List<string> { "Mktg", "Brand", "Growth", "Digital Marketing" } },
            { "Operations", new List<string> { "Ops", "Operations Team" } },
            { "Engineering", new List<string> { "Eng", "Development", "Dev", "R&D", "Research and Development" } },
            { "Customer Service", new List<string> { "CS", "Support", "Customer Support", "Help Desk", "Client Services" } },
            { "Legal", new List<string> { "Legal Team", "Compliance", "Legal & Compliance" } },
            { "Administration", new List<string> { "Admin", "Administrative" } },
            { "Research", new List<string> { "R&D", "Research & Development" } },
            { "Quality Assurance", new List<string> { "QA", "Quality", "Testing" } },
            { "Product", new List<string> { "Product Management", "PM", "Product Team" } },
            { "Design", new List<string> { "UX", "UI", "UX/UI", "Creative", "Graphics" } },
            { "Data", new List<string> { "Data Team", "Analytics", "BI", "Business Intelligence", "Data Science" } },
            { "Security", new List<string> { "InfoSec", "Cybersecurity", "IT Security" } },
            { "Facilities", new List<string> { "Building Services", "Maintenance", "Office Management" } },
            { "Procurement", new List<string> { "Purchasing", "Supply Chain", "Vendor Management" } },
            { "Training", new List<string> { "L&D", "Learning", "Learning & Development", "Education" } },
            { "Communications", new List<string> { "Comms", "PR", "Public Relations", "Corporate Communications" } }
        };

        // Check for exact match in alias map
        if (aliasMap.TryGetValue(departmentName, out var exactAliases))
        {
            aliases.AddRange(exactAliases);
        }
        else
        {
            // Check if department name contains any key phrases
            foreach (var kvp in aliasMap)
            {
                if (normalizedName.Contains(kvp.Key.ToLowerInvariant()) ||
                    kvp.Key.ToLowerInvariant().Contains(normalizedName))
                {
                    aliases.AddRange(kvp.Value);
                    break;
                }
            }
        }

        // Generate acronym from department name
        var words = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            var acronym = string.Join("", words.Select(w => w[0])).ToUpperInvariant();
            if (!aliases.Contains(acronym) && acronym.Length >= 2)
            {
                aliases.Add(acronym);
            }
        }

        return aliases.Distinct().ToList();
    }
}

public class DepartmentInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public Guid OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
    public Guid? ParentDepartmentId { get; set; }
}

public class DepartmentListResult
{
    public List<DepartmentInfo> Departments { get; set; } = new();
    public int TotalCount { get; set; }
    public List<string> Notes { get; set; } = new();
}

public class DepartmentSearchResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public Guid OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
    public string MatchType { get; set; } = string.Empty;
}

public class DepartmentSearchResponse
{
    public string SearchTerm { get; set; } = string.Empty;
    public List<DepartmentSearchResult> Matches { get; set; } = new();
    public int TotalMatches { get; set; }
    public string Suggestion { get; set; } = string.Empty;
}
