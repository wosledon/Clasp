using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("watch", Description = "监听文件/目录变化并执行命令")]
internal class Watch : ClaspCommand
{
    [ClaspOption("--path", "-p", Description = "要监听的文件或目录路径（默认当前目录）")]
    public string Path { get; set; } = ".";

    [ClaspOption("--command", "-c", Description = "变化时要执行的命令（必填）")]
    public string Command { get; set; } = string.Empty;

    [ClaspOption("--interval", "-i", Description = "轮询间隔（秒，默认2）")]
    public int Interval { get; set; } = 2;

    [ClaspOption("--once", Description = "只执行一次命令")]
    public bool Once { get; set; }

    [ClaspOption("--verbose", "-v", Description = "显示详细信息")]
    public bool Verbose { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Command))
            ValidationError("请提供 --command 要执行的命令");
        if (Interval < 1)
            ValidationError("--interval 必须 >= 1");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"正在监听: {Path}", ClaspColorType.Cyan);
        WriteLine($"执行命令: {Command}", ClaspColorType.Yellow);
        WriteLine($"轮询间隔: {Interval} 秒", ClaspColorType.White);
        WriteLine("按 Ctrl+C 停止监听", ClaspColorType.White);
        WriteLine("", ClaspColorType.Default);

        await ExecuteCommandAsync(cancellationToken);

        if (Once)
            return;

        var lastSnapshot = GetSnapshot(Path);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Interval), cancellationToken);
                var currentSnapshot = GetSnapshot(Path);
                if (!SnapshotsEqual(lastSnapshot, currentSnapshot))
                {
                    if (Verbose)
                        WriteLine($"[{DateTime.Now:HH:mm:ss}] 检测到变化", ClaspColorType.Yellow);
                    await ExecuteCommandAsync(cancellationToken);
                    lastSnapshot = currentSnapshot;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                WriteLine($"错误: {ex.Message}", ClaspColorType.Red);
            }
        }
    }

    private async Task ExecuteCommandAsync(CancellationToken cancellationToken)
    {
        WriteLine($"[{DateTime.Now:HH:mm:ss}] 执行: {Command}", ClaspColorType.Cyan);
        try
        {
            var result = await CmdAsync("cmd.exe", $"/c {Command}", cancellationToken: cancellationToken);
            if (!string.IsNullOrEmpty(result.StandardOutput))
                Write(result.StandardOutput);
            if (result.ExitCode != 0 && !string.IsNullOrEmpty(result.StandardError))
                WriteLine(result.StandardError, ClaspColorType.Red);
        }
        catch (Exception ex)
        {
            WriteLine($"执行失败: {ex.Message}", ClaspColorType.Red);
        }
    }

    private Dictionary<string, DateTime> GetSnapshot(string path)
    {
        var snapshot = new Dictionary<string, DateTime>();
        if (File.Exists(path))
        {
            snapshot[path] = File.GetLastWriteTimeUtc(path);
            return snapshot;
        }

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { snapshot[file] = File.GetLastWriteTimeUtc(file); } catch { }
            }
        }

        return snapshot;
    }

    private bool SnapshotsEqual(Dictionary<string, DateTime> oldSnapshot, Dictionary<string, DateTime> newSnapshot)
    {
        if (oldSnapshot.Count != newSnapshot.Count) return false;
        foreach (var (path, time) in oldSnapshot)
        {
            if (!newSnapshot.TryGetValue(path, out var newTime)) return false;
            if (time != newTime) return false;
        }
        return true;
    }
}
