using System.IO;

using Clasp.Commands;

namespace Clasp.Tests;

public class Base64Tests
{
    [Fact]
    public async Task ExecuteAsync_ShouldEncodeInput()
    {
        var command = CommandTestHelper.CreateCommand<Base64>();
        CommandTestHelper.SetOption(command, nameof(Base64.Input), "hello");

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDecodeInput()
    {
        var command = CommandTestHelper.CreateCommand<Base64>();
        CommandTestHelper.SetOption(command, nameof(Base64.Decode), true);
        CommandTestHelper.SetOption(command, nameof(Base64.Input), "aGVsbG8=");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
