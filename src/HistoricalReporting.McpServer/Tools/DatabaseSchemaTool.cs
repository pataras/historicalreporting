using System.ComponentModel;
using System.Text.Json;
using HistoricalReporting.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace HistoricalReporting.McpServer.Tools;

[McpServerToolType]
public class DatabaseSchemaTool
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseSchemaTool(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [McpServerTool("get_database_schema")]
    [Description("Returns the complete database schema including tables, columns, data types, and relationships. Use this to understand the database structure before generating SQL queries.")]
    public async Task<string> GetDatabaseSchemaAsync()
    {
        var schema = new DatabaseSchema
        {
            Tables = GetTableDefinitions(),
            Relationships = GetRelationships(),
            ImportantNotes = GetImportantNotes()
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

    private List<TableDefinition> GetTableDefinitions()
    {
        return new List<TableDefinition>
        {
            new TableDefinition
            {
                Name = "Organisations",
                Description = "Contains organisation/company information",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "Name", DataType = "nvarchar(10)", IsPrimaryKey = false, Description = "Organisation name/code (e.g., 'Org1', 'Org2')" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "Departments",
                Description = "Contains department information within organisations. Supports hierarchical structure with parent departments.",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "Name", DataType = "nvarchar(100)", IsPrimaryKey = false, Description = "Department name (e.g., 'Information Technology', 'Human Resources')" },
                    new ColumnDefinition { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations table" },
                    new ColumnDefinition { Name = "ParentDepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = false, IsNullable = true, Description = "Self-referencing foreign key for hierarchical departments" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "Managers",
                Description = "Contains manager accounts that can view reports for their assigned departments",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations table" },
                    new ColumnDefinition { Name = "ManagesAllDepartments", DataType = "bit", IsPrimaryKey = false, Description = "If true, manager has access to all departments in the organisation" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "ManagerDepartments",
                Description = "Junction table linking managers to their assigned departments (many-to-many relationship)",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "ManagerId", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Foreign key to Managers table (composite PK)" },
                    new ColumnDefinition { Name = "DepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Foreign key to Departments table (composite PK)" }
                }
            },
            new TableDefinition
            {
                Name = "OrganisationUsers",
                Description = "Contains users/employees within organisations, assigned to departments",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "OrganisationId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Organisations table" },
                    new ColumnDefinition { Name = "DepartmentId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to Departments table" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "AuditRecords",
                Description = "Contains audit/compliance records for users. This is the main data table with millions of records.",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "UserId", DataType = "uniqueidentifier", IsPrimaryKey = false, Description = "Foreign key to OrganisationUsers table" },
                    new ColumnDefinition { Name = "Date", DataType = "int", IsPrimaryKey = false, Description = "Date as integer in yyyyMMdd format (e.g., 20240115 for January 15, 2024)" },
                    new ColumnDefinition { Name = "Status", DataType = "nvarchar(20)", IsPrimaryKey = false, Description = "Audit status: 'Valid' or 'Invalid'" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "Users",
                Description = "Contains system user accounts for authentication (managers login with these)",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "Email", DataType = "nvarchar(256)", IsPrimaryKey = false, Description = "Unique email address for login" },
                    new ColumnDefinition { Name = "PasswordHash", DataType = "nvarchar(max)", IsPrimaryKey = false, Description = "Hashed password (never select this)" },
                    new ColumnDefinition { Name = "FirstName", DataType = "nvarchar(100)", IsPrimaryKey = false, Description = "User's first name" },
                    new ColumnDefinition { Name = "LastName", DataType = "nvarchar(100)", IsPrimaryKey = false, Description = "User's last name" },
                    new ColumnDefinition { Name = "ManagerId", DataType = "uniqueidentifier", IsPrimaryKey = false, IsNullable = true, Description = "Foreign key to Managers table (if user is a manager)" },
                    new ColumnDefinition { Name = "LastLoginAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last login timestamp" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            },
            new TableDefinition
            {
                Name = "Reports",
                Description = "Contains saved report definitions",
                Columns = new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "Id", DataType = "uniqueidentifier", IsPrimaryKey = true, Description = "Primary key" },
                    new ColumnDefinition { Name = "Name", DataType = "nvarchar(200)", IsPrimaryKey = false, Description = "Report name" },
                    new ColumnDefinition { Name = "Description", DataType = "nvarchar(1000)", IsPrimaryKey = false, IsNullable = true, Description = "Report description" },
                    new ColumnDefinition { Name = "Query", DataType = "nvarchar(max)", IsPrimaryKey = false, Description = "SQL query for the report" },
                    new ColumnDefinition { Name = "Status", DataType = "nvarchar(50)", IsPrimaryKey = false, Description = "Report status" },
                    new ColumnDefinition { Name = "CreatedAt", DataType = "datetime2", IsPrimaryKey = false, Description = "Record creation timestamp" },
                    new ColumnDefinition { Name = "UpdatedAt", DataType = "datetime2", IsPrimaryKey = false, IsNullable = true, Description = "Last update timestamp" }
                }
            }
        };
    }

    private List<RelationshipDefinition> GetRelationships()
    {
        return new List<RelationshipDefinition>
        {
            new RelationshipDefinition
            {
                FromTable = "Departments",
                FromColumn = "OrganisationId",
                ToTable = "Organisations",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Each department belongs to one organisation"
            },
            new RelationshipDefinition
            {
                FromTable = "Departments",
                FromColumn = "ParentDepartmentId",
                ToTable = "Departments",
                ToColumn = "Id",
                RelationshipType = "Many-to-One (Self-referencing)",
                Description = "Departments can have parent departments for hierarchy"
            },
            new RelationshipDefinition
            {
                FromTable = "Managers",
                FromColumn = "OrganisationId",
                ToTable = "Organisations",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Each manager belongs to one organisation"
            },
            new RelationshipDefinition
            {
                FromTable = "ManagerDepartments",
                FromColumn = "ManagerId",
                ToTable = "Managers",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Links managers to their departments"
            },
            new RelationshipDefinition
            {
                FromTable = "ManagerDepartments",
                FromColumn = "DepartmentId",
                ToTable = "Departments",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Links departments to their managers"
            },
            new RelationshipDefinition
            {
                FromTable = "OrganisationUsers",
                FromColumn = "OrganisationId",
                ToTable = "Organisations",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Each user belongs to one organisation"
            },
            new RelationshipDefinition
            {
                FromTable = "OrganisationUsers",
                FromColumn = "DepartmentId",
                ToTable = "Departments",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Each user is assigned to one department"
            },
            new RelationshipDefinition
            {
                FromTable = "AuditRecords",
                FromColumn = "UserId",
                ToTable = "OrganisationUsers",
                ToColumn = "Id",
                RelationshipType = "Many-to-One",
                Description = "Each audit record belongs to one organisation user"
            },
            new RelationshipDefinition
            {
                FromTable = "Users",
                FromColumn = "ManagerId",
                ToTable = "Managers",
                ToColumn = "Id",
                RelationshipType = "One-to-One",
                Description = "System users can be linked to manager records"
            }
        };
    }

    private List<string> GetImportantNotes()
    {
        return new List<string>
        {
            "The Date column in AuditRecords is stored as an integer in yyyyMMdd format. To filter by year, use: Date / 10000. To filter by month, use: (Date / 100) % 100. To filter by day, use: Date % 100.",
            "The Status column in AuditRecords contains only two values: 'Valid' or 'Invalid'.",
            "NEVER select PasswordHash from the Users table.",
            "For performance, always include appropriate WHERE clauses when querying AuditRecords as it contains millions of records.",
            "Use COUNT, SUM, and GROUP BY for aggregation queries on AuditRecords.",
            "Common query patterns: Count valid/invalid records per department, count records per month/year, aggregate by organisation.",
            "Row-level security is applied - queries will be filtered to only show data the manager has access to."
        };
    }
}

public class DatabaseSchema
{
    public List<TableDefinition> Tables { get; set; } = new();
    public List<RelationshipDefinition> Relationships { get; set; } = new();
    public List<string> ImportantNotes { get; set; } = new();
}

public class TableDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ColumnDefinition> Columns { get; set; } = new();
}

public class ColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class RelationshipDefinition
{
    public string FromTable { get; set; } = string.Empty;
    public string FromColumn { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
    public string ToColumn { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
