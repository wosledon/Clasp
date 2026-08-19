using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("file-download", "fd", Description = "多线程下载文件")]
internal class FileDownload : ClaspCommand
{
    [ClaspOption("--url", "-u", Description = "下载地址")]
    public string Url { get; set; } = string.Empty;

    [ClaspOption("--output", "-o", Description = "保存路径")]
    public string Output { get; set; } = string.Empty;

    [ClaspOption("--threads", "-t", Description = "线程数")]
    public int Threads { get; set; } = 4;

    [ClaspOption("--stdin", Description = "从标准输入读取 URL")]
    public bool ReadUrlFromStdin { get; set; }

    [ClaspOption("--file", "-f", Description = "从文件读取 URL")]
    public string UrlFile { get; set; } = string.Empty;

    [ClaspOption("--user-agent", Description = "User-Agent")]
    public string UserAgent { get; set; } = string.Empty;

    [ClaspOption("--referer", Description = "Referer")]
    public string Referer { get; set; } = string.Empty;

    [ClaspOption("--header", "-H", Description = "自定义请求头，多行格式为 Key: Value")]
    public string Headers { get; set; } = string.Empty;

    [ClaspOption("--cookie", Description = "Cookie 字符串，例如 \"key=value; key2=value2\"")]
    public string Cookie { get; set; } = string.Empty;

    [ClaspOption("--no-head", Description = "跳过 HEAD 探测，直接下载")]
    public bool NoHead { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(UrlFile))
        {
            if (!File.Exists(UrlFile))
            {
                ValidationError($"URL 文件不存在: {UrlFile}");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(Url))
        {
            ValidationError("请提供下载地址");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(UrlFile))
        {
            Url = (await File.ReadAllTextAsync(UrlFile, cancellationToken)).Trim();
        }
        else if (ReadUrlFromStdin)
        {
            Url = (await Console.In.ReadToEndAsync(cancellationToken)).Trim();
        }

        var fileName = string.IsNullOrWhiteSpace(Output)
            ? GetInferredFileName(Url)
            : Output;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            WriteLine("无法推断输出文件名，请使用 --output 指定", ClaspColorType.BrightRed);
            return;
        }

        if (Threads < 1)
            Threads = 1;

        var tempFile = fileName + ".tmp";

        using var http = new HttpClient();

        if (string.IsNullOrWhiteSpace(UserAgent))
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        long? totalSize = null;
        var supportsRange = false;

        if (!NoHead)
        {
            try
            {
                using var head = new HttpRequestMessage(HttpMethod.Head, Url);
                ApplyHeaders(head);
                using var response = await http.SendAsync(head, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    totalSize = response.Content.Headers.ContentLength;
                    supportsRange = response.Headers.AcceptRanges.Contains("bytes");

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        var contentDisposition = response.Content.Headers.ContentDisposition;
                        if (contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileNameStar))
                            fileName = contentDisposition.FileNameStar;
                        else if (contentDisposition != null && !string.IsNullOrWhiteSpace(contentDisposition.FileName))
                            fileName = contentDisposition.FileName;
                    }
                }
            }
            catch
            {
                // HEAD 失败则继续尝试流式下载
            }
        }

        try
        {
            if (totalSize is > 0 && supportsRange && Threads > 1)
            {
                await DownloadMultiThreadAsync(http, Url, tempFile, totalSize.Value, Threads, supportsRange, cancellationToken);
            }
            else
            {
                await DownloadSingleThreadAsync(http, Url, tempFile, totalSize ?? 0, supportsRange, cancellationToken);
            }

            File.Move(tempFile, fileName, overwrite: true);
            WriteLine($"下载完成: {fileName}", ClaspColorType.Green);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
        catch (Exception ex)
        {
            WriteLine($"下载失败: {ex.Message}", ClaspColorType.BrightRed);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private async Task DownloadSingleThreadAsync(HttpClient http, string url, string output, long totalSize, bool supportsRange, CancellationToken ct)
    {
        var existingSize = 0L;
        if (File.Exists(output))
        {
            existingSize = new FileInfo(output).Length;
            if (totalSize > 0 && existingSize >= totalSize)
            {
                WriteLine("文件已完整，跳过下载", ClaspColorType.Yellow);
                return;
            }

            if (existingSize > 0 && !supportsRange)
            {
                WriteLine("服务器不支持断点续传，重新下载", ClaspColorType.Yellow);
                File.Delete(output);
                existingSize = 0;
            }
            else if (existingSize > 0)
            {
                WriteLine($"断点续传: {FormatSize(existingSize)}", ClaspColorType.Yellow);
            }
        }

        ct.ThrowIfCancellationRequested();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyHeaders(request);
                if (existingSize > 0)
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, null);

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await using var file = new FileStream(output, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                if (existingSize > 0)
                    file.Seek(existingSize, SeekOrigin.Begin);

                await stream.CopyToAsync(file, ct);
                return;
            }
            catch when (attempt < 2)
            {
                WriteLine($"下载中断，{Math.Pow(2, attempt)} 秒后重试...", ClaspColorType.Yellow);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }

    private async Task DownloadMultiThreadAsync(HttpClient http, string url, string output, long totalSize, int threads, bool supportsRange, CancellationToken ct)
    {
        var existingSize = File.Exists(output) ? new FileInfo(output).Length : 0;

        if (existingSize > 0 && !supportsRange)
        {
            WriteLine("服务器不支持断点续传，重新下载", ClaspColorType.Yellow);
            File.Delete(output);
            existingSize = 0;
        }
        else if (existingSize > 0)
        {
            WriteLine($"断点续传: {FormatSize(existingSize)}", ClaspColorType.Yellow);
        }

        var chunkSize = totalSize / threads;
        var remainder = totalSize % threads;

        var ranges = new List<(long Start, long End)>();
        long start = 0;
        for (var i = 0; i < threads; i++)
        {
            var end = start + chunkSize - 1;
            if (i == threads - 1)
                end += remainder;

            if (end >= existingSize)
            {
                var effectiveStart = Math.Max(start, existingSize);
                if (effectiveStart <= end)
                    ranges.Add((effectiveStart, end));
            }

            start = end + 1;
        }

        if (ranges.Count == 0)
        {
            WriteLine("文件已完整，跳过下载", ClaspColorType.Yellow);
            return;
        }

        await using var file = existingSize > 0
            ? new FileStream(output, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
            : new FileStream(output, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        if (file.Length < totalSize)
            file.SetLength(totalSize);

        var downloaded = new long[ranges.Count];
        var tasks = new Task[ranges.Count];

        for (var i = 0; i < ranges.Count; i++)
        {
            var index = i;
            var (startPos, endPos) = ranges[index];
            tasks[i] = Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        ApplyHeaders(request);
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startPos, endPos);

                        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                        response.EnsureSuccessStatusCode();

                        await using var stream = await response.Content.ReadAsStreamAsync(ct);
                        var buffer = new byte[81920];
                        int read;
                        long offset = startPos;
                        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                        {
                            ct.ThrowIfCancellationRequested();

                            await RandomAccess.WriteAsync(file.SafeFileHandle, buffer.AsMemory(0, read), offset, ct);
                            offset += read;
                            Interlocked.Add(ref downloaded[index], read);
                        }
                        break;
                    }
                    catch when (attempt < 2)
                    {
                        WriteLine($"下载中断，{Math.Pow(2, attempt)} 秒后重试...", ClaspColorType.Yellow);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                    }
                }
            }, ct);
        }

        var sw = Stopwatch.StartNew();
        while (!Task.WhenAll(tasks).IsCompleted)
        {
            var total = downloaded.Sum();
            PrintProgress(total, totalSize, sw.Elapsed);
            await Task.Delay(200, ct);
        }

        await Task.WhenAll(tasks);
        Console.WriteLine();
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(UserAgent))
            request.Headers.Add("User-Agent", UserAgent);

        if (!string.IsNullOrWhiteSpace(Referer))
            request.Headers.Referrer = new Uri(Referer);

        if (!string.IsNullOrWhiteSpace(Cookie))
            request.Headers.Add("Cookie", Cookie);

        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        request.Headers.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.ConnectionClose = true;

        if (!string.IsNullOrWhiteSpace(Headers))
        {
            foreach (var line in Headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var index = line.IndexOf(':');
                if (index <= 0)
                    continue;

                var name = line[..index].Trim();
                var value = line[(index + 1)..].Trim();
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }
    }

    private void PrintProgress(long downloaded, long totalSize, TimeSpan elapsed)
    {
        var percent = totalSize > 0 ? (double)downloaded / totalSize * 100 : 0;
        var speed = FormatSpeed(downloaded, elapsed);
        var text = $"{percent:F1}% ({FormatSize(downloaded)}/{FormatSize(totalSize)}) {speed}";
        Write($"\r{text}", ClaspColorType.Cyan);
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        var len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F1} {sizes[order]}";
    }

    private static string FormatSpeed(long bytes, TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds <= 0)
            return "0 B/s";

        var bytesPerSecond = bytes / elapsed.TotalSeconds;
        string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
        var len = bytesPerSecond;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F1} {sizes[order]}";
    }

    private static string GetDefaultDownloadDirectory()
    {
        var home = Environment.GetEnvironmentVariable("USERPROFILE")
                   ?? Environment.GetEnvironmentVariable("HOME")
                   ?? string.Empty;

        var path = Path.Combine(home, "Downloads");

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"默认下载目录不存在: {path}");
        Debug.WriteLine($"默认下载目录: {path}");

        return path;
    }

    private static string GetInferredFileName(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var filename = uri.Query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2 && p[0].Equals("filename", StringComparison.OrdinalIgnoreCase))
                .Select(p => p[1])
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(filename))
                return Uri.UnescapeDataString(filename);
        }

        return Path.GetFileName(new Uri(url).AbsolutePath);
    }
}
