using System.Text;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("b64", Description = "Base64 编码或解码")]
internal class Base64 : ClaspCommand
{
    [ClaspOption("--decode", "-d", Description = "解码模式")]
    public bool Decode { get; set; }

    [ClaspOption("--file", "-f", Description = "从文件读取内容")]
    public string TargetFile { get; set; } = string.Empty;

    [ClaspOption("--input", "-i", Description = "要编码或解码的文本")]
    public string Input { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = Input;
        if (!string.IsNullOrWhiteSpace(TargetFile))
        {
            var path = Path.GetFullPath(TargetFile);
            if (!File.Exists(path))
            {
                ValidationError($"文件不存在: {path}");
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            return;

        if (string.IsNullOrWhiteSpace(input))
        {
            ValidationError("请提供要编码或解码的文本");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(TargetFile))
        {
            var path = Path.GetFullPath(TargetFile);
            var input = await File.ReadAllTextAsync(path, cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(Input) && Console.IsInputRedirected)
        {
            Input = await ReadStandardInputAsync(cancellationToken);
        }

        try
        {
            if (Decode)
                WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(Input.Trim())));
            else
                WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(Input)));
        }
        catch (Exception ex)
        {
            WriteLine($"{(Decode ? "解码" : "编码")}失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
