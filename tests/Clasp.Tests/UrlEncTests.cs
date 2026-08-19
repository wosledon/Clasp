using Clasp.Commands;

namespace Clasp.Tests;

public class UrlEncTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenInputIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<UrlEnc>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEncodeText()
    {
        var command = CommandTestHelper.CreateCommand<UrlEnc>();
        CommandTestHelper.SetOption(command, nameof(UrlEnc.Input), "hello world");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
