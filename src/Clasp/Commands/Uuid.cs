using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("uuid", Description = "生成 UUID (v4)")]
internal class Uuid : ClaspCommand
{
    [ClaspOption("--count", "-n", Description = "生成数量 (默认 1)")]
    public int Count { get; set; } = 1;

    [ClaspOption("--no-dash", Description = "去掉连字符")]
    public bool NoDash { get; set; }

    [ClaspOption("--upper", "-u", Description = "大写输出")]
    public bool Upper { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(Count, 1, 1000);
        for (var i = 0; i < count; i++)
        {
            var value = Guid.NewGuid().ToString(NoDash ? "N" : "D");
            WriteLine(Upper ? value.ToUpperInvariant() : value);
        }

        await Task.CompletedTask;
    }
}
