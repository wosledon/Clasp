using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("table", Description = "将JSON/CSV/文本输出为表格")]
internal class Table : ClaspCommand
{
    [ClaspOption("--input", "-i", Description = "输入文本")]
    public string Input { get; set; } = string.Empty;

    [ClaspOption("--format", "-f", Description = "输入格式：json, csv, text（默认text）")]
    public string Format { get; set; } = "text";

    [ClaspOption("--headers", Description = "自定义列标题（逗号分隔）")]
    public string Headers { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Input))
            ValidationError("请提供 --input 输入文本");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var rows = Input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        if (rows.Count == 0)
        {
            WriteLine("空数据", ClaspColorType.Yellow);
            return;
        }

        List<string> headers;
        if (!string.IsNullOrEmpty(Headers))
        {
            headers = Headers.Split(',').Select(h => h.Trim()).ToList();
        }
        else
        {
            headers = Enumerable.Range(0, rows[0].Length).Select(i => $"列{i + 1}").ToList();
        }

        var columnWidths = new List<int>();
        for (int i = 0; i < headers.Count; i++)
        {
            var maxWidth = headers[i].Length;
            foreach (var row in rows)
            {
                if (i < row.Length)
                    maxWidth = Math.Max(maxWidth, row[i].Length);
            }
            columnWidths.Add(maxWidth);
        }

        var top = "+" + string.Join("+", columnWidths.Select(w => new string('-', w + 2))) + "+";
        var mid = "+" + string.Join("+", columnWidths.Select(w => new string('-', w + 2))) + "+";
        var bot = "+" + string.Join("+", columnWidths.Select(w => new string('-', w + 2))) + "+";

        WriteLine(top, ClaspColorType.Cyan);
        var headerLine = "|";
        for (int i = 0; i < headers.Count; i++)
        {
            var text = headers[i].PadRight(columnWidths[i]);
            headerLine += $" {text} |";
        }
        WriteLine(headerLine, ClaspColorType.BrightYellow);
        WriteLine(mid, ClaspColorType.Cyan);

        foreach (var row in rows)
        {
            var line = "|";
            for (int i = 0; i < headers.Count; i++)
            {
                var text = i < row.Length ? row[i] : string.Empty;
                text = text.PadRight(columnWidths[i]);
                line += $" {text} |";
            }
            WriteLine(line);
        }

        WriteLine(bot, ClaspColorType.Cyan);
    }
}
