using System.Text.Json.Nodes;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class ContentFileFormatterTests
{
    private const string Canonical = """
        {
          "startingPlaceId": 1,
          "areas": [
            {"id": "central", "name": "中央大陸", "placeIds": [1, 3], "note": null},
            {"id": "empty", "tags": [], "bonus": {}, "rate": 1.10, "on": true}
          ]
        }

        """;

    [Fact]
    public void Format_WritesOneRecordPerLineInsideTopLevelArrays()
    {
        var messy = """
            {"startingPlaceId":1,"areas":[
              { "id" : "central", "name": "中央大陸", "placeIds": [
                1,
                3 ], "note": null },
              {"id":"empty","tags":[],"bonus":{},"rate":1.10,"on":true}]}
            """;

        Assert.Equal(Canonical, ContentFileFormatter.Format(JsonNode.Parse(messy)!));
    }

    [Fact]
    public void Format_LeavesCanonicalTextByteIdentical()
    {
        Assert.Equal(Canonical, ContentFileFormatter.Format(JsonNode.Parse(Canonical)!));
    }

    [Theory]
    [InlineData("items.json")]
    [InlineData("monsters.json")]
    [InlineData("jobs.json")]
    [InlineData("skills.json")]
    [InlineData("world.json")]
    [InlineData("missions.json")]
    [InlineData("recipes.json")]
    [InlineData("honour-shop.json")]
    [InlineData("mercenaries.json")]
    [InlineData("job-skills.json")]
    public void OfficialEditableContentFiles_AreInCanonicalFormat(string fileName)
    {
        var text = File.ReadAllText(Path.Combine(BackendPaths.BackendDirectory, "content", fileName));

        Assert.Equal(text, ContentFileFormatter.Format(JsonNode.Parse(text)!));
    }
}
