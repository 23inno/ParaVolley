using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace SportsManagementMVC.Data;

public static class DatabaseConflictClassifier
{
    public static bool IsUniqueViolation(
        DbUpdateException exception,
        DatabaseFacade database,
        string? constraintName = null)
    {
        if (exception.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return constraintName == null ||
                string.Equals(
                    postgres.ConstraintName,
                    constraintName,
                    StringComparison.Ordinal);
        }

        // SQLite is used only by the integration test host. Avoid taking a
        // production dependency on its provider while still exercising real
        // database uniqueness behavior in tests.
        return database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite" &&
            exception.InnerException?.Message.Contains(
                "UNIQUE constraint failed",
                StringComparison.OrdinalIgnoreCase) == true;
    }
}
