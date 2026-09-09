using System.Diagnostics;
using System.IO;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("open", Description = "用系统默认程序打开目录、文件或 URL")]
internal class Open : ClaspCommand
{
    [ClaspOption("--path", "-p", Description = "要打开的路径（默认当前目录）")]
    public string TargetPath { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var target = ResolveTarget(args);
        if (string.IsNullOrWhiteSpace(target))
            return;

        if (IsUrl(target))
            return;

        var fullPath = System.IO.Path.GetFullPath(target);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            ValidationError($"路径不存在: {fullPath}");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var target = ResolveTarget(args);
        if (string.IsNullOrWhiteSpace(target))
            target = Environment.CurrentDirectory;

        var openTarget = IsUrl(target) ? target : System.IO.Path.GetFullPath(target);

        try
        {
            OpenWithDefaultApp(openTarget);
            WriteLine($"已打开: {openTarget}", ClaspColorType.Green);
        }
        catch (Exception ex)
        {
            WriteLine($"打开失败: {ex.Message}", ClaspColorType.BrightRed);
        }

        await Task.CompletedTask;
    }

    private string ResolveTarget(ClaspCommandArgs args)
    {
        if (!string.IsNullOrWhiteSpace(TargetPath))
            return TargetPath.Trim();

        return args.Values.FirstOrDefault() ?? string.Empty;
    }

    private static bool IsUrl(string target)
    {
        // 仅把 http/https 和显式 file:// 当作 URL；
        // Windows 盘符路径（C:\...）也会被 Uri 解析成 file scheme，需排除。
        if (target.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return true;

        return Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private void OpenWithDefaultApp(string target)
    {
        if (IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
            })?.Dispose();
            return;
        }

        var fileName = OperatingSystem.IsMacOS() ? "open" : "xdg-open";
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(target);

        using var process = Process.Start(startInfo);
        if (process is null)
            throw new InvalidOperationException($"无法启动进程: {fileName}");
    }
}
