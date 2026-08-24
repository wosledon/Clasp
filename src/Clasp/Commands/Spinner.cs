using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("spinner", Description = "加载动画工具")]
internal class Spinner : ClaspCommand
{
    [ClaspOption("--message", "-m", Description = "提示消息（默认Loading...）")]
    public string Message { get; set; } = "Loading...";

    [ClaspOption("--style", "-s", Description = "动画样式：dots, line, bounce, arrows（默认dots）")]
    public string Style { get; set; } = "dots";

    [ClaspOption("--duration", "-d", Description = "显示持续时间（秒，默认5）")]
    public int Duration { get; set; } = 5;

    [ClaspOption("--success", Description = "成功消息")]
    public string Success { get; set; } = string.Empty;

    [ClaspOption("--fail", Description = "失败消息")]
    public string Fail { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Duration < 0)
            ValidationError("--duration 必须 >= 0");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var spinner = CreateSpinner(Style);
        var color = ClaspColor.FromEnum(ClaspColorType.Cyan);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var spinnerTask = ShowSpinnerAsync(spinner, Message, color, Duration, cts.Token);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Duration), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }

        cts.Cancel();
        await spinnerTask;

        if (!string.IsNullOrEmpty(Success))
        {
            WriteLine(Success, ClaspColorType.Green);
        }
        else if (!string.IsNullOrEmpty(Fail))
        {
            WriteLine(Fail, ClaspColorType.Red);
        }
    }

    private string[] CreateSpinner(string style)
    {
        return style.ToLower() switch
        {
            "line" => new[] { "|", "/", "-", "\\" },
            "bounce" => new[] { "⠁", "⠂", "⠄", "⠂" },
            "arrows" => new[] { "←", "↖", "↑", "↗", "→", "↘", "↓", "↙" },
            "dots" or _ => new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" }
        };
    }

    private async Task ShowSpinnerAsync(string[] frames, string message, ClaspColor color, int duration, CancellationToken cancellationToken)
    {
        var frameIndex = 0;
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var frame = frames[frameIndex % frames.Length];
                var elapsed = (DateTimeOffset.UtcNow - startTime).TotalSeconds;

                Console.Write($"\r{color.Apply($"{frame} {message}")} {elapsed:F1}s ");

                frameIndex++;
                await Task.Delay(80, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 动画结束
        }

        Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}\r");
    }
}