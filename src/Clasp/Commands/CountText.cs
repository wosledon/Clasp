using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("count", Description = "统计文本的行数、词数、字符数")]
internal class CountText : ClaspCommand
{
    [ClaspOption("--lines", "-l", Description = "仅显示行数")]
    public bool LinesOnly { get; set; }

    [ClaspOption("--words", "-w", Description = "仅显示词数")]
    public bool WordsOnly { get; set; }

    [ClaspOption("--chars", "-c", Description = "仅显示字符数")]
    public bool CharsOnly { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var input = args.Values.FirstOrDefault();

        string text;
        if (!string.IsNullOrWhiteSpace(input) && File.Exists(input))
        {
            text = await File.ReadAllTextAsync(input, cancellationToken);
        }
        else if (Console.IsInputRedirected)
        {
            text = await ReadStandardInputAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(input))
        {
            text = input;
        }
        else
        {
            ShowHelp();
            return;
        }

        var lineCount = string.IsNullOrEmpty(text)
            ? 0
            : text.Count(c => c == '\n') + (text[^1] == '\n' ? 0 : 1);
        var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = text.Length;

        var showAll = !LinesOnly && !WordsOnly && !CharsOnly;
        if (showAll || LinesOnly)
            WriteLine($"行数: {lineCount}");
        if (showAll || WordsOnly)
            WriteLine($"词数: {wordCount}");
        if (showAll || CharsOnly)
            WriteLine($"字符数: {charCount}");
    }
}
