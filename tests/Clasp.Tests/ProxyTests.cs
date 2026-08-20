using Clasp.Commands;

namespace Clasp.Tests;

public class ProxyTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenPortIsInvalid()
    {
        var command = CommandTestHelper.CreateCommand<Proxy>();
        CommandTestHelper.SetOption(command, nameof(Proxy.Port), 0);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenPortIsTooLarge()
    {
        var command = CommandTestHelper.CreateCommand<Proxy>();
        CommandTestHelper.SetOption(command, nameof(Proxy.Port), 65536);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenPortIsValid()
    {
        var command = CommandTestHelper.CreateCommand<Proxy>();
        CommandTestHelper.SetOption(command, nameof(Proxy.Port), 8080);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenForwardUrlIsInvalid()
    {
        var command = CommandTestHelper.CreateCommand<Proxy>();
        CommandTestHelper.SetOption(command, nameof(Proxy.Port), 8080);
        CommandTestHelper.SetOption(command, nameof(Proxy.Forward), "not-a-url");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WithForwardUrl()
    {
        var command = CommandTestHelper.CreateCommand<Proxy>();
        CommandTestHelper.SetOption(command, nameof(Proxy.Port), 8080);
        CommandTestHelper.SetOption(command, nameof(Proxy.Forward), "http://localhost:5000");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}