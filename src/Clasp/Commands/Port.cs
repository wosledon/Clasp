using System.Net.Sockets;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("port", Description = "检测 TCP 端口是否开放")]
internal class Port : ClaspCommand
{
    [ClaspOption("--timeout", Description = "超时毫秒 (默认 2000)")]
    public int Timeout { get; set; } = 2000;

    [ClaspOption("--host", Description = "主机名或 IP")]
    public string Host { get; set; } = string.Empty;

    [ClaspOption("--port", "-p", Description = "端口号")]
    public int PortNumber { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var host = Host;
        var portText = PortNumber == 0 ? string.Empty : PortNumber.ToString();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portText))
        {
            ValidationError("请提供主机和端口");
        }

        if (!int.TryParse(portText, out var port) || port is < 1 or > 65535)
        {
            ValidationError($"无效端口: {portText}");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var portText = PortNumber == 0 ? string.Empty : PortNumber.ToString();
        var port = int.Parse(portText);

        using var client = new TcpClient();
        var connect = client.ConnectAsync(Host, port, cancellationToken).AsTask();
        var timeoutTask = Task.Delay(Math.Clamp(Timeout, 100, 60000));

        var completed = await Task.WhenAny(connect, timeoutTask);
        if (completed == timeoutTask)
        {
            WriteLine($"{Host}:{port} 关闭或不可达 (超时)", ClaspColorType.Yellow);
            return;
        }

        if (connect.IsFaulted)
        {
            WriteLine($"{Host}:{port} 关闭或不可达 ({connect.Exception?.InnerException?.Message})", ClaspColorType.Yellow);
            return;
        }

        WriteLine($"{Host}:{port} 开放", ClaspColorType.BrightGreen);
    }
}
