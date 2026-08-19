using System.Text;
using System.Text.Json;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("jwt", Description = "解码 JWT (不验签)")]
internal class Jwt : ClaspCommand
{
    [ClaspOption("--compact", "-c", Description = "压缩 JSON 输出")]
    public bool Compact { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var token = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token) && Console.IsInputRedirected)
            token = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            ShowHelp();
            return;
        }

        var parts = token.Trim().Split('.');
        if (parts.Length != 3)
        {
            WriteLine("无效 JWT: 应包含 3 段", ClaspColorType.BrightRed);
            return;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = !Compact };

            using var headerDoc = JsonDocument.Parse(Base64UrlDecode(parts[0]));
            WriteLine("Header:", ClaspColorType.Cyan);
            WriteLine(JsonSerializer.Serialize(headerDoc.RootElement, options));

            using var payloadDoc = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            WriteLine("Payload:", ClaspColorType.Cyan);
            WriteLine(JsonSerializer.Serialize(payloadDoc.RootElement, options));

            foreach (var claim in new[] { "exp", "iat", "nbf" })
            {
                if (payloadDoc.RootElement.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.Number)
                {
                    var time = DateTimeOffset.FromUnixTimeSeconds(value.GetInt64()).ToLocalTime();
                    WriteLine($"{claim}: {time:yyyy-MM-dd HH:mm:ss} (本地)", ClaspColorType.Yellow);
                }
            }

            var sig = parts[2];
            WriteLine($"签名: {sig[..Math.Min(24, sig.Length)]}... (未验证)", ClaspColorType.Yellow);
        }
        catch (Exception ex)
        {
            WriteLine($"JWT 解码失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }

    private static string Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
