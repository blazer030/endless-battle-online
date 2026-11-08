using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Api.Endpoints.Admin;

// 嚴格讀取內容紀錄的本體：缺欄位、多欄位、型別不對、列舉不認識都回 400 並指出欄位，不能寫進草稿
public readonly record struct ContentRecordBody<TRecord>(TRecord? Record, ContentFieldError? Error);

public static partial class ContentRecordBody
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        NumberHandling = JsonNumberHandling.Strict,
        // 表單送出時 kind 不一定在第一個欄位
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    public static async Task<ContentRecordBody<TRecord>> ReadAsync<TRecord>(HttpRequest request, CancellationToken cancellationToken)
        where TRecord : class
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);
            if (FindNullInList(document.RootElement, "") is { } nullEntry)
            {
                return new(null, new ContentFieldError(nullEntry, "列表裡不能有 null"));
            }

            var record = document.Deserialize<TRecord>(Options);
            return record is null
                ? new(null, new ContentFieldError("", "本體不可為 null"))
                : new(record, null);
        }
        catch (JsonException exception)
        {
            return new(null, new ContentFieldError(FieldOf(exception), exception.Message));
        }
        catch (NotSupportedException exception)
        {
            // 條件、消耗、效果少了 kind 時，序列化器不知道要建立哪一種，丟的不是 JsonException
            return new(null, new ContentFieldError(PathInMessage().Match(exception.Message).Groups["path"].Value, "缺少 kind"));
        }
    }

    // RespectNullableAnnotations 不檢查列表的元素，而紀錄的列表都不允許 null
    private static string? FindNullInList(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var found = FindNullInList(property.Value, path.Length == 0 ? property.Name : $"{path}.{property.Name}");
                    if (found is not null)
                    {
                        return found;
                    }
                }

                return null;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemPath = $"{path}[{index++}]";
                    if (item.ValueKind == JsonValueKind.Null)
                    {
                        return itemPath;
                    }

                    var found = FindNullInList(item, itemPath);
                    if (found is not null)
                    {
                        return found;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    // exception.Path 像 $.drops[0]；缺必填欄位時欄位名稱只在訊息裡
    private static string FieldOf(JsonException exception)
    {
        var path = (exception.Path ?? "$").TrimStart('$').TrimStart('.');
        var named = NamedInMessage().Match(exception.Message);
        if (!named.Success)
        {
            return path;
        }

        var field = named.Groups["field"].Value;
        var alreadyInPath = path == field || path.EndsWith($".{field}", StringComparison.Ordinal);
        return path.Length == 0 ? field : alreadyInPath ? path : $"{path}.{field}";
    }

    [GeneratedRegex(@"Path: \$\.?(?<path>\S*)")]
    private static partial Regex PathInMessage();

    [GeneratedRegex("(?:required properties including: |JSON property )'(?<field>[^']+)'")]
    private static partial Regex NamedInMessage();
}
