using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System.Threading;
using System.Threading.Tasks;

namespace TestCsPlugin;

[ClaspCommand("cshello", Description = "测试 C# 源码插件")]
internal class CsHelloCommand : ClaspCommand
{
    [ClaspOption("--name", "-n", Description = "要问好的名称")]
    public string Name { get; set; } = "world";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"Hello from CS plugin, {Name}!");
        await Task.CompletedTask;
    }
}
