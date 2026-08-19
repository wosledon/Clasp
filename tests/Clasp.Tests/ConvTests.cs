using Clasp.Commands;

namespace Clasp.Tests;

public class ConvTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenFromIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Conv>();
        CommandTestHelper.SetOption(command, nameof(Conv.Number), 1024);
        CommandTestHelper.SetOption(command, nameof(Conv.To), "kb");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenToIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Conv>();
        CommandTestHelper.SetOption(command, nameof(Conv.Number), 1024);
        CommandTestHelper.SetOption(command, nameof(Conv.From), "b");

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConvertBytes()
    {
        var command = CommandTestHelper.CreateCommand<Conv>();
        CommandTestHelper.SetOption(command, nameof(Conv.Number), 1024);
        CommandTestHelper.SetOption(command, nameof(Conv.From), "b");
        CommandTestHelper.SetOption(command, nameof(Conv.To), "kb");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
