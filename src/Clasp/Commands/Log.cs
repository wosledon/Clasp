using System.IO;
using System.Text.RegularExpressions;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("log", Description = "快速查看和过滤日志文件（支持 tail/head/filter/follow）")]
internal class Log : ClaspCommand
{
    [ClaspOption("--file", "-f", Description = "日志文件路径")]
    public string LogFile { get; set; } = string.Empty;

    [ClaspOption("--mode", "-m", Description = "操作模式：tail（默认）/ head / filter / follow")]
    public string Mode { get; set; } = "tail";

    [ClaspOption("--lines", "-n", Description = "显示行数（默认 100）")]
    public int Lines { get; set; } = 100;

    [ClaspOption("--pattern", "-p", Description = "过滤模式（支持正则）")]
    public string Pattern { get; set; } = string.Empty;

    [ClaspOption("--ignore-case", "-i", Description = "忽略大小写")]
    public bool IgnoreCase { get; set; }

    [ClaspOption("--follow", "-F", Description = "实时跟踪文件（类似 tail -f）")]
    public bool Follow { get; set; }

    [ClaspOption("--timeout", "-t", Description = "follow 模式超时秒数（默认 0=不限制）")]
    public int Timeout { get; set; }

    [ClaspOption("--level", "-l", Description = "按日志级别过滤，逗号分隔，如 ERROR,WARN,INFO")]
    public string Level { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(LogFile))
            ValidationError("请提供日志文件路径 (--file)");

        var path = Path.GetFullPath(LogFile);
        if (!File.Exists(path))
            ValidationError($"文件不存在: {path}");

        var mode = Mode.Trim().ToLowerInvariant();
        if (mode != "tail" && mode != "head" && mode != "filter" && mode != "follow")
            ValidationError("--mode 仅支持 tail / head / filter / follow");

        if (Follow && Timeout < 0)
            ValidationError("--timeout 不能为负数");

        if (!string.IsNullOrWhiteSpace(Pattern))
        {
            try
            {
                Regex.Match(string.Empty, Pattern, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            }
            catch
            {
                ValidationError($"无效的正则表达式: {Pattern}");
            }
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var path = Path.GetFullPath(LogFile);
        var mode = Mode.Trim().ToLowerInvariant();

        try
        {
            if (mode == "follow")
            {
                await FollowLogAsync(path, cancellationToken);
                return;
            }

            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            IEnumerable<string> result = lines;

            if (!string.IsNullOrWhiteSpace(Level))
            {
                var levels = Level.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(l => l.ToUpperInvariant())
                    .ToHashSet();

                result = result.Where(line =>
                {
                    var upper = line.ToUpperInvariant();
                    return levels.Any(lvl => upper.Contains(lvl));
                });
            }

            if (!string.IsNullOrWhiteSpace(Pattern))
            {
                var options = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                var regex = new Regex(Pattern, options);

                result = result.Where(line => regex.IsMatch(line));
            }

            var list = result.ToList();

            if (mode == "tail")
            {
                list = list.Skip(Math.Max(0, list.Count - Lines)).ToList();
            }
            else if (mode == "head")
            {
                list = list.Take(Lines).ToList();
            }

            foreach (var line in list)
                WriteLine(line);
        }
        catch (Exception ex)
        {
            WriteLine($"读取日志失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }

    private async Task FollowLogAsync(string path, CancellationToken cancellationToken)
    {
        WriteLine($"正在跟踪: {path}", ClaspColorType.Cyan);
        WriteLine("按 Ctrl+C 退出", ClaspColorType.Yellow);
        WriteLine(string.Empty);

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);

        var timeoutMs = Timeout > 0 ? Timeout * 1000 : -1;
        var startTime = Environment.TickCount64;

        // 先输出最后 Lines 行
        var allLines = await File.ReadAllLinesAsync(path, cancellationToken);
        var tailLines = allLines.Skip(Math.Max(0, allLines.Length - Lines)).ToList();
        foreach (var line in tailLines)
            WriteLine(line);

        var lastPosition = fs.Position;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            fs.Seek(lastPosition, SeekOrigin.Begin);
            var line = await sr.ReadLineAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(line))
            {
                lastPosition = fs.Position;
                WriteLine(line);
                continue;
            }

            if (timeoutMs > 0 && Environment.TickCount64 - startTime >= timeoutMs)
            {
                WriteLine(string.Empty);
                WriteLine("跟踪超时，已退出", ClaspColorType.Yellow);
                break;
            }

            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }
    }
}
