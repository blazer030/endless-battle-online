using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace Inkbound.Tests.Support;

// 測試用：依 Flyway 的版本順序直接執行 migration，不必另外啟動 Flyway 容器
public static partial class MigrationRunner
{
    [GeneratedRegex(@"^V(\d{9})__[a-z0-9_]+\.sql$")]
    private static partial Regex MigrationFileName();

    public static async Task ApplyAllAsync(string connectionString, string databaseDirectory)
    {
        var migrations = new[] { "ddl", "dml" }
            .Select(folder => Path.Combine(databaseDirectory, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.GetFiles(folder, "*.sql"))
            .Select(path => (Path: path, Version: ParseVersion(path)))
            .ToList();

        var duplicateVersion = migrations.GroupBy(migration => migration.Version).FirstOrDefault(group => group.Count() > 1);
        if (duplicateVersion is not null)
        {
            throw new InvalidOperationException($"Migration 版本號重複：{duplicateVersion.Key}");
        }

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var migration in migrations.OrderBy(migration => migration.Version))
        {
            var script = new MySqlScript(connection, await File.ReadAllTextAsync(migration.Path));
            await script.ExecuteAsync();
        }
    }

    private static long ParseVersion(string path)
    {
        var match = MigrationFileName().Match(Path.GetFileName(path));
        if (!match.Success)
        {
            throw new InvalidOperationException($"Migration 檔名不合法：{path}");
        }

        return long.Parse(match.Groups[1].Value);
    }
}
