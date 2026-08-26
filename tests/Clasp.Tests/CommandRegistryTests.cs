using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Clasp.Commands;
using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Tests;

public class CommandRegistryTests
{
    [Fact]
    public void Scan_ShouldFindAllCommands()
    {
        var claspAssembly = typeof(DnsLookup).Assembly;
        var registry = CommandRegistry.Scan(claspAssembly);

        var commands = registry.GetCommands().ToList();
        Assert.NotEmpty(commands);

        var names = commands.SelectMany(c => c.Names.Split(',')).Select(n => n.Trim()).ToList();
        Assert.Contains("dns", names);
        Assert.Contains("kill", names);
        Assert.Contains("http", names);
        Assert.Contains("port", names);
    }

    [Fact]
    public void GetCommands_ShouldReturnCommandDescriptions()
    {
        var claspAssembly = typeof(DnsLookup).Assembly;
        var registry = CommandRegistry.Scan(claspAssembly);

        var commands = registry.GetCommands().ToList();
        var dns = commands.FirstOrDefault(c => c.Names.Contains("dns"));

        Assert.NotNull(dns.Description);
        Assert.Equal("查询域名的 A/AAAA 记录", dns.Description);
    }

    [ClaspCommand("optparsetest", Description = "option parsing test")]
    private class OptionParseTestCommand : ClaspCommand
    {
        [ClaspOption("--name", "-n", Description = "name")]
        public string Name { get; set; } = string.Empty;

        [ClaspOption("--flag", "-f", Description = "flag")]
        public bool Flag { get; set; }

        public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(Name))
                WriteLine($"NAME={Name}");

            if (Flag)
                WriteLine("FLAG=true");

            foreach (var value in args.Values)
                WriteLine($"VAL={value}");

            await Task.CompletedTask;
        }
    }

    private static CommandRegistry CreateRegistry()
    {
        return CommandRegistry.Scan(typeof(OptionParseTestCommand).Assembly);
    }

    [Fact]
    public async Task Dispatch_ShouldParseAttachedLongValue()
    {
        var registry = CreateRegistry();
        using var sw = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(sw);
            var result = await registry.DispatchAsync(new[] { "optparsetest", "--name=alice" });
            Assert.Equal(0, result);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = sw.ToString();
        Assert.Contains("NAME=alice", output);
    }

    [Fact]
    public async Task Dispatch_ShouldParseAttachedShortValue()
    {
        var registry = CreateRegistry();
        using var sw = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(sw);
            var result = await registry.DispatchAsync(new[] { "optparsetest", "-nalice" });
            Assert.Equal(0, result);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = sw.ToString();
        Assert.Contains("NAME=alice", output);
    }

    [Fact]
    public async Task Dispatch_ShouldParseGroupedShortOptionsWithAttachedValue()
    {
        var registry = CreateRegistry();
        using var sw = new StringWriter();
        var original = Console.Out;
        try
        {
            Console.SetOut(sw);
            var result = await registry.DispatchAsync(new[] { "optparsetest", "-f -nvalue" });
            Assert.Equal(0, result);
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = sw.ToString();
        Assert.Contains("NAME=value", output);
        Assert.Contains("FLAG=true", output);
    }
}
