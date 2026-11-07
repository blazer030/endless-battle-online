using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Infrastructure;

public class MonsterCodecTests
{
    public static TheoryData<string> ContentDirectories => CodecRoundTrip.ContentDirectories;

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void MonsterCodec_RoundTripsContentFiles(string directory) =>
        CodecRoundTrip.AssertByteIdentical(directory, new MonsterCodec());

    [Fact]
    public void MonsterCodec_UnknownFieldFailsAndNamesTheFile()
    {
        var codec = new MonsterCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monsters.json"] = files["monsters.json"].Replace("\"id\": \"test-sprite\"", "\"rarity\": 3, \"id\": \"test-sprite\"", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("monsters.json", exception.Message);
        Assert.Contains("rarity", exception.Message);
    }

    [Fact]
    public void MonsterCodec_SkillRowOfUnknownMonsterFailsInsteadOfBeingDropped()
    {
        var codec = new MonsterCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monster-skills.json"] = files["monster-skills.json"].Replace("test-wraith", "no-such-monster", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("monster-skills.json", exception.Message);
        Assert.Contains("no-such-monster", exception.Message);
    }

    [Fact]
    public void MonsterCodec_InvalidElementFailsAndNamesTheMonster()
    {
        var codec = new MonsterCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["monsters.json"] = files["monsters.json"].Replace("\"element\": 2", "\"element\": 9", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("test-sprite", exception.Message);
    }
}
