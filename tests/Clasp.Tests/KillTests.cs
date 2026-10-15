using Clasp.Commands;

namespace Clasp.Tests;

public class KillTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenPortIsInvalidAndNameIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_WhenPortIsValid()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();
        CommandTestHelper.SetOption(command, nameof(Kill.PortNumber), 8080);

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.False(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRunWithoutException()
    {
        var command = CommandTestHelper.CreateCommand<Kill>();
        CommandTestHelper.SetOption(command, nameof(Kill.PortNumber), 1);
        CommandTestHelper.SetOption(command, nameof(Kill.DryRun), true);

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public void ParseNetstatListenPids_ShouldKeepOnlyListeningTcpRowsOfThatPort()
    {
        var output = string.Join('\n',
            "活动连接",
            string.Empty,
            "  协议  本地地址          外部地址          状态           PID",
            "  TCP   0.0.0.0:8080      0.0.0.0:0         LISTENING       4242",
            "  TCP   127.0.0.1:8080    127.0.0.1:52000   ESTABLISHED     9999",
            "  TCP   0.0.0.0:8081      0.0.0.0:0         LISTENING       5151",
            "  UDP   0.0.0.0:8080      *:*                               7777",
            "  TCP   [::]:8080         [::]:0            LISTENING       4242");

        Assert.Equal(new List<int> { 4242 }, Kill.ParseNetstatListenPids(output, 8080));
    }

    [Fact]
    public void ParseLsofPids_ShouldDeduplicateAndIgnoreNoise()
    {
        Assert.Equal(new List<int> { 11, 22 }, Kill.ParseLsofPids("11\n22\n11\n\n"));
    }
}
