using Clasp.Commands;

namespace Clasp.Tests;

public class DateTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnCurrentDate()
    {
        var command = CommandTestHelper.CreateCommand<Date>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseCustomFormat()
    {
        var command = CommandTestHelper.CreateCommand<Date>();
        CommandTestHelper.SetOption(command, nameof(Date.Format), "yyyy-MM-dd");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
