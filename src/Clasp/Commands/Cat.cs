using System.IO;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("cat", Description = "读取并输出文件内容")]
internal class Cat : ClaspCommand
{
    [ClaspOption("--file", "-f", Description = "要读取的文件路径")]
    public string TargetFile { get; set; } = string.Empty;

    [ClaspOption("--lines", "-n", Description = "显示行号")]
    public bool ShowLineNumbers { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(TargetFile))
        {
            WriteLine("请提供要读取的文件路径", ClaspColorType.BrightRed);
            ShowHelp();
            return;
        }

        var path = Path.GetFullPath(TargetFile);

        if (!File.Exists(path))
        {
            WriteLine($"文件不存在: {path}", ClaspColorType.BrightRed);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            for (var i = 0; i < lines.Length; i++)
            {
                if (ShowLineNumbers)
                    WriteLine($"{i + 1,4}: {lines[i]}");
                else
                    WriteLine(lines[i]);
            }
        }
        catch (Exception ex)
        {
            WriteLine($"读取文件失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
