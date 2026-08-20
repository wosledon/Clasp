using System.Net.Sockets;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("scan", Description = "扫描主机的开放端口")]
internal class Scan : ClaspCommand
{
    [ClaspOption("--host", Description = "目标主机")]
    public string Host { get; set; } = string.Empty;

    [ClaspOption("--range", "-r", Description = "端口范围，如 80-1000 或 22,80,443")]
    public string Range { get; set; } = string.Empty;

    [ClaspOption("--timeout", "-t", Description = "超时毫秒 (默认 1000)")]
    public int Timeout { get; set; } = 1000;

    [ClaspOption("--concurrency", "-c", Description = "并发数 (默认 100)")]
    public int Concurrency { get; set; } = 100;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Host))
            ValidationError("请提供目标主机 (--host)");

        if (string.IsNullOrWhiteSpace(Range))
            ValidationError("请提供端口范围 (--range)，如 80-1000 或 22,80,443");

        var ports = ParsePorts(Range);
        if (ports.Length == 0)
            ValidationError("无法解析端口范围，请使用格式如 80-1000 或 22,80,443");

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var ports = ParsePorts(Range);
        var host = Host.Trim();
        var timeout = Math.Clamp(Timeout, 100, 30000);
        var concurrency = Math.Clamp(Concurrency, 1, 500);
        var total = ports.Length;
        var scanned = 0;
        var openPorts = new List<int>();
        var semaphore = new SemaphoreSlim(concurrency);
        var scanTasks = new List<Task>();

        WriteLine($"正在扫描 {host} ({total} 个端口, 并发 {concurrency})...", ClaspColorType.Cyan);

        foreach (var port in ports)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await semaphore.WaitAsync(cancellationToken);

            scanTasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (await IsPortOpenAsync(host, port, timeout, cancellationToken))
                    {
                        lock (openPorts)
                            openPorts.Add(port);

                        WriteLine($"端口 {port} 开放", ClaspColorType.Green);
                    }
                }
                catch
                {
                    // ignore scan errors for individual ports
                }
                finally
                {
                    semaphore.Release();
                    Interlocked.Increment(ref scanned);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(scanTasks);

        WriteLine(string.Empty);
        if (openPorts.Count > 0)
        {
            WriteLine($"扫描完成: {total} 个端口, 发现 {openPorts.Count} 个开放端口: {string.Join(", ", openPorts.OrderBy(p => p))}", ClaspColorType.BrightGreen);
        }
        else
        {
            WriteLine($"扫描完成: {total} 个端口, 未发现开放端口", ClaspColorType.Yellow);
        }
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port, cancellationToken).AsTask();
            var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

            var completed = await Task.WhenAny(connectTask, timeoutTask);
            if (completed == timeoutTask)
                return false;

            if (connectTask.IsFaulted)
                return false;

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static int[] ParsePorts(string range)
    {
        var ports = new List<int>();
        foreach (var part in range.Split(','))
        {
            var trimmed = part.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Contains('-'))
            {
                var parts = trimmed.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out var start) &&
                    int.TryParse(parts[1].Trim(), out var end) &&
                    start >= 1 && end <= 65535 && start <= end)
                {
                    for (var p = start; p <= end; p++)
                        ports.Add(p);
                }
            }
            else if (int.TryParse(trimmed, out var single) && single >= 1 && single <= 65535)
            {
                ports.Add(single);
            }
        }

        return ports.Distinct().OrderBy(p => p).ToArray();
    }
}