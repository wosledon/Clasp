using System.IO;

using Clasp.Commands;

namespace Clasp.Tests;

public class LsTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldListCurrentDirectory()
    {
        var command = CommandTestHelper.CreateCommand<ListFiles>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMissingDirectory()
    {
        var command = CommandTestHelper.CreateCommand<ListFiles>();
        CommandTestHelper.SetOption(command, nameof(ListFiles.TargetDir), "nonexistent_directory_12345");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
