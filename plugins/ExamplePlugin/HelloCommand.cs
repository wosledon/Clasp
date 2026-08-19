using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace ExamplePlugin;

[ClaspCommand("hello", Description = "向指定名称问好")]
internal class HelloCommand : ClaspCommand
{
    [ClaspOption("--name", "-n", Description = "要问好的名称")]
    public string Name { get; set; } = "world";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"Hello, {Name}!");
        await Task.CompletedTask;
    }
}
