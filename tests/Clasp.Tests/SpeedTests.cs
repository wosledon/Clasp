using Clasp.Commands;

namespace Clasp.Tests;

public class SpeedTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenUrlIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Speed>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenUrlIsValid()
    {
        var command = CommandTestHelper.CreateCommand<Speed>();
        CommandTestHelper.SetOption(command, nameof(Speed.Url), "http://example.com");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}
