using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System.Text.RegularExpressions;

namespace Clasp.Commands;

[ClaspCommand("grep", Description = "在文件中搜索文本模式（支持正则）")]
internal class Grep : ClaspCommand
{
    [ClaspOption("--pattern", "-p", Description = "要搜索的正则表达式模式（必填）")]
    public string Pattern { get; set; } = string.Empty;

    [ClaspOption("--path", Description = "要搜索的文件或目录路径（默认当前目录）")]
    public string Path { get; set; } = ".";

    [ClaspOption("--ignore-case", "-i", Description = "忽略大小写")]
    public bool IgnoreCase { get; set; }

    [ClaspOption("--recursive", "-r", Description = "递归搜索子目录")]
    public bool Recursive { get; set; } = true;

    [ClaspOption("--line-number", "-n", Description = "显示行号")]
    public bool LineNumber { get; set; } = true;

    [ClaspOption("--count", "-c", Description = "只显示匹配的行数")]
    public bool CountOnly { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Pattern))
            ValidationError("请提供 --pattern 搜索模式");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"搜索模式: {Pattern}", ClaspColorType.Cyan);
        WriteLine($"路径: {Path}", ClaspColorType.Default);
        WriteLine("", ClaspColorType.Default);
        WriteLine("注意: 此命令需要进一步实现完整功能", ClaspColorType.Yellow);
    }
}
