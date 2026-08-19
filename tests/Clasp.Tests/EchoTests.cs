using Clasp.Commands;

namespace Clasp.Tests;

public class EchoTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenMessageIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Echo>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEchoMessage()
    {
        var command = CommandTestHelper.CreateCommand<Echo>();
        CommandTestHelper.SetOption(command, nameof(Echo.Message), "hello");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
