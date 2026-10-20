namespace Inkbound.Tests.Support;

public static class BackendPaths
{
    public static string BackendDirectory { get; } = FindBackendDirectory();

    private static string FindBackendDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Inkbound.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("找不到含 Inkbound.slnx 的 backend 資料夾");
    }
}
