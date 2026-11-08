using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Infrastructure;

public class JobCodecTests
{
    public static TheoryData<string> ContentDirectories => CodecRoundTrip.ContentDirectories;

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void JobCodec_RoundTripsContentFiles(string directory) =>
        CodecRoundTrip.AssertByteIdentical(directory, new JobCodec());

    [Fact]
    public void JobCodec_UnknownStatFailsAndNamesTheJob()
    {
        var codec = new JobCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        var text = files["jobs.json"];
        var withRequirement = text.Replace("\"statRequirements\": []", "\"statRequirements\": [{\"stat\": \"luck\", \"minimum\": 1}]", StringComparison.Ordinal);
        Assert.NotEqual(text, withRequirement);
        files["jobs.json"] = withRequirement;

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("luck", exception.Message);
    }
}
