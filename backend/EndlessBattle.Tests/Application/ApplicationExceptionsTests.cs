using EndlessBattle.Application.Common;

namespace EndlessBattle.Tests.Application;

// 錯誤回應一定要有原因代碼：沒帶代碼的例外在建立時就擋下
public class ApplicationExceptionsTests
{
    [Fact]
    public void InputValidation_EmptyReasons_Throws() =>
        Assert.Throws<ArgumentException>(() => new InputValidationException("資料不完整", []));

    [Fact]
    public void Conflict_EmptyReasons_Throws() =>
        Assert.Throws<ArgumentException>(() => new ConflictException("版本不符", []));

    [Fact]
    public void NotFound_BlankReason_Throws() =>
        Assert.Throws<ArgumentException>(() => new NotFoundException("尚未建立角色", " "));

    // 代碼格式寫錯（例如還是駝峰式）也在建立時擋下：大寫底線，冒號後的參數不限
    [Theory]
    [InlineData("bagFull")]
    [InlineData("Bag_Full")]
    [InlineData("missingTool:item-1")]
    public void InputValidation_CodeNotUpperSnake_Throws(string code) =>
        Assert.Throws<ArgumentException>(() => new InputValidationException("無法購買", [code]));

    [Fact]
    public void InputValidation_ParameterAfterColonKeepsItsSpelling() =>
        Assert.Equal(["MISSING_TOOL:item-1378"], new InputValidationException("缺少需要的道具", ["MISSING_TOOL:item-1378"]).Reasons);
}
