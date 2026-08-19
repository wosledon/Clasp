using Clasp.Commands;

namespace Clasp.Tests;

public class JwtTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenTokenIsEmpty()
    {
        var command = CommandTestHelper.CreateCommand<Jwt>();

        var threw = await CommandTestHelper.ValidateThrowsAsync(command);
        Assert.True(threw);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDecodeInvalidToken()
    {
        var command = CommandTestHelper.CreateCommand<Jwt>();
        CommandTestHelper.SetOption(command, nameof(Jwt.Token), "invalid.token");

        await CommandTestHelper.RunExecuteAsync(command);
    }
}
