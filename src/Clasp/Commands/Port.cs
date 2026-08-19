using System.Net.Sockets;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("port", Description = "检测 TCP 端口是否开放")]
internal class Port : ClaspCommand
{
    [ClaspOption("--timeout", Description = "超时毫秒 (默认 2000)")]
    public int Timeout { get; set; } = 2000;

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var host = args.Values.FirstOrDefault();
        var portText = args.Values.Skip(1).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portText))
        {
            ShowHelp();
            return;
        }

        if (!int.TryParse(portText, out var port) || port is < 1 or > 65535)
        {
            WriteLine($"无效端口: {portText}", ClaspColorType.BrightRed);
            return;
        }

        using var client = new TcpClient();
        var connect = client.ConnectAsync(host, port, cancellationToken).AsTask();
        var timeoutTask = Task.Delay(Math.Clamp(Timeout, 100, 60000));

        var completed = await Task.WhenAny(connect, timeoutTask);
        if (completed == timeoutTask)
        {
            WriteLine($"{host}:{port} 关闭或不可达 (超时)", ClaspColorType.Yellow);
            return;
        }

        if (connect.IsFaulted)
        {
            WriteLine($"{host}:{port} 关闭或不可达 ({connect.Exception?.InnerException?.Message})", ClaspColorType.Yellow);
            return;
        }

        WriteLine($"{host}:{port} 开放", ClaspColorType.BrightGreen);
    }
}
