using System.Reflection;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("help", Description = "显示所有支持的工具")]
internal class Help : ClaspCommand
{
    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var registry = CommandRegistry.Scan(Assembly.GetExecutingAssembly());
        foreach (var line in ClaspHelp.RenderCommandList(registry.GetCommands()))
            WriteLine(line);

        await Task.CompletedTask;
    }
}
