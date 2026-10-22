using EndlessBattle.Application.Common;

namespace EndlessBattle.Infrastructure.Persistence;

// 所有 repository 共用同一個 scoped DbContext，交易開在它的連線上，裡面的 SaveChanges 與原生 SQL 都會加入。
// 已有交易（巢狀呼叫）就直接執行，由最外層提交或回滾
public sealed class EfUnitOfWork(EndlessBattleDbContext context) : IUnitOfWork
{
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return await work(cancellationToken);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            // 請求被取消也要回滾；回滾後追蹤中的實體已與資料庫不符，一併清掉
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
