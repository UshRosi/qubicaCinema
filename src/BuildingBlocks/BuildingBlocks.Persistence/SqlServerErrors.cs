using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QubicaCinema.BuildingBlocks.Persistence;

/// <summary>Recognises the SQL Server failures that stand for a business rule.</summary>
public static class SqlServerErrors
{
    /// <summary>SQL Server's error numbers for a unique index and a unique constraint violation.</summary>
    private static readonly int[] UniqueViolationNumbers = [2601, 2627];

    /// <summary>
    /// Whether the save failed because it would have broken the named unique index or key.
    /// </summary>
    /// <remarks>
    /// The name is checked, not only the number: a table can have several unique indexes, and only the one
    /// that encodes a business rule may be turned into that rule's exception. Any other violation is a bug
    /// and should surface as one.
    /// </remarks>
    public static bool IsUniqueViolation(DbUpdateException exception, string indexName) =>
        exception.InnerException is SqlException sql
        && UniqueViolationNumbers.Contains(sql.Number)
        && sql.Message.Contains(indexName, StringComparison.Ordinal);
}
