using Clasp.Commands;

namespace Clasp.Tests;

public class ProcsTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnProcessList()
    {
        var command = CommandTestHelper.CreateCommand<Procs>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByName()
    {
        var command = CommandTestHelper.CreateCommand<Procs>();
        CommandTestHelper.SetOption(command, nameof(Procs.Name), "dotnet");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
