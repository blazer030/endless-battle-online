using System.Data;
using EndlessBattle.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EndlessBattle.Infrastructure.Persistence;

// 所有 repository 共用同一個 scoped DbContext，交易開在它的連線上，裡面的 SaveChanges 與原生 SQL 都會加入。
// 已有交易（巢狀呼叫）就直接執行，由最外層提交或回滾。
// READ COMMITTED：不同角色的請求不會因間隙鎖互相卡住；同一角色的並行請求靠角色列的版本鎖排隊
public sealed class EfUnitOfWork(EndlessBattleDbContext context) : IUnitOfWork
{
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await work(cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            await RollbackQuietlyAsync(transaction);
            if (MySqlErrors.IsDeadlockOrLockTimeout(exception))
            {
                throw new ConflictException("資料正被其他請求使用，請重試");
            }

            throw;
        }
    }

    // 請求被取消也要回滾；回滾後追蹤中的實體已與資料庫不符，一併清掉。
    // 回滾本身失敗（例如連線已斷）時吞掉，讓呼叫端看到原本的例外
    private async Task RollbackQuietlyAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // 連線斷掉時伺服器會自行回滾未提交的交易
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }
}
