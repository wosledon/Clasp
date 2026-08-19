using Clasp.Commands;

namespace Clasp.Tests;

public class HelpTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnHelpText()
    {
        var command = CommandTestHelper.CreateCommand<Help>();

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
