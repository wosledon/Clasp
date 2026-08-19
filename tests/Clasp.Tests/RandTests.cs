using Clasp.Commands;

namespace Clasp.Tests;

public class RandTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldGeneratePassword()
    {
        var command = CommandTestHelper.CreateCommand<Rand>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateInteger()
    {
        var command = CommandTestHelper.CreateCommand<Rand>();
        CommandTestHelper.SetOption(command, nameof(Rand.Type), "int");
        CommandTestHelper.SetOption(command, nameof(Rand.Min), 1);
        CommandTestHelper.SetOption(command, nameof(Rand.Max), 10);

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
