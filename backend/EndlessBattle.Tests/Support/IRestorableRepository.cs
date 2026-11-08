namespace EndlessBattle.Tests.Support;

// InMemoryUnitOfWork 回滾時用：Snapshot 回傳一個把 repository 還原到此刻狀態的動作
public interface IRestorableRepository
{
    Action Snapshot();
}
