using Clasp.Commands;

namespace Clasp.Tests;

public class DnsLookupTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenHostIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<DnsLookup>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenHostIsProvided()
    {
        var command = CommandTestHelper.CreateCommand<DnsLookup>();
        CommandTestHelper.SetOption(command, nameof(DnsLookup.Host), "example.com");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }
}
