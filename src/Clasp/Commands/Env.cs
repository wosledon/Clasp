using System.Collections;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("env", Description = "显示环境变量")]
internal class Env : ClaspCommand
{
    [ClaspOption("--name", Description = "按名称模糊过滤")]
    public string Name { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var entries = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(e => (Key: e.Key?.ToString() ?? string.Empty, Value: e.Value?.ToString() ?? string.Empty))
            .Where(e => string.IsNullOrWhiteSpace(Name) || e.Key.Contains(Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Key);

        foreach (var (key, value) in entries)
            WriteLine($"{key}={value}");

        await Task.CompletedTask;
    }
}
