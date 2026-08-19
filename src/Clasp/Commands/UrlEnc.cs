using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("urlenc", Description = "URL 编码或解码")]
internal class UrlEnc : ClaspCommand
{
    [ClaspOption("--decode", "-d", Description = "解码模式")]
    public bool Decode { get; set; }

    [ClaspOption("--input", "-i", Description = "要编码或解码的文本")]
    public string Input { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = Input;
        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            input = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(input))
        {
            ValidationError("请提供要编码或解码的文本");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Input) && Console.IsInputRedirected)
            Input = await ReadStandardInputAsync(cancellationToken);

        if (Decode)
            WriteLine(Uri.UnescapeDataString(Input));
        else
            WriteLine(Uri.EscapeDataString(Input));
    }
}
