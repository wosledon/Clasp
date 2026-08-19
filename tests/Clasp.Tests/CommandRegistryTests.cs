using System.Reflection;
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
}
