using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("http", Description = "发送 HTTP 请求并显示响应")]
internal class Http : ClaspCommand
{
    [ClaspOption("--method", "-X", Description = "请求方法 (默认 GET)")]
    public string Method { get; set; } = "GET";

    [ClaspOption("--header", "-H", Description = "自定义请求头，格式 Name: Value")]
    public string Header { get; set; } = string.Empty;

    [ClaspOption("--data", "-d", Description = "请求体内容")]
    public string Data { get; set; } = string.Empty;

    [ClaspOption("--timeout", Description = "超时秒数 (默认 30)")]
    public int Timeout { get; set; } = 30;

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var url = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowHelp();
            return;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Clamp(Timeout, 1, 600)) };
        using var request = new HttpRequestMessage(new HttpMethod(Method.Trim().ToUpperInvariant()), url);

        if (!string.IsNullOrWhiteSpace(Header))
        {
            var idx = Header.IndexOf(':');
            var name = idx > 0 ? Header[..idx].Trim() : Header.Trim();
            var value = idx > 0 ? Header[(idx + 1)..].Trim() : string.Empty;
            request.Headers.TryAddWithoutValidation(name, value);
        }

        if (!string.IsNullOrEmpty(Data))
            request.Content = new StringContent(Data);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var color = (int)response.StatusCode < 400 ? ClaspColorType.Green : ClaspColorType.BrightRed;
            WriteLine($"{(int)response.StatusCode} {response.ReasonPhrase}", color);

            foreach (var header in response.Headers)
                WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
                WriteLine(body);
        }
        catch (Exception ex)
        {
            WriteLine($"请求失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }
}
