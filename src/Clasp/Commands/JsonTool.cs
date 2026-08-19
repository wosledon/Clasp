using System.Text.Json;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("json", Description = "格式化或校验 JSON")]
internal class JsonTool : ClaspCommand
{
    [ClaspOption("--compact", "-c", Description = "压缩为单行输出")]
    public bool Compact { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            input = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHelp();
            return;
        }

        string json;
        if (File.Exists(input))
        {
            json = await File.ReadAllTextAsync(input, cancellationToken);
        }
        else
        {
            json = input;
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
