using Clasp.Commands;

namespace Clasp.Tests;

public class PortTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenHostIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Port>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenPortIsInvalid()
    {
        var command = CommandTestHelper.CreateCommand<Port>();
        CommandTestHelper.SetOption(command, nameof(Port.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Port.PortNumber), 0);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenHostAndPortAreValid()
    {
        var command = CommandTestHelper.CreateCommand<Port>();
        CommandTestHelper.SetOption(command, nameof(Port.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Port.PortNumber), 80);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}
