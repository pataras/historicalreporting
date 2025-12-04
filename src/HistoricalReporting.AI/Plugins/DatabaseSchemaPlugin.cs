using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace HistoricalReporting.AI.Plugins;

public class DatabaseSchemaPlugin
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseSchemaPlugin(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [KernelFunction("get_database_schema")]
    [Description("Returns the complete database schema including tables, columns, data types, and relationships. Use this to understand the database structure before generating SQL queries.")]
    public string GetDatabaseSchema()
    {
        var schema = new
        {
            Tables = GetTableDefinitions(),
            Relationships = GetRelationships(),
            ImportantNotes = GetImportantNotes()
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

    [KernelFunction("get_status_values")]
    [Description("Returns all possible status values used in AuditRecords table.")]
    public async Task<string> GetStatusValuesAsync()
    {
        var statuses = await _dbContext.AuditRecords
            .AsNoTracking()
            .Select(a => a.Status)
            .Distinct()
            .ToListAsync();

        return JsonSerializer.Serialize(new
        {
            StatusValues = statuses,
            Notes = new[]
            {
                "Use these exact values in WHERE clauses (case-sensitive).",
                "Status values are: 'Valid' (compliant/passed) and 'Invalid' (non-compliant/failed)."
            }
        });
    }

    [KernelFunction("get_date_range")]
    [Description("Returns the minimum and maximum dates in the AuditRecords table.")]
    public async Task<string> GetDateRangeAsync()
    {
        var hasRecords = await _dbContext.AuditRecords.AnyAsync();
        if (!hasRecords)
        {
            return JsonSerializer.Serialize(new { Error = "No audit records found in database." });
        }

        var minDate = await _dbContext.AuditRecords.MinAsync(a => a.Date);
        var maxDate = await _dbContext.AuditRecords.MaxAsync(a => a.Date);

        return JsonSerializer.Serialize(new
        {
            MinDate = minDate,
            MaxDate = maxDate,
            MinDateFormatted = FormatDate(minDate),
            MaxDateFormatted = FormatDate(maxDate),
            Notes = new[]
            {
                "Dates are stored as integers in yyyyMMdd format.",
                "To filter by year: WHERE Date / 10000 = 2024",
                "To filter by month: WHERE (Date / 100) % 100 = 1 (for January)"
            }
        });
    }

    private static string FormatDate(int dateInt)
    {
        var year = dateInt / 10000;
        var month = (dateInt / 100) % 100;
        var day = dateInt % 100;
        return $"{year:D4}-{month:D2}-{day:D2}";
    }

    private static List<object> GetTableDefinitions()
    {
        return new List<object>
        {
            new
            {
                Name = "Organisations",
                Description = "Contains organisation/company information",
                Columns = new[]
                {
                    new { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new { Name = "Name", DataType = "nvarchar(10)", IsPrimaryKey = false, Description = "Organisation name/code" },
                    new { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Last update timestamp (nullable)" }
                }
            },
            new
            {
                Name = "Departments",
                Description = "Contains department information within organisations",
                Columns = new[]
                {
                    new { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new { Name = "Name", DataType = "nvarchar(100)", IsPrimaryKey = false, Description = "Department name" },
                    new { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations" },
                    new { Name = "ParentDepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Self-reference for hierarchy (nullable)" },
                    new { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Last update timestamp (nullable)" }
                }
            },
            new
            {
                Name = "OrganisationUsers",
                Description = "Contains users/employees within organisations",
                Columns = new[]
                {
                    new { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations" },
                    new { Name = "DepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Departments" },
                    new { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Last update timestamp (nullable)" }
                }
            },
            new
            {
                Name = "AuditRecords",
                Description = "Contains audit/compliance records for users (main data table)",
                Columns = new[]
                {
                    new { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new { Name = "UserId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to OrganisationUsers" },
                    new { Name = "Date", DataType = "int", IsPrimaryKey = false, Description = "Date as yyyyMMdd integer (e.g., 20240115)" },
                    new { Name = "Status", DataType = "nvarchar(20)", IsPrimaryKey = false, Description = "Status: 'Valid' or 'Invalid'" },
                    new { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Last update timestamp (nullable)" }
                }
            },
            new
            {
                Name = "Managers",
                Description = "Contains manager accounts",
                Columns = new[]
                {
                    new { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations" },
                    new { Name = "ManagesAllDepartments", DataType = "bit", IsPrimaryKey = false, Description = "If true, has access to all departments" },
                    new { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Last update timestamp (nullable)" }
                }
            },
            new
            {
                Name = "ManagerDepartments",
                Description = "Junction table for manager-department relationships",
                Columns = new[]
                {
                    new { Name = "ManagerId", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "FK to Managers (composite PK)" },
                    new { Name = "DepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "FK to Departments (composite PK)" }
                }
            }
        };
    }

    private static List<object> GetRelationships()
    {
        return new List<object>
        {
            new { From = "Departments.OrganisationId", To = "Organisations.Id", Type = "Many-to-One" },
            new { From = "Departments.ParentDepartmentId", To = "Departments.Id", Type = "Self-reference" },
            new { From = "OrganisationUsers.OrganisationId", To = "Organisations.Id", Type = "Many-to-One" },
            new { From = "OrganisationUsers.DepartmentId", To = "Departments.Id", Type = "Many-to-One" },
            new { From = "AuditRecords.UserId", To = "OrganisationUsers.Id", Type = "Many-to-One" },
            new { From = "Managers.OrganisationId", To = "Organisations.Id", Type = "Many-to-One" },
            new { From = "ManagerDepartments.ManagerId", To = "Managers.Id", Type = "Many-to-One" },
            new { From = "ManagerDepartments.DepartmentId", To = "Departments.Id", Type = "Many-to-One" }
        };
    }

    private static List<string> GetImportantNotes()
    {
        return new List<string>
        {
            "Date in AuditRecords is yyyyMMdd integer. Year: Date/10000, Month: (Date/100)%100, Day: Date%100",
            "Status values are exactly 'Valid' or 'Invalid' (case-sensitive)",
            "NEVER select PasswordHash from Users table",
            "Always include WHERE clauses for AuditRecords (millions of records)",
            "Use JOINs: AuditRecords -> OrganisationUsers -> Departments/Organisations",
            "Row-level security will filter results by manager's organisation and departments"
        };
    }
}
