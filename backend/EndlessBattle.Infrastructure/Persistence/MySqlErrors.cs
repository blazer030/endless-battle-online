using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace EndlessBattle.Infrastructure.Persistence;

internal static class MySqlErrors
{
    private const int DuplicateEntry = 1062;
    private const int LockWaitTimeout = 1205;
    private const int Deadlock = 1213;

    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: DuplicateEntry };

    // 撞到的是不是這個唯一鍵：MySQL 的訊息是 Duplicate entry '…' for key '<資料表>.<鍵名>'
    public static bool IsDuplicateKey(DbUpdateException exception, string keyName) =>
        exception.InnerException is MySqlException { Number: DuplicateEntry } duplicate
        && duplicate.Message.EndsWith($".{keyName}'", StringComparison.Ordinal);

    // 原生 SQL 直接丟 MySqlException；SaveChanges 包成 DbUpdateException
    public static bool IsDeadlockOrLockTimeout(Exception exception) =>
        exception is MySqlException { Number: Deadlock or LockWaitTimeout }
            || exception.InnerException is MySqlException { Number: Deadlock or LockWaitTimeout };
}
