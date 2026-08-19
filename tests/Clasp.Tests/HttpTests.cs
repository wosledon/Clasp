using Clasp.Commands;

namespace Clasp.Tests;

public class HttpTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenUrlIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Http>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenUrlIsProvided()
    {
        var command = CommandTestHelper.CreateCommand<Http>();
        CommandTestHelper.SetOption(command, nameof(Http.Url), "http://example.com");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}
