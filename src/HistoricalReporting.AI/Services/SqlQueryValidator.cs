using System.Text.RegularExpressions;

namespace HistoricalReporting.AI.Services;

public class SqlQueryValidator
{
    private static readonly HashSet<string> ForbiddenKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE",
        "EXEC", "EXECUTE", "SP_", "XP_", "GRANT", "REVOKE", "DENY",
        "BACKUP", "RESTORE", "SHUTDOWN", "KILL", "WAITFOR",
        "OPENROWSET", "OPENDATASOURCE", "OPENQUERY", "BULK",
        "CMDSHELL", "RECONFIGURE", "DBCC"
    };

    private static readonly HashSet<string> ForbiddenColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "Password", "Secret", "ApiKey", "Token"
    };

    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Organisations", "Departments", "Managers", "ManagerDepartments",
        "OrganisationUsers", "AuditRecords", "Reports", "Users", "NlpQueryLogs"
    };

    public SqlValidationResult Validate(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlValidationResult.Fail("SQL query cannot be empty.");
        }

        var normalizedSql = NormalizeSql(sql);
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for forbidden keywords
        foreach (var keyword in ForbiddenKeywords)
        {
            if (ContainsKeyword(normalizedSql, keyword))
            {
                errors.Add($"Forbidden keyword detected: {keyword}. Only SELECT queries are allowed.");
            }
        }

        // Check for forbidden columns
        foreach (var column in ForbiddenColumns)
        {
            if (ContainsColumnReference(normalizedSql, column))
            {
                errors.Add($"Forbidden column detected: {column}. This column cannot be selected.");
            }
        }

        // Check for comment-based SQL injection
        if (normalizedSql.Contains("--") || normalizedSql.Contains("/*"))
        {
            errors.Add("SQL comments are not allowed.");
        }

        // Check for multiple statements
        if (ContainsMultipleStatements(normalizedSql))
        {
            errors.Add("Multiple SQL statements are not allowed.");
        }

        // Check that it starts with SELECT
        if (!normalizedSql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Query must start with SELECT.");
        }

        // Check for UNION-based injection
        if (Regex.IsMatch(normalizedSql, @"\bUNION\s+(?:ALL\s+)?SELECT\b", RegexOptions.IgnoreCase))
        {
            warnings.Add("UNION SELECT detected. Ensure this is intentional.");
        }

        // Check for referenced tables
        var referencedTables = ExtractTableNames(normalizedSql);
        foreach (var table in referencedTables)
        {
            if (!AllowedTables.Contains(table))
            {
                warnings.Add($"Query references table '{table}' which may not exist.");
            }
        }

        // Check for missing WHERE clause on large tables
        if (normalizedSql.Contains("AuditRecords", StringComparison.OrdinalIgnoreCase) &&
            !normalizedSql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Query on AuditRecords without WHERE clause may be slow.");
        }

        // Check for missing TOP/LIMIT
        if (!Regex.IsMatch(normalizedSql, @"\bTOP\s+\d+\b", RegexOptions.IgnoreCase) &&
            !normalizedSql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Consider using TOP N to limit results.");
        }

        if (errors.Count > 0)
        {
            return SqlValidationResult.Fail(string.Join(" ", errors), warnings);
        }

        return SqlValidationResult.Pass(warnings);
    }

    public string SanitizeAndEnforce(string sql, Guid organisationId, List<Guid>? departmentIds, bool managesAllDepartments)
    {
        var sanitized = sql.Trim();

        // Add TOP if not present
        if (!Regex.IsMatch(sanitized, @"\bTOP\s+\d+\b", RegexOptions.IgnoreCase))
        {
            sanitized = Regex.Replace(sanitized, @"^SELECT\s+", "SELECT TOP 1000 ", RegexOptions.IgnoreCase);
        }

        // Note: Row-level security enforcement should be done at the parameter level
        // The actual filtering is done by replacing @OrganisationId and @DepartmentIds parameters

        return sanitized;
    }

    private static string NormalizeSql(string sql)
    {
        // Remove extra whitespace
        return Regex.Replace(sql, @"\s+", " ").Trim();
    }

    private static bool ContainsKeyword(string sql, string keyword)
    {
        // Match keyword as a whole word
        var pattern = $@"\b{Regex.Escape(keyword)}\b";
        return Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase);
    }

    private static bool ContainsColumnReference(string sql, string column)
    {
        // Match column name in SELECT or other contexts
        var patterns = new[]
        {
            $@"\b{Regex.Escape(column)}\b",
            $@"\.\s*{Regex.Escape(column)}\b",
            $@"\[\s*{Regex.Escape(column)}\s*\]"
        };

        return patterns.Any(p => Regex.IsMatch(sql, p, RegexOptions.IgnoreCase));
    }

    private static bool ContainsMultipleStatements(string sql)
    {
        // Check for semicolons that might indicate multiple statements
        var semicolonCount = sql.Count(c => c == ';');
        return semicolonCount > 1 || (semicolonCount == 1 && !sql.TrimEnd().EndsWith(';'));
    }

    private static List<string> ExtractTableNames(string sql)
    {
        var tables = new List<string>();

        // Match FROM and JOIN clauses
        var patterns = new[]
        {
            @"\bFROM\s+(\[?[\w]+\]?)",
            @"\bJOIN\s+(\[?[\w]+\]?)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(sql, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var tableName = match.Groups[1].Value.Trim('[', ']');
                if (!string.IsNullOrEmpty(tableName))
                {
                    tables.Add(tableName);
                }
            }
        }

        return tables.Distinct().ToList();
    }
}

public class SqlValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Warnings { get; private set; } = new();

    public static SqlValidationResult Pass(List<string>? warnings = null)
    {
        return new SqlValidationResult
        {
            IsValid = true,
            Warnings = warnings ?? new List<string>()
        };
    }

    public static SqlValidationResult Fail(string error, List<string>? warnings = null)
    {
        return new SqlValidationResult
        {
            IsValid = false,
            ErrorMessage = error,
            Warnings = warnings ?? new List<string>()
        };
    }
}
