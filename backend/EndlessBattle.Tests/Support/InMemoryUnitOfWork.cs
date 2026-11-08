using EndlessBattle.Application.Common;

namespace EndlessBattle.Tests.Support;

// 應用層測試用：直接執行工作並依序記錄 begin、commit、rollback；巢狀呼叫沿用外層交易。
// 回滾時把登記的 repository 還原成交易開始時的狀態，模擬資料庫交易
public sealed class InMemoryUnitOfWork(params IRestorableRepository[] repositories) : IUnitOfWork
{
    private bool active;

    public List<string> Log { get; } = [];

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        if (active)
        {
            return await work(cancellationToken);
        }

        var restores = repositories.Select(repository => repository.Snapshot()).ToList();
        Log.Add("begin");
        active = true;
        try
        {
            var result = await work(cancellationToken);
            Log.Add("commit");
            return result;
        }
        catch
        {
            restores.ForEach(restore => restore());
            Log.Add("rollback");
            throw;
        }
        finally
        {
            active = false;
        }
    }
}
