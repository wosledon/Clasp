using Clasp.Commands;

namespace Clasp.Tests;

public class HashTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Hash>();

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
