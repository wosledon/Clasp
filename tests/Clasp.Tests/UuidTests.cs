using Clasp.Commands;

namespace Clasp.Tests;

public class UuidTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldGenerateUuid()
    {
        var command = CommandTestHelper.CreateCommand<Uuid>();

        await CommandTestHelper.RunExecuteAsync(command);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateMultipleUuids()
    {
        var command = CommandTestHelper.CreateCommand<Uuid>();
        CommandTestHelper.SetOption(command, nameof(Uuid.Count), 3);

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
