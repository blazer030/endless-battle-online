using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class MonsterCodecTests
{
    private static Dictionary<string, string> ReadFiles(string directory, IContentCodec<EndlessBattle.Application.Admin.Content.MonsterRecord> codec) =>
        codec.FileNames.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(directory, name)));

    public static TheoryData<string> ContentDirectories => new()
    {
        Path.Combine(BackendPaths.BackendDirectory, "content"),
        Path.Combine(AppContext.BaseDirectory, "TestContent"),
    };

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void MonsterCodec_RoundTripsContentFiles(string directory)
    {
        var codec = new MonsterCodec();
        var files = ReadFiles(directory, codec);

        var written = codec.Write(codec.Read(files), files);

        Assert.Equal(files["monsters.json"], written["monsters.json"]);
        Assert.Equal(files["monster-skills.json"], written["monster-skills.json"]);
    }

    [Fact]
    public void MonsterCodec_UnknownFieldFailsAndNamesTheFile()
    {
        var codec = new MonsterCodec();
        var files = ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monsters.json"] = files["monsters.json"].Replace("\"id\": \"test-sprite\"", "\"rarity\": 3, \"id\": \"test-sprite\"", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("monsters.json", exception.Message);
        Assert.Contains("rarity", exception.Message);
    }

    [Fact]
    public void MonsterCodec_SkillRowOfUnknownMonsterFailsInsteadOfBeingDropped()
    {
        var codec = new MonsterCodec();
        var files = ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monster-skills.json"] = files["monster-skills.json"].Replace("test-wraith", "no-such-monster", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("monster-skills.json", exception.Message);
        Assert.Contains("no-such-monster", exception.Message);
    }

    [Fact]
    public void MonsterCodec_InvalidElementFailsAndNamesTheMonster()
    {
        var codec = new MonsterCodec();
        var files = ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monsters.json"] = files["monsters.json"].Replace("\"element\": 2", "\"element\": 9", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("test-sprite", exception.Message);
    }
}
