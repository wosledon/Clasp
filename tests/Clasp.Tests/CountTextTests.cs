using System.IO;

using Clasp.Commands;

namespace Clasp.Tests;

public class CountTextTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCountText()
    {
        var command = CommandTestHelper.CreateCommand<CountText>();
        CommandTestHelper.SetOption(command, nameof(CountText.Input), "hello world");

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCountFile()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "line1\nline2\nline3");

        try
        {
            var command = CommandTestHelper.CreateCommand<CountText>();
            CommandTestHelper.SetOption(command, nameof(CountText.Input), tempFile);

            await CommandTestHelper.RunExecuteAsync(command);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
