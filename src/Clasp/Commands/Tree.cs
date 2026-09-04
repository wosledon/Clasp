using System.IO;
using System.Text;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("tree", Description = "以树形结构显示目录内容")]
internal class Tree : ClaspCommand
{
    [ClaspOption("--depth", "-d", Description = "最大递归深度（默认 3）")]
    public int Depth { get; set; } = 3;

    [ClaspOption("--dirs-only", Description = "只显示目录")]
    public bool DirsOnly { get; set; }

    [ClaspOption("--hidden", Description = "显示隐藏文件")]
    public bool ShowHidden { get; set; }

    [ClaspOption("--path", "-p", Description = "要显示的目录路径（默认当前目录）")]
    public string Path { get; set; } = ".";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Depth < 0)
            ValidationError("--depth 不能为负数");

        var fullPath = System.IO.Path.GetFullPath(Path);
        if (!Directory.Exists(fullPath))
            ValidationError($"目录不存在: {fullPath}");

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var root = System.IO.Path.GetFullPath(Path);
        WriteLine(root, ClaspColorType.Cyan);
        await PrintTreeAsync(root, string.Empty, 0, cancellationToken);
    }

    private async Task PrintTreeAsync(string directory, string prefix, int currentDepth, CancellationToken cancellationToken)
    {
        if (currentDepth > Depth)
            return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(directory);
        }
        catch
        {
            return;
        }

        var items = entries
            .Where(e => ShowHidden || !IsHidden(e))
            .Select(e => new
            {
                Name = System.IO.Path.GetFileName(e),
                FullPath = e,
                IsDir = Directory.Exists(e),
            })
            .OrderByDescending(e => e.IsDir)
            .ThenBy(e => e.Name)
            .ToList();

        if (DirsOnly)
            items = items.Where(e => e.IsDir).ToList();

        var total = items.Count;
        for (var i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = items[i];
            var isLast = i == total - 1;
            var connector = isLast ? "└── " : "├── ";
            var nextPrefix = isLast ? "    " : "│   ";

            if (item.IsDir)
            {
                WriteLine($"{prefix}{connector}{item.Name}/", ClaspColorType.Cyan);
                await PrintTreeAsync(item.FullPath, prefix + nextPrefix, currentDepth + 1, cancellationToken);
            }
            else
            {
                WriteLine($"{prefix}{connector}{item.Name}");
            }
        }

        await Task.CompletedTask;
    }

    private static bool IsHidden(string path)
    {
        try
        {
            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Hidden) || attr.HasFlag(FileAttributes.System);
        }
        catch
        {
            return false;
        }
    }
}
