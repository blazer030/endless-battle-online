using System.Diagnostics;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// scripts/import-wog45.py 在原版資料缺欄位或參照不存在時：以非 0 結束、印出檔名與紀錄 id，而且不寫任何檔
public class ImportScriptTests
{
    private static readonly string RepositoryDirectory = Path.GetFullPath(Path.Combine(BackendPaths.BackendDirectory, ".."));

    // 缺的欄位：道具的價格（require 檢查的欄位）與配方材料的數量（直接讀取的子物件欄位）
    [Theory]
    [InlineData("items.json", "price", "items.json 的紀錄 1 缺少欄位 price")]
    [InlineData("recipes.json", "materials.count", "recipes.json 的紀錄 1 缺少欄位 count")]
    public Task Import_MissingSourceFieldStopsWithoutWriting(string file, string field, string message) =>
        AssertImportStopsAsync(file, records => RemoveFirstRecordField(records, field), message);

    // 前置技能指到不存在的紀錄
    [Fact]
    public Task Import_UnknownPrerequisiteStopsWithoutWriting() =>
        AssertImportStopsAsync("skills.json", records =>
        {
            foreach (var record in records.Select(record => record!.AsObject()).Where(record => record["requiresSkillId"] is { } required && required.GetValue<int>() != 0))
            {
                record["requiresSkillId"] = 999999;
            }
        }, "的 requiresSkillId 999999 不存在");

    private static async Task AssertImportStopsAsync(string file, Action<JsonArray> change, string message)
    {
        var root = Directory.CreateTempSubdirectory("ebo-import-").FullName;
        try
        {
            CopyImportInputs(root);
            ChangeRecords(Path.Combine(root, "docs/research/wog45/data", file), change);
            var contentBefore = Snapshot(Path.Combine(root, "backend/content"));

            var (exitCode, error) = await RunAsync(Path.Combine(root, "scripts/import-wog45.py"));

            Assert.NotEqual(0, exitCode);
            Assert.Contains(message, error);
            Assert.Contains($"{file} 的紀錄", error);
            Assert.Equal(contentBefore, Snapshot(Path.Combine(root, "backend/content")));
            Assert.False(File.Exists(Path.Combine(root, "docs/research/wog45/gaps.md")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void CopyImportInputs(string root)
    {
        CopyDirectory(Path.Combine(RepositoryDirectory, "scripts"), Path.Combine(root, "scripts"));
        CopyDirectory(Path.Combine(RepositoryDirectory, "docs/research/wog45/data"), Path.Combine(root, "docs/research/wog45/data"));
        CopyDirectory(Path.Combine(RepositoryDirectory, "backend/content"), Path.Combine(root, "backend/content"));
    }

    private static void ChangeRecords(string path, Action<JsonArray> change)
    {
        var records = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
        change(records);
        File.WriteAllText(path, records.ToJsonString());
    }

    private static void RemoveFirstRecordField(JsonArray records, string field)
    {
        var first = records[0]!.AsObject();
        if (field.Split('.') is [var list, var nested])
        {
            first[list]!.AsArray()[0]!.AsObject().Remove(nested);
        }
        else
        {
            first.Remove(field);
        }
    }

    private static async Task<(int ExitCode, string Error)> RunAsync(string script)
    {
        using var process = Process.Start(new ProcessStartInfo("python3", ["-I", script])
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        })!;
        var error = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, error);
    }

    private static Dictionary<string, string> Snapshot(string directory) =>
        Directory.GetFiles(directory).ToDictionary(file => Path.GetFileName(file), File.ReadAllText);

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
    }
}
