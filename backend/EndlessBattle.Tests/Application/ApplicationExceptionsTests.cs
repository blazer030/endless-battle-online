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
}
