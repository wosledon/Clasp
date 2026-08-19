using System.Diagnostics;
using System.Net.Http.Headers;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("speed", Description = "网络下载测速")]
internal class Speed : ClaspCommand
{
    [ClaspOption("--seconds", "-t", Description = "测速时长秒 (默认 10)")]
    public int Seconds { get; set; } = 10;

    [ClaspOption("--threads", "-n", Description = "并发连接数 (默认 4)")]
    public int Threads { get; set; } = 4;

    [ClaspOption("--size", "-s", Description = "下载上限 MB (默认 0 = 不限)")]
    public int Size { get; set; }

    [ClaspOption("--url", "-u", Description = "测速地址")]
    public string Url { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Url) || !Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            ValidationError("请提供有效的 HTTP(S) 地址");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var seconds = Math.Clamp(Seconds, 1, 300);
        var threads = Math.Clamp(Threads, 1, 64);
        var capBytes = Size <= 0 ? long.MaxValue : Size * 1048576L;

        var rangeSupported = await SupportsRangeAsync(Url);
        var workerCount = rangeSupported ? threads : 1;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(seconds));

        long totalBytes = 0;
        var failures = 0;
        var nextOffset = 0L;
        var segment = 1048576L;

        var sw = Stopwatch.StartNew();
        var workers = new List<Task>(workerCount);
        for (var i = 0; i < workerCount; i++)
        {
            workers.Add(Task.Run(async () =>
            {
                var buffer = new byte[64 * 1024];
                while (!cts.IsCancellationRequested && Interlocked.Read(ref totalBytes) < capBytes)
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                        using var request = new HttpRequestMessage(HttpMethod.Get, Url);
                        if (rangeSupported)
                        {
                            var start = Interlocked.Add(ref nextOffset, segment) - segment;
                            request.Headers.Range = new RangeHeaderValue(start, null);
                        }

                        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        response.EnsureSuccessStatusCode();
                        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);

                        int read;
                        while (!cts.IsCancellationRequested && (read = await stream.ReadAsync(buffer, cts.Token)) > 0)
                        {
                            Interlocked.Add(ref totalBytes, read);
                            if (Interlocked.Read(ref totalBytes) >= capBytes)
                                return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch
                    {
                        Interlocked.Increment(ref failures);
                    }
                }
            }, cts.Token));
        }

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
        }

        sw.Stop();
        var elapsed = Math.Max(sw.Elapsed.TotalSeconds, 0.001);
        var mb = Interlocked.Read(ref totalBytes) / 1048576.0;

        WriteLine($"下载: {mb:N2} MB  用时: {elapsed:N1} 秒  平均速度: {mb / elapsed:N2} MB/s", ClaspColorType.BrightGreen);
        if (failures > 0)
            WriteLine($"{failures} 个连接失败", ClaspColorType.Yellow);
    }

    private static async Task<bool> SupportsRangeAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response.Headers.AcceptRanges?.Contains("bytes") ?? false;
        }
        catch
        {
            return false;
        }
    }
}
