using System.Net;
using System.Net.Sockets;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("dns", Description = "查询域名的 A/AAAA 记录")]
internal class DnsLookup : ClaspCommand
{
    [ClaspOption("--host", Description = "要查询的域名")]
    public string Host { get; set; } = string.Empty;

    [ClaspOption("--type", "-t", Description = "记录类型: A/AAAA (默认 A)")]
    public string Type { get; set; } = "A";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var host = Host.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            ValidationError("请提供要查询的域名");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var host = Host.Trim();
        var type = Type.Trim().ToUpperInvariant();
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            var found = false;
            foreach (var addr in addresses)
            {
                if (type == "A" && addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    WriteLine(addr.ToString());
                    found = true;
                }
                else if (type == "AAAA" && addr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    WriteLine(addr.ToString());
                    found = true;
                }
            }

            if (!found)
                WriteLine($"未找到 {host} 的 {type} 记录", ClaspColorType.Yellow);
        }
        catch (Exception ex)
        {
            WriteLine($"解析失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
