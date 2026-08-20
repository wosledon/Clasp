using Clasp.Commands;

namespace Clasp.Tests;

public class ScanTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenHostIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Range), "80-100");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenRangeIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Host), "127.0.0.1");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenRangeIsInvalid()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Scan.Range), "abc-def");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenHostAndRangeAreValid()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Scan.Range), "80-100");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WithCommaSeparatedPorts()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Scan.Range), "22,80,443");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WithSinglePort()
    {
        var command = CommandTestHelper.CreateCommand<Scan>();
        CommandTestHelper.SetOption(command, nameof(Scan.Host), "127.0.0.1");
        CommandTestHelper.SetOption(command, nameof(Scan.Range), "80");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}