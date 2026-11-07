using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

// 內容檔讀成紀錄再寫回：每個檔案都要逐字相同
public static class CodecRoundTrip
{
    public static TheoryData<string> ContentDirectories => new()
    {
        Path.Combine(BackendPaths.BackendDirectory, "content"),
        Path.Combine(AppContext.BaseDirectory, "TestContent"),
    };

    public static Dictionary<string, string> ReadFiles<TRecord>(string directory, IContentCodec<TRecord> codec) =>
        codec.FileNames.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(directory, name)));

    public static void AssertByteIdentical<TRecord>(string directory, IContentCodec<TRecord> codec)
    {
        var files = ReadFiles(directory, codec);

        var written = codec.Write(codec.Read(files), files);

        Assert.Equal(files.Keys.Order(), written.Keys.Order());
        foreach (var (name, text) in files)
        {
            Assert.Equal(text, written[name]);
        }
    }
}
