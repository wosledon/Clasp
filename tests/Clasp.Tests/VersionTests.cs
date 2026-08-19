using Clasp.Commands;

namespace Clasp.Tests;

public class VersionTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnVersion()
    {
        var command = CommandTestHelper.CreateCommand<Commands.Version>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnShortVersion()
    {
        var command = CommandTestHelper.CreateCommand<Commands.Version>();
        CommandTestHelper.SetOption(command, nameof(Commands.Version.Short), true);

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
