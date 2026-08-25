using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System.Reflection;

namespace Clasp.Commands;

[ClaspCommand("version", Description = "显示版本号")]
internal class Version : ClaspCommand
{
    [ClaspOption("--short", "-s", Description = "仅显示版本号")]
    public bool Short { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var version = typeof(Version).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? typeof(Version).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        if (Short)
        {
            WriteLine(version);
        }
        else
        {
            WriteLine($"Clasp {version}");
        }

        await Task.CompletedTask;
    }
}
