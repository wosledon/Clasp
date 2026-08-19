using Clasp.Commands;

namespace Clasp.Tests;

public class SysInfoTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnSystemInfo()
    {
        var command = CommandTestHelper.CreateCommand<SysInfo>();

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
