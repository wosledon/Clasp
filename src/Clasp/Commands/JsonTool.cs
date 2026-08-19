using System.Text.Json;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("json", Description = "格式化或校验 JSON")]
internal class JsonTool : ClaspCommand
{
    [ClaspOption("--compact", "-c", Description = "压缩为单行输出")]
    public bool Compact { get; set; }

    [ClaspOption("--input", "-i", Description = "要格式化的 JSON 文本或文件路径")]
    public string Input { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = Input;
        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            input = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(input))
        {
            ValidationError("请提供要格式化的 JSON 文本或文件路径");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Input) && Console.IsInputRedirected)
            Input = await ReadStandardInputAsync(cancellationToken);

        string json;
        if (File.Exists(Input))
        {
            json = await File.ReadAllTextAsync(Input, cancellationToken);
        }
        else
        {
            json = Input;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = !Compact });
            WriteLine(formatted);
        }
        catch (Exception ex)
        {
            WriteLine($"JSON 无效: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
