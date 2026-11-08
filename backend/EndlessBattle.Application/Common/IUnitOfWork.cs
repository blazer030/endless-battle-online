namespace EndlessBattle.Application.Common;

// 把一個用例的寫入包成一筆交易：work 成功才提交，丟例外就回滾並照常拋出。
// 已在交易中時（巢狀呼叫）沿用外層交易，由最外層決定提交或回滾
public interface IUnitOfWork
{
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);

    Task RunAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
        RunAsync(async token =>
        {
            await work(token);
            return true;
        }, cancellationToken);
}
