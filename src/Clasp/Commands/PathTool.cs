using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("path", Description = "路径处理工具（拼接、规范化、获取信息）")]
internal class PathTool : ClaspCommand
{
    [ClaspOption("--join", "-j", Description = "拼接多个路径")]
    public string Join { get; set; } = string.Empty;

    [ClaspOption("--full", "-f", Description = "获取绝对路径")]
    public string Full { get; set; } = string.Empty;

    [ClaspOption("--dir", "-d", Description = "获取路径所在目录")]
    public string Dir { get; set; } = string.Empty;

    [ClaspOption("--name", Description = "获取文件名（含扩展名）")]
    public string Name { get; set; } = string.Empty;

    [ClaspOption("--sep", Description = "显示路径分隔符")]
    public bool ShowSep { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var hasOperation = !string.IsNullOrEmpty(Join) || !string.IsNullOrEmpty(Full) ||
                          !string.IsNullOrEmpty(Dir) || !string.IsNullOrEmpty(Name) || ShowSep;
        if (!hasOperation)
            ValidationError("请提供至少一个操作");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (ShowSep)
        {
            WriteLine($"路径分隔符: {Path.DirectorySeparatorChar}", ClaspColorType.Cyan);
            WriteLine($"备用分隔符: {Path.AltDirectorySeparatorChar}", ClaspColorType.Cyan);
            WriteLine($"卷标分隔符: {Path.VolumeSeparatorChar}", ClaspColorType.Cyan);
            WriteLine($"路径分隔符(通用): {Path.PathSeparator}", ClaspColorType.Cyan);
            return;
        }

        if (!string.IsNullOrEmpty(Full))
        {
            var fullPath = Path.GetFullPath(Full);
            WriteLine(fullPath, ClaspColorType.Cyan);
            return;
        }

        if (!string.IsNullOrEmpty(Join))
        {
            var parts = Join.Split(' ');
            if (parts.Length < 2)
                ValidationError("--join 需要至少两个路径，用空格分隔");
            var result = Path.Combine(parts);
            WriteLine(result, ClaspColorType.Cyan);
            return;
        }

        if (!string.IsNullOrEmpty(Dir))
        {
            var dir = Path.GetDirectoryName(Dir);
            WriteLine(dir ?? Environment.CurrentDirectory, ClaspColorType.Cyan);
            return;
        }

        if (!string.IsNullOrEmpty(Name))
        {
            var name = Path.GetFileName(Name);
            WriteLine(name, ClaspColorType.Cyan);
            return;
        }
    }
}
