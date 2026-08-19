using System.IO;
using System.Reflection;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("help", Description = "显示所有支持的工具")]
internal class Help : ClaspCommand
{
    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
        var registry = CommandRegistry.Scan(Assembly.GetExecutingAssembly(), pluginsPath);
        foreach (var line in ClaspHelp.RenderCommandList(registry.GetCommands()))
            WriteLine(line);

        await Task.CompletedTask;
    }
}
