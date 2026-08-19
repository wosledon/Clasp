using Clasp.Commands;

namespace Clasp.Tests;

public class EnvTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnEnvironmentVariables()
    {
        var command = CommandTestHelper.CreateCommand<Env>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterByName()
    {
        var command = CommandTestHelper.CreateCommand<Env>();
        CommandTestHelper.SetOption(command, nameof(Env.Name), "PATH");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
