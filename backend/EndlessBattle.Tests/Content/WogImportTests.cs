using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Content;

// 釘住 scripts/import-wog45.py 從 docs/research/wog45/data 匯入的正式內容：抽樣紀錄要等於原版值
public class WogImportTests
{
    private static readonly string ContentDirectory = Path.Combine(BackendPaths.BackendDirectory, "content");

    private static readonly JsonGameContent Content = JsonGameContent.Load(ContentDirectory);

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
}
