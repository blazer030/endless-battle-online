using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace EndlessBattle.Infrastructure.Persistence;

internal static class MySqlErrors
{
    private const int DuplicateEntry = 1062;

    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: DuplicateEntry };
}
