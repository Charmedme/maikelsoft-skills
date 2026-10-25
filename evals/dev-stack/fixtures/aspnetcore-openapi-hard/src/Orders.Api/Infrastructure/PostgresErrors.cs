using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Orders.Api.Infrastructure;

public static class PostgresErrors
{
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
