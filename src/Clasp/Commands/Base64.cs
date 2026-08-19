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

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(TargetFile))
        {
            var path = Path.GetFullPath(TargetFile);
            if (!File.Exists(path))
            {
                WriteLine($"文件不存在: {path}", ClaspColorType.BrightRed);
                return;
            }
            input = await File.ReadAllTextAsync(path, cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
        {
            input = await ReadStandardInputAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHelp();
            return;
        }

        try
        {
            if (Decode)
                WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(input.Trim())));
            else
                WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(input)));
        }
        catch (Exception ex)
        {
            WriteLine($"{(Decode ? "解码" : "编码")}失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
