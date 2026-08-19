using Clasp.Commands;

namespace Clasp.Tests;

public class KillTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenPortIsInvalidAndNameIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenPortIsValid()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();
        CommandTestHelper.SetOption(command, nameof(Kill.PortNumber), 8080);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRunWithoutException()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();
        CommandTestHelper.SetOption(command, nameof(Kill.PortNumber), 1);
        CommandTestHelper.SetOption(command, nameof(Kill.DryRun), true);

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
