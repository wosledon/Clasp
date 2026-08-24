using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("bar", Description = "进度条工具")]
internal class Bar : ClaspCommand
{
    [ClaspOption("--progress", "-p", Description = "进度百分比（0-100）")]
    public double Progress { get; set; }

    [ClaspOption("--width", "-w", Description = "进度条宽度（默认20）")]
    public int Width { get; set; } = 20;

    [ClaspOption("--label", "-l", Description = "进度条标签")]
    public string Label { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Progress < 0 || Progress > 100)
            ValidationError("--progress 必须在 0-100 之间");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var filledWidth = (int)((Progress / 100.0) * Width);
        var emptyWidth = Width - filledWidth;
        var filledPart = new string('=', filledWidth);
        var emptyPart = new string('-', emptyWidth);
        var bar = $"[{filledPart}{emptyPart}] {Progress:F1}%";

        if (!string.IsNullOrEmpty(Label))
            bar = $"{Label} {bar}";

        WriteLine(bar, ClaspColorType.Green);
    }
}
