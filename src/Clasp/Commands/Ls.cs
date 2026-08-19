using System.IO;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("ls", Description = "列出当前目录文件")]
internal class ListFiles : ClaspCommand
{
    [ClaspOption("--dir", "-d", Description = "要列出的目录，默认为当前目录")]
    public string TargetDir { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var targetDir = string.IsNullOrWhiteSpace(TargetDir)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(TargetDir);

        if (!Directory.Exists(targetDir))
        {
            WriteLine($"目录不存在: {targetDir}", ClaspColorType.BrightRed);
            return;
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var targetDir = string.IsNullOrWhiteSpace(TargetDir)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(TargetDir);

        var entries = new List<(string Text, ClaspColorType? Color)>();
        try
        {
            foreach (var dir in Directory.GetDirectories(targetDir))
            {
                var name = Path.GetFileName(dir);
                entries.Add(($"{name}/", ClaspColorType.Cyan));
            }

            foreach (var file in Directory.GetFiles(targetDir))
            {
                var name = Path.GetFileName(file);
                entries.Add((name, null));
            }
        }
        catch (Exception ex)
        {
            WriteLine($"读取目录失败: {ex.Message}", ClaspColorType.BrightRed);
            return;
        }

        foreach (var (text, color) in entries)
        {
            if (color is null)
                WriteLine(text);
            else
                WriteLine(text, color.Value);
        }

        await Task.CompletedTask;
    }
}
