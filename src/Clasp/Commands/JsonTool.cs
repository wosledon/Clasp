using Clasp.Plugin;
using System.IO;
using Clasp.Plugin.Attributes;
using System.Text.Json;

namespace Clasp.Commands;

[ClaspCommand("json", Description = "增强JSON工具（格式化、查询、转换）")]
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

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(File) && string.IsNullOrEmpty(Input))
            ValidationError("请提供 --file 或 --input 输入内容");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        string json = Input;
        if (!string.IsNullOrEmpty(File))
        {
            json = System.IO.File.ReadAllText(File);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
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
}
