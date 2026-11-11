using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Application.Balance;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class BalanceReportServiceTests
{
    private static readonly BalanceReportService Service = new();

    // TestContent 再加兩把高階劍：L2 需要力量 30、L3 需要力量 400
    private static JsonGameContent ContentWithHeavySwords(AdminContentDirectories directories)
    {
        var path = Path.Combine(directories.Official, "items.json");
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var items = root["items"]!.AsArray();
        var template = items.Single(item => item!["id"]!.GetValue<string>() == "test-sword")!;
        foreach (var (id, shopLevel, strength) in new[] { ("report-heavy-sword", 2, 30), ("report-giant-sword", 3, 400) })
        {
            var sword = template.DeepClone().AsObject();
            sword["id"] = id;
            sword["shopLevel"] = shopLevel;
            sword["requires"] = new JsonObject { ["str"] = strength };
            items.Add(sword);
        }

        File.WriteAllText(path, ContentFileFormatter.Format(root));
        return JsonGameContent.Load(directories.Official);
    }

    [Fact]
    public void WearLevels_LowestLevelWhoseExpectedStrengthMeetsEachShopLevel()
    {
        using var directories = new AdminContentDirectories();

        var report = Service.Build(ContentWithHeavySwords(directories), new BalanceReportOptions());

        // 創角力量 4 + 10 = 14；戰士每級期望 2.5：Lv8 為 14 + ⌊7 × 2.5⌋ = 31 ≥ 30，Lv7 只有 29
        // 盜賊每級 1.5：Lv12 為 14 + ⌊11 × 1.5⌋ = 30；術士每級 0.5：Lv33 為 14 + 16 = 30
        Assert.Equal([1, 2, 3], report.ShopLevels);
        Assert.Equal([1, 8, null], StrengthLevels(report, "warrior"));
        Assert.Equal([1, 12, null], StrengthLevels(report, "thief"));
        Assert.Equal([1, 33, null], StrengthLevels(report, "sorcerer"));
    }

    [Fact]
    public void WearLevels_StatWithoutRequirementsHasNoRequiredValue()
    {
        var report = Service.Build(TestGameContent.Instance, new BalanceReportOptions());

        var agility = report.WearLevels.Single(row => row.JobId == "warrior" && row.Stat == "agility");
        Assert.All(agility.Cells, cell => Assert.Null(cell.Required));
    }

    [Fact]
    public void Adventures_ReferenceCharacterStartsAtThePlacesRecommendedMinimum()
    {
        var report = Service.Build(TestGameContent.Instance, new BalanceReportOptions());

        Assert.Equal(16, report.Adventures.Count);
        var cave = report.Adventures.Single(row => row.PlaceId == 2 && row.JobId == "warrior");
        Assert.Equal(6, cave.Level);
        Assert.Equal([1, 1], cave.EquipmentShopLevels);
        Assert.True(cave.WithoutSkills.Fights > 0);
    }

    [Fact]
    public void Build_SameInputGivesTheSameReport()
    {
        var first = JsonSerializer.Serialize(Service.Build(TestGameContent.Instance, new BalanceReportOptions()));
        var second = JsonSerializer.Serialize(Service.Build(TestGameContent.Instance, new BalanceReportOptions()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Adventures_OnlyTheWithSkillsResultsFollowASkillChange()
    {
        using var directories = new AdminContentDirectories();
        var path = Path.Combine(directories.Official, "skills.json");
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var burst = root["skills"]!.AsArray().Single(skill => skill!["id"]!.GetValue<string>() == "burst")!;
        foreach (var level in burst["levels"]!.AsArray())
        {
            level!["conditions"]![0]!["percent"] = 100;
        }

        File.WriteAllText(path, ContentFileFormatter.Format(root));

        var official = Warrior(Service.Build(TestGameContent.Instance, new BalanceReportOptions()));
        var changed = Warrior(Service.Build(JsonGameContent.Load(directories.Official), new BalanceReportOptions()));

        Assert.Equal(official.Select(row => JsonSerializer.Serialize(row.WithoutSkills)), changed.Select(row => JsonSerializer.Serialize(row.WithoutSkills)));
        Assert.NotEqual(official.Select(row => JsonSerializer.Serialize(row.WithSkills)), changed.Select(row => JsonSerializer.Serialize(row.WithSkills)));
    }

    [Fact]
    public void Adventures_SkillLevelOptionChangesTheWithSkillsResults()
    {
        var first = Warrior(Service.Build(TestGameContent.Instance, new BalanceReportOptions(BalanceSkillLevel.First)));
        var max = Warrior(Service.Build(TestGameContent.Instance, new BalanceReportOptions(BalanceSkillLevel.Max)));

        Assert.NotEqual(first.Select(row => JsonSerializer.Serialize(row.WithSkills)), max.Select(row => JsonSerializer.Serialize(row.WithSkills)));
        Assert.Equal(first.Select(row => JsonSerializer.Serialize(row.WithoutSkills)), max.Select(row => JsonSerializer.Serialize(row.WithoutSkills)));
    }

    [Fact]
    public void Adventures_JobWithoutSkillsHasTheSameResultsWithAndWithoutSkills()
    {
        var report = Service.Build(TestGameContent.Instance, new BalanceReportOptions());

        Assert.All(
            report.Adventures.Where(row => row.JobId == "sorcerer"),
            row => Assert.Equal(JsonSerializer.Serialize(row.WithoutSkills), JsonSerializer.Serialize(row.WithSkills)));
    }

    private static IReadOnlyList<AdventureRow> Warrior(BalanceReport report) => report.Adventures.Where(row => row.JobId == "warrior").ToList();

    private static IReadOnlyList<int?> StrengthLevels(BalanceReport report, string jobId) =>
        report.WearLevels.Single(row => row.JobId == jobId && row.Stat == "strength").Cells.Select(cell => cell.Level).ToList();
}
