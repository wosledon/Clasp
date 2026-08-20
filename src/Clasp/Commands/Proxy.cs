using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("proxy", Description = "启动本地 HTTP 代理 (正向代理/反向代理)")]
internal class Proxy : ClaspCommand
{
    [ClaspOption("--port", "-p", Description = "监听端口 (默认 8080)")]
    public int Port { get; set; } = 8080;

    [ClaspOption("--forward", "-f", Description = "转发目标 URL (反向代理模式)")]
    public string Forward { get; set; } = string.Empty;

    [ClaspOption("--verbose", "-v", Description = "显示请求详情")]
    public bool Verbose { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Port is < 1 or > 65535)
            ValidationError("端口号必须在 1-65535 之间");

        if (!string.IsNullOrWhiteSpace(Forward) && !Uri.TryCreate(Forward, UriKind.Absolute, out var uri))
            ValidationError("转发目标 URL 格式无效");

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var isReverse = !string.IsNullOrWhiteSpace(Forward);

        if (!isReverse)
            WriteLine($"正向代理已启动: http://localhost:{Port}", ClaspColorType.Green);
        else
            WriteLine($"反向代理已启动: http://localhost:{Port} -> {Forward}", ClaspColorType.Green);

        WriteLine("按 Ctrl+C 停止", ClaspColorType.Yellow);

        var listener = new TcpListener(IPAddress.Loopback, Port);
        listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleConnectionAsync(client, isReverse, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        finally
        {
            listener.Stop();
        }

        WriteLine("\n代理已停止", ClaspColorType.Yellow);
    }

    private async Task HandleConnectionAsync(TcpClient client, bool isReverse, CancellationToken cancellationToken)
    {
        try
        {
            client.ReceiveTimeout = 30000;
            client.SendTimeout = 30000;

            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

            var requestLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            var parts = requestLine.Split(' ');
            if (parts.Length < 3)
                return;

            var method = parts[0].ToUpperInvariant();
            var rawUrl = parts[1];
            var version = parts[2];

            // Read headers
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? headerLine;
            while (!string.IsNullOrWhiteSpace(headerLine = await reader.ReadLineAsync(cancellationToken)))
            {
                var colonIdx = headerLine.IndexOf(':');
                if (colonIdx > 0)
                {
                    var name = headerLine[..colonIdx].Trim();
                    var value = headerLine[(colonIdx + 1)..].Trim();
                    headers[name] = value;
                }
            }

            if (Verbose)
            {
                WriteLine($"\n{DateTime.Now:HH:mm:ss} {method} {rawUrl}", ClaspColorType.Cyan);
                if (isReverse)
                    WriteLine($"  -> {Forward}{rawUrl}", ClaspColorType.Magenta);
            }

            if (method == "CONNECT")
            {
                await HandleConnectTunnelAsync(rawUrl, stream, cancellationToken);
                return;
            }

            // Determine target URL
            Uri targetUri;
            if (isReverse)
            {
                var baseUri = new Uri(Forward.TrimEnd('/'));
                targetUri = new Uri(baseUri, rawUrl);
            }
            else
            {
                if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out targetUri!))
                {
                    WriteLine($"无效请求 URL: {rawUrl}", ClaspColorType.BrightRed);
                    return;
                }
            }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var request = new HttpRequestMessage(new HttpMethod(method), targetUri);

            // Copy headers (skip hop-by-hop and host for HTTP/1.0 compatibility)
            foreach (var (name, value) in headers)
            {
                if (HopByHopHeaders.Contains(name))
                    continue;
                if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) && isReverse)
                    continue;
                request.Headers.TryAddWithoutValidation(name, value);
            }

            // Read and forward body for POST/PUT/PATCH etc.
            if (method is "POST" or "PUT" or "PATCH" && headers.TryGetValue("Content-Length", out var clStr)
                && int.TryParse(clStr, out var contentLength) && contentLength > 0)
            {
                var body = new byte[contentLength];
                var read = 0;
                while (read < contentLength)
                {
                    var n = await stream.ReadAsync(body.AsMemory(read, contentLength - read), cancellationToken);
                    if (n == 0) break;
                    read += n;
                }
                request.Content = new ByteArrayContent(body);
                if (headers.TryGetValue("Content-Type", out var contentType))
                    request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }

            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var sb = new StringBuilder();
                sb.Append($"{version} {(int)response.StatusCode} {response.ReasonPhrase}\r\n");

                foreach (var header in response.Headers)
                    sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");

                foreach (var header in response.Content.Headers)
                    sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");

                sb.Append("\r\n");
                var headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
                await stream.WriteAsync(headerBytes, cancellationToken);

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await responseStream.CopyToAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                var error = $"{version} 502 Bad Gateway\r\nContent-Type: text/plain\r\n\r\nProxy Error: {ex.Message}";
                var errorBytes = Encoding.ASCII.GetBytes(error);
                await stream.WriteAsync(errorBytes, cancellationToken);
                WriteLine($"  转发失败: {ex.Message}", ClaspColorType.BrightRed);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            if (Verbose)
                WriteLine($"  连接处理异常: {ex.Message}", ClaspColorType.BrightRed);
        }
    }

    private static async Task HandleConnectTunnelAsync(string target, NetworkStream clientStream, CancellationToken cancellationToken)
    {
        var parts = target.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 443;

        try
        {
            using var targetClient = new TcpClient();
            await targetClient.ConnectAsync(host, port, cancellationToken);
            await using var targetStream = targetClient.GetStream();

            // Send 200 Connection Established
            var ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
            await clientStream.WriteAsync(ok, cancellationToken);

            // Bidirectional tunnel
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var task1 = RelayAsync(clientStream, targetStream, cts.Token);
            var task2 = RelayAsync(targetStream, clientStream, cts.Token);

            await Task.WhenAny(task1, task2);
            cts.Cancel();
        }
        catch (Exception ex)
        {
            var error = Encoding.ASCII.GetBytes($"HTTP/1.1 502 Bad Gateway\r\n\r\nTunnel Error: {ex.Message}");
            await clientStream.WriteAsync(error, cancellationToken);
        }
    }

    private static async Task RelayAsync(NetworkStream from, NetworkStream to, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[8192];
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await from.ReadAsync(buffer, cancellationToken);
                if (read == 0) break;
                await to.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch
        {
            // tunnel closed
        }
    }

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Proxy-Connection", "Proxy-Authenticate", "Proxy-Authorization",
        "Connection", "Transfer-Encoding", "Upgrade"
    };
}