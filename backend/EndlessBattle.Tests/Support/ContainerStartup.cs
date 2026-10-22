namespace EndlessBattle.Tests.Support;

public static class ContainerStartup
{
    // 每次嘗試都建立新的實例；逾時或失敗時先 dispose 再重試，最後一次仍失敗丟 TimeoutException（逾時）或原例外
    public static async Task<T> StartWithRetryAsync<T>(
        Func<T> create,
        Func<T, CancellationToken, Task> start,
        Func<T, ValueTask> dispose,
        TimeSpan timeout,
        int attempts)
    {
        for (var attempt = 1; ; attempt++)
        {
            var instance = create();
            try
            {
                using var cancellation = new CancellationTokenSource(timeout);
                // WaitAsync 保證即使 start 不理會取消也會在逾時時返回
                await start(instance, cancellation.Token).WaitAsync(timeout);
                return instance;
            }
            catch (Exception exception)
            {
                await DisposeQuietlyAsync(instance, dispose);
                if (attempt >= attempts)
                {
                    if (exception is TimeoutException) throw;
                    if (exception is OperationCanceledException)
                        throw new TimeoutException($"容器在 {timeout} 內沒有就緒", exception);
                    throw;
                }
            }
        }
    }

    private static async Task DisposeQuietlyAsync<T>(T instance, Func<T, ValueTask> dispose)
    {
        try
        {
            await dispose(instance);
        }
        catch
        {
            // 卡住的容器可能連 dispose 都失敗；不要蓋掉原本的錯誤
        }
    }
}
