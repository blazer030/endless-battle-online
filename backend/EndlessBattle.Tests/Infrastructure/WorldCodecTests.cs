using EndlessBattle.Infrastructure.Content;

namespace EndlessBattle.Tests.Infrastructure;

public class WorldCodecTests
{
    public static TheoryData<string> ContentDirectories => CodecRoundTrip.ContentDirectories;

    [Theory]
    [MemberData(nameof(ContentDirectories))]
    public void WorldCodec_RoundTripsContentFiles(string directory)
    {
        CodecRoundTrip.AssertByteIdentical(directory, new AreaCodec());
        CodecRoundTrip.AssertByteIdentical(directory, new PlaceCodec());
    }

    [Fact]
    public void WorldCodec_KeepsTheOtherArrayAndStartingPlaceWhenWriting()
    {
        var codec = new PlaceCodec();
        var files = CodecRoundTrip.ReadFiles(Path.Combine(AppContext.BaseDirectory, "TestContent"), codec);
        var places = codec.Read(files);

        var written = codec.Write([places[0] with { Name = "改名" }, .. places.Skip(1)], files)["world.json"];

        Assert.Contains("\"name\": \"改名\"", written);
        Assert.Contains("\"startingPlaceId\": 1", written);
        Assert.Contains("\"areas\": [", written);
    }
}
