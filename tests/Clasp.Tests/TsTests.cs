using Clasp.Commands;

namespace Clasp.Tests;

public class TsTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenInputIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Ts>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldParseTimestamp()
    {
        var command = CommandTestHelper.CreateCommand<Ts>();
        CommandTestHelper.SetOption(command, nameof(Ts.Input), "1609459200");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
