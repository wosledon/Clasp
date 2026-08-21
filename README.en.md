# Clasp

[中文主文档](README.md) | English

A cross-platform .NET CLI toolbox with a plugin system. It ships with built-in utilities for networking, text processing, system info, and more. You can also extend it by dropping plugin DLLs into the `plugins` folder.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Quick Start

Build from source:

```bash
dotnet build
./src/Clasp/bin/Debug/net10.0/Clasp help
./src/Clasp/bin/Debug/net10.0/Clasp dns --host example.com
./src/Clasp/bin/Debug/net10.0/Clasp echo --msg "hello"
```

Or download a release archive, extract it, and run `clasp` directly:

```bash
./clasp help
./clasp dns --host example.com
```

## Build and Test

```bash
dotnet build
dotnet test tests/Clasp.Tests/Clasp.Tests.csproj
```

## Built-in Commands

| Command               | Description                               |
| --------------------- | ----------------------------------------- |
| `b64`                 | Base64 encode/decode                      |
| `cat`                 | Read and print a file                     |
| `conv`                | Convert units: bytes/temperature          |
| `count`               | Count lines, words, and characters        |
| `date`                | Show current date and time                |
| `dns`                 | Query DNS A/AAAA records                  |
| `echo`                | Print a message                           |
| `env`                 | Show environment variables                |
| `file-download`, `fd` | Multi-threaded file downloader            |
| `hash`                | Compute text or file hash                 |
| `help`                | Show all available commands               |
| `http`                | Send an HTTP request                      |
| `ip`                  | Show local or public IP addresses         |
| `json`                | Format or validate JSON                   |
| `jwt`                 | Decode JWT without signature verification |
| `kill`                | Kill processes by port or name            |
| `ls`                  | List files in a directory                 |
| `port`                | Check TCP port connectivity               |
| `procs`               | List processes                            |
| `rand`                | Generate random passwords or numbers      |
| `speed`               | Measure download speed                    |
| `sysinfo`             | Show system information                   |
| `ts`                  | Convert timestamps and dates              |
| `urlenc`              | URL encode/decode                         |
| `uuid`                | Generate UUID v4                          |
| `version`             | Show version                              |

Use `--help` or `-h` with any command to see its options.

## Plugin System

Clasp can load external commands from `.dll` files placed in the `plugins` folder next to the app. The folder is created automatically if it does not exist.

### How to create a plugin

1. Create a .NET 10 class library.
2. Add a project reference to `src/Clasp.Plugin/Clasp.Plugin.csproj`.
3. Create a command class that inherits from `ClaspCommand`.
4. Decorate the class with `[ClaspCommand("name", Description = "...")]`.
5. Add option properties decorated with `[ClaspOption("--option", "-o", Description = "...")]`.
6. Implement `ValidateAsync` and `ExecuteAsync`.

### Minimal Example

```csharp
using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace MyPlugin;

[ClaspCommand("hello", Description = "Say hello")]
internal class HelloCommand : ClaspCommand
{
    [ClaspOption("--name", "-n", Description = "Name to greet")]
    public string Name { get; set; } = "world";

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"Hello, {Name}!");
        await Task.CompletedTask;
    }
}
```

### Build and install the plugin

```bash
dotnet build
```

Then copy the compiled plugin DLL into the `plugins` folder beside the Clasp app or executable.

### Notes

- Plugin commands are discovered via reflection using `[ClaspCommand]` and `[ClaspOption]`.
- The same `ClaspCommandArgs`, color helpers, and process helper APIs are available in plugins.
- If multiple plugins define the same command name, the last loaded one wins.

## License

MIT