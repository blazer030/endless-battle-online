using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EndlessBattle.Infrastructure.Content;

// 內容檔的固定格式：兩格縮排、頂層陣列一筆一行，其餘寫成單行；數字與字串保留原文，發布的 diff 只出現改動的那幾筆
public static class ContentFileFormatter
{
    private static readonly JsonSerializerOptions ValueOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Format(JsonNode root)
    {
        if (root is not JsonObject properties)
        {
            throw new ArgumentException("內容檔的最外層必須是物件", nameof(root));
        }

        var text = new StringBuilder("{\n");
        var index = 0;
        foreach (var (name, value) in properties)
        {
            text.Append("  ").Append(Quote(name)).Append(": ");
            if (value is JsonArray records && records.Count > 0)
            {
                text.Append("[\n");
                for (var record = 0; record < records.Count; record++)
                {
                    text.Append("    ");
                    WriteInline(text, records[record]);
                    text.Append(record < records.Count - 1 ? ",\n" : "\n");
                }

                text.Append("  ]");
            }
            else
            {
                WriteInline(text, value);
            }

            text.Append(++index < properties.Count ? ",\n" : "\n");
        }

        return text.Append("}\n").ToString();
    }

    private static void WriteInline(StringBuilder text, JsonNode? node)
    {
        switch (node)
        {
            case null:
                text.Append("null");
                break;
            case JsonObject properties:
                text.Append('{');
                var first = true;
                foreach (var (name, value) in properties)
                {
                    text.Append(first ? "" : ", ").Append(Quote(name)).Append(": ");
                    WriteInline(text, value);
                    first = false;
                }

                text.Append('}');
                break;
            case JsonArray items:
                text.Append('[');
                for (var item = 0; item < items.Count; item++)
                {
                    text.Append(item == 0 ? "" : ", ");
                    WriteInline(text, items[item]);
                }

                text.Append(']');
                break;
            case JsonValue value when value.TryGetValue(out JsonElement element):
                // 從檔案讀進來的值保留原文（例如 1.10 不會變成 1.1）
                text.Append(element.GetRawText());
                break;
            default:
                text.Append(node.ToJsonString(ValueOptions));
                break;
        }
    }

    private static string Quote(string name) => JsonSerializer.Serialize(name, ValueOptions);
}
