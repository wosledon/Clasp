using Clasp.Plugin;
using System.IO;
using Clasp.Plugin.Attributes;
using System.Text.Json;

namespace Clasp.Commands;

[ClaspCommand("json", Description = "增强JSON工具（格式化、查询、转义/去转义）")]
internal class JsonTool : ClaspCommand
{
    [ClaspOption("--file", "-f", Description = "JSON文件路径")]
    public string File { get; set; } = string.Empty;

    [ClaspOption("--input", "-i", Description = "JSON文本输入")]
    public string Input { get; set; } = string.Empty;

    [ClaspOption("--query", "-q", Description = "JSON查询（属性名）")]
    public string Query { get; set; } = string.Empty;

    [ClaspOption("--pretty", "-p", Description = "美化输出")]
    public bool Pretty { get; set; } = true;

    [ClaspOption("--escape", "-e", Description = "将输入转义为 JSON 字符串字面量")]
    public bool Escape { get; set; }

    [ClaspOption("--unescape", "-u", Description = "去除 JSON 字符串中的转义")]
    public bool Unescape { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Input))
            ValidationError("请提供 --file 或 --input 输入内容");

        if (Escape && Unescape)
            ValidationError("--escape 和 --unescape 不能同时使用");

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        string content = Input;
        if (!string.IsNullOrEmpty(File))
        {
            content = System.IO.File.ReadAllText(File);
        }

        if (Escape)
        {
            var escaped = JsonSerializer.Serialize(content);
            WriteLine(escaped, ClaspColorType.Cyan);
            return;
        }

        if (Unescape)
        {
            try
            {
                var unescaped = UnescapeJsonString(content);
                WriteLine(unescaped, ClaspColorType.Green);
            }
            catch (JsonException ex)
            {
                WriteLine("去转义失败: " + ex.Message, ClaspColorType.Red);
            }
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var element = doc.RootElement;

            if (!string.IsNullOrEmpty(Query))
            {
                if (element.TryGetProperty(Query, out var prop))
                {
                    WriteLine(prop.GetRawText(), ClaspColorType.Green);
                }
                else
                {
                    WriteLine("未找到属性: " + Query, ClaspColorType.Yellow);
                }
                return;
            }

            var options = new JsonSerializerOptions { WriteIndented = Pretty };
            var prettyJson = JsonSerializer.Serialize(element, options);
            WriteLine(prettyJson, ClaspColorType.Cyan);
        }
        catch (JsonException ex)
        {
            WriteLine("JSON解析失败: " + ex.Message, ClaspColorType.Red);
        }
    }

    private static string UnescapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // 如果输入本身就是带引号的 JSON 字符串，直接解析
        var trimmed = value.Trim();
        if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) || (trimmed.StartsWith("'") && trimmed.EndsWith("'")))
        {
            using var doc = JsonDocument.Parse(trimmed);
            return doc.RootElement.GetString() ?? string.Empty;
        }

        // 否则按原始字符串交给 JSON 序列化器转义后再解析
        var wrapped = JsonSerializer.Serialize(value);
        using var unwrapped = JsonDocument.Parse(wrapped);
        return unwrapped.RootElement.GetString() ?? string.Empty;
    }
}
