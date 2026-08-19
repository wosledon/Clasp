using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("urlenc", Description = "URL 编码或解码")]
internal class UrlEnc : ClaspCommand
{
    [ClaspOption("--decode", "-d", Description = "解码模式")]
    public bool Decode { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(input) && Console.IsInputRedirected)
            input = await ReadStandardInputAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHelp();
            return;
        }

        if (Decode)
            WriteLine(Uri.UnescapeDataString(input));
        else
            WriteLine(Uri.EscapeDataString(input));
    }
}
