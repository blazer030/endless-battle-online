using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class ContainerStartupTests
{
    private sealed class FakeContainer
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(50);

    private static Task HangUntilCancelled(CancellationToken token) => Task.Delay(Timeout.Infinite, token);

    [Fact]
    public async Task StartWithRetry_RetriesAfterTimeout()
    {
        var created = new List<FakeContainer>();

        var result = await ContainerStartup.StartWithRetryAsync(
            () =>
            {
                var container = new FakeContainer();
                created.Add(container);
                return container;
            },
            (container, token) => created.Count == 1 ? HangUntilCancelled(token) : Task.CompletedTask,
            container =>
            {
                container.Dispose();
                return ValueTask.CompletedTask;
            },
            ShortTimeout,
            attempts: 2);

        Assert.Same(created[1], result);
        Assert.Equal(1, created[0].DisposeCount);
        Assert.Equal(0, created[1].DisposeCount);
    }

    [Fact]
    public async Task StartWithRetry_ThrowsTimeoutAfterLastAttempt()
    {
        var created = new List<FakeContainer>();

        await Assert.ThrowsAsync<TimeoutException>(() => ContainerStartup.StartWithRetryAsync(
            () =>
            {
                var container = new FakeContainer();
                created.Add(container);
                return container;
            },
            (_, token) => HangUntilCancelled(token),
            container =>
            {
                container.Dispose();
                return ValueTask.CompletedTask;
            },
            ShortTimeout,
            attempts: 2));

        Assert.Equal(2, created.Count);
        Assert.All(created, container => Assert.Equal(1, container.DisposeCount));
    }

    [Fact]
    public async Task StartWithRetry_DoesNotRetryAfterSuccess()
    {
        var createCount = 0;

        await ContainerStartup.StartWithRetryAsync(
            () =>
            {
                createCount++;
                return new FakeContainer();
            },
            (_, _) => Task.CompletedTask,
            _ => ValueTask.CompletedTask,
            ShortTimeout,
            attempts: 2);

        Assert.Equal(1, createCount);
    }

    [Fact]
    public async Task StartWithRetry_RethrowsOriginalExceptionAfterLastAttempt()
    {
        var created = new List<FakeContainer>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ContainerStartup.StartWithRetryAsync(
            () =>
            {
                var container = new FakeContainer();
                created.Add(container);
                return container;
            },
            (_, _) => throw new InvalidOperationException("boom"),
            container =>
            {
                container.Dispose();
                return ValueTask.CompletedTask;
            },
            ShortTimeout,
            attempts: 2));

        Assert.Equal("boom", exception.Message);
        Assert.Equal(2, created.Count);
        Assert.All(created, container => Assert.Equal(1, container.DisposeCount));
    }
}
