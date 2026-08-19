using Clasp.Commands;

namespace Clasp.Tests;

public class JsonToolTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenInputIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<JsonTool>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFormatJson()
    {
        var command = CommandTestHelper.CreateCommand<JsonTool>();
        CommandTestHelper.SetOption(command, nameof(JsonTool.Input), "{\"a\":1}");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
