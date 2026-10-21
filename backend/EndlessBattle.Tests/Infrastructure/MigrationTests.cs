using EndlessBattle.Tests.Support;
using MySql.Data.MySqlClient;

namespace EndlessBattle.Tests.Infrastructure;

// 共用容器上另開一個空的 schema，從 V5 的形狀開始遷移，不影響其他測試的資料庫
public class MigrationTests(MySqlDatabaseFixture database)
{
    private const long VersionBeforePlace = 5;

    private static string DatabaseDirectory => Path.Combine(BackendPaths.BackendDirectory, "db");

    [Fact]
    public async Task Migration_BackfillsPlaceForExistingCharacters()
    {
        var connectionString = await CreateEmptySchemaAsync();
        await MigrationRunner.ApplyUpToAsync(connectionString, DatabaseDirectory, VersionBeforePlace);
        await ExecuteAsync(connectionString, """
            INSERT INTO accounts (id, username, password_hash, created_at) VALUES (1, 'veteran', 'hash', UTC_TIMESTAMP(6));
            INSERT INTO characters (account_id, name, current_stage_id, last_settled_at, created_at)
            VALUES (1, '老角色', 'frost-peak-1', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6));
            """);

        await MigrationRunner.ApplyAfterAsync(connectionString, DatabaseDirectory, VersionBeforePlace);

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new MySqlCommand(
            "SELECT current_place_id, current_sub_place, current_stage_id FROM characters WHERE name = '老角色'", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal("frost-peak-1", reader.GetString(2));
    }

    // Testcontainers 的 root 密碼與一般使用者相同；一般使用者沒有建立 schema 的權限
    private async Task<string> CreateEmptySchemaAsync()
    {
        var schema = $"migration_{Guid.NewGuid():N}"[..32];
        var root = new MySqlConnectionStringBuilder(database.ConnectionString) { UserID = "root", Database = "" };
        await ExecuteAsync(root.ConnectionString, $"CREATE DATABASE {schema}");
        return new MySqlConnectionStringBuilder(root.ConnectionString) { Database = schema }.ConnectionString;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await new MySqlScript(connection, sql).ExecuteAsync(TestContext.Current.CancellationToken);
    }
}
