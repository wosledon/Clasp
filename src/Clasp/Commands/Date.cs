using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("date", Description = "显示当前日期和时间")]
internal class Date : ClaspCommand
{
    [ClaspOption("--format", "-f", Description = "日期格式字符串，默认 yyyy-MM-dd HH:mm:ss")]
    public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine(DateTime.Now.ToString(Format));
        await Task.CompletedTask;
    }
}
