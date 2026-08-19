using System.IO;

using Clasp.Commands;

namespace Clasp.Tests;

public class CatTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenFileIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Cat>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenFileDoesNotExist()
    {
        var command = CommandTestHelper.CreateCommand<Cat>();
        CommandTestHelper.SetOption(command, nameof(Cat.TargetFile), "nonexistent.txt");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReadFile()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "line1\nline2");

        try
        {
            var command = CommandTestHelper.CreateCommand<Cat>();
            CommandTestHelper.SetOption(command, nameof(Cat.TargetFile), tempFile);

            await CommandTestHelper.RunExecuteAsync(command);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
