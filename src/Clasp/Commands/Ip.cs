using System.Net.NetworkInformation;
using System.Net.Sockets;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("ip", Description = "显示本机 IP 地址")]
internal class Ip : ClaspCommand
{
    [ClaspOption("--public", "-p", Description = "查询公网 IP")]
    public bool Public { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Public)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var ip = await client.GetStringAsync("https://api.ipify.org", cancellationToken);
                WriteLine(ip.Trim());
            }
            catch (Exception ex)
            {
                WriteLine($"查询公网 IP 失败: {ex.Message}", ClaspColorType.BrightRed);
            }
            return;
        }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (var addr in nic.GetIPProperties().UnicastAddresses)
            {
                var kind = addr.Address.AddressFamily switch
                {
                    AddressFamily.InterNetwork => "IPv4",
                    AddressFamily.InterNetworkV6 => "IPv6",
                    _ => addr.Address.AddressFamily.ToString()
                };

                if (kind is "IPv4" or "IPv6")
                    WriteLine($"{nic.Name,-25} {addr.Address,-45} {kind}");
            }
        }
    }
}
