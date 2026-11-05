using System.Text.Json;
using EndlessBattle.Domain.Content;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 從 docs/research/wog45/data 匯入的正式內容：抽樣紀錄要等於原版值
public class WogImportTests
{
    private static readonly string ContentDirectory = Path.Combine(BackendPaths.BackendDirectory, "content");

    private static readonly JsonGameContent Content = JsonGameContent.Load(ContentDirectory);

    private static readonly string Gaps = File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/gaps.md"));

    [Fact]
    public void OfficialContent_Loads()
    {
        var content = JsonGameContent.Load(ContentDirectory);

        Assert.NotEmpty(content.AllItems);
    }

    [Fact]
    public void Items_KeepOriginalValues()
    {
        var cloth = Content.GetItem("item-1");

        Assert.Equal(
            ("布衣", ItemCategory.Body, 3, 1, 6L, 1, 3),
            (cloth.Name, cloth.Category, cloth.Bonus.Df, cloth.Bonus.Mdf, cloth.Price, cloth.Holes, cloth.RefineMax));
    }

    [Fact]
    public void Items_KeepVariantsSeparate()
    {
        var swords = new[] { Content.GetItem("item-660"), Content.GetItem("item-928") };

        Assert.All(swords, sword => Assert.Equal(("木刀", ItemCategory.Weapon), (sword.Name, sword.Category)));
    }

    [Fact]
    public void Jobs_KeepOriginalGrowth()
    {
        var warrior = Content.GetJob("job-6");

        Assert.Equal(("戰士", 1, (string?)null), (warrior.Name, warrior.Tier, warrior.ParentId));
        Assert.Equal(new StatRange(1, 3), warrior.Growth.Str);
        Assert.Equal(("job-6", 150, 3500), (Content.GetJob("job-10").ParentId!, Content.GetJob("job-10").UnlockLevel, Content.GetJob("job-10").ProficiencyRequired));
    }

    [Fact]
    public void Monsters_KeepOriginalValues()
    {
        var slime = Content.GetMonster("monster-1");

        Assert.Equal(("史萊姆", 2, 32, 5, 1), (slime.Name, slime.Level, slime.Hp, slime.Element, slime.PlaceId));
        Assert.Equal([1], slime.SubPlaces);
        Assert.Contains(new DropEntry("item-204", 90), slime.Drops);
    }

    [Fact]
    public void Monsters_LeaveOutInvalidRecords()
    {
        using var source = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "..", "docs/research/wog45/data/monsters.json")));
        var invalid = source.RootElement.EnumerateArray()
            .Where(monster => monster.GetProperty("level").GetInt32() < 1 || monster.GetProperty("hp").GetInt32() < 1)
            .Select(monster => monster.GetProperty("id").GetInt32())
            .ToList();

        Assert.NotEmpty(invalid);
        Assert.All(invalid, id =>
        {
            Assert.DoesNotContain(Content.AllMonsters, monster => monster.Id == $"monster-{id}");
            Assert.Contains($"| {id} |", Gaps);
        });
    }

    [Fact]
    public void Monsters_ListUnsupportedSkillsInGaps()
    {
        Assert.Matches(@"\| 1 \| 史萊姆 \| [^\n]*猛烈一擊[^\n]*爆氣 Lv2 rate 10", Gaps);
    }
}
