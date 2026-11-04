using System.Runtime.CompilerServices;

namespace EndlessBattle.Tests.Support;

// 測試主機不監看設定檔變更：每個 WebApplicationFactory 都會替 appsettings*.json 開檔案監看，
// 大量主機同時啟動時，macOS 上偶爾有主機建到一半卡住，等滿 WebApplicationFactory 的 5 分鐘逾時。
// 這個值要在 Program 建立 WebApplicationBuilder 之前就存在，所以用環境變數，不能用 UseSetting
internal static class HostConfigurationSetup
{
    [ModuleInitializer]
    internal static void DisableConfigurationReload() =>
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
}
