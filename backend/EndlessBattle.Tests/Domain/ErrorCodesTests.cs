using System.Reflection;
using EndlessBattle.Domain;

namespace EndlessBattle.Tests.Domain;

// 後端的原因代碼都集中在 ErrorCodes：每個常數都要合格式、不能重複
public class ErrorCodesTests
{
    private static readonly string[] Codes = typeof(ErrorCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    [Fact]
    public void EveryCodeIsWellFormed() =>
        Assert.All(Codes, code => Assert.True(ErrorCodes.IsWellFormed(code), code));

    [Fact]
    public void CodesAreDistinct() =>
        Assert.Empty(Codes.GroupBy(code => code).Where(group => group.Count() > 1).Select(group => group.Key));

    [Fact]
    public void StatTooLow_NamesTheStat() =>
        Assert.Equal("STAT_TOO_LOW:strength", ErrorCodes.StatTooLow("strength"));

    [Fact]
    public void MissingTool_NamesTheItem() =>
        Assert.Equal("MISSING_TOOL:item-1378", ErrorCodes.MissingTool("item-1378"));

    [Theory]
    [InlineData("BAG_FULL", true)]
    [InlineData("FULL", true)]
    [InlineData("STAT_TOO_LOW:strength", true)]
    [InlineData("bagFull", false)]
    [InlineData("Bag_Full", false)]
    [InlineData("missingTool:item-1", false)]
    [InlineData("", false)]
    public void IsWellFormed_AcceptsOnlyUpperSnake(string code, bool expected) =>
        Assert.Equal(expected, ErrorCodes.IsWellFormed(code));
}
