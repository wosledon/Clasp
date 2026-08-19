using Clasp.Commands;

namespace Clasp.Tests;

public class FileDownloadTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenUrlIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<FileDownload>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenUrlFileDoesNotExist()
    {
        var command = CommandTestHelper.CreateCommand<FileDownload>();
        CommandTestHelper.SetOption(command, nameof(FileDownload.UrlFile), "nonexistent.txt");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }
}
