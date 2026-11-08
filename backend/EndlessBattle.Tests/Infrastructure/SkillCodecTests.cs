using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Infrastructure;

public class SkillCodecTests
{
    public static TheoryData<string> ContentDirectories => CodecRoundTrip.ContentDirectories;

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void SkillCodec_RoundTripsContentFiles(string directory) =>
        CodecRoundTrip.AssertByteIdentical(directory, new SkillCodec());

    [Fact]
    public void SkillCodec_UnknownEffectFieldFailsAndNamesTheFile()
    {
        var codec = new SkillCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["skills.json"] = files["skills.json"].Replace("\"kind\": \"StatChange\",", "\"kind\": \"StatChange\", \"power\": 3,", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("skills.json", exception.Message);
        Assert.Contains("power", exception.Message);
    }

    [Fact]
    public void SkillCodec_JobRowOfUnknownSkillFailsInsteadOfBeingDropped()
    {
        var codec = new SkillCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["job-skills.json"] = files["job-skills.json"].Replace("\"skillId\": \"burst\"", "\"skillId\": \"no-such-skill\"", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("job-skills.json", exception.Message);
    }
}
