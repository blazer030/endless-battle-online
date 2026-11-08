using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Infrastructure;

public class ItemCodecTests
{
    public static TheoryData<string> ContentDirectories => CodecRoundTrip.ContentDirectories;

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void ItemCodec_RoundTripsContentFiles(string directory) =>
        CodecRoundTrip.AssertByteIdentical(directory, new ItemCodec());

    [Fact]
    public void ItemCodec_UnknownFieldFailsAndNamesTheFile()
    {
        var codec = new ItemCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        files["items.json"] = files["items.json"].Replace("\"id\": \"test-sword\"", "\"rarity\": 3, \"id\": \"test-sword\"", StringComparison.Ordinal);

        var exception = Assert.Throws<GameContentException>(() => codec.Read(files));

        Assert.Contains("items.json", exception.Message);
        Assert.Contains("rarity", exception.Message);
    }
}
