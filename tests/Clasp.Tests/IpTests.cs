using Clasp.Commands;

namespace Clasp.Tests;

public class IpTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnLocalIps()
    {
        var command = CommandTestHelper.CreateCommand<Ip>();

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
