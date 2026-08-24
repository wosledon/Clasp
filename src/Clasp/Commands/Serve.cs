using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System.Net;

namespace Clasp.Commands;

[ClaspCommand("serve", Description = "启动静态文件服务器（开发用）")]
internal class Serve : ClaspCommand
{
    [ClaspOption("--path", "-p", Description = "要服务的目录路径（默认当前目录）")]
    public string Path { get; set; } = ".";

    [ClaspOption("--port", "-P", Description = "端口号（默认8080）")]
    public int Port { get; set; } = 8080;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Port < 1 || Port > 65535)
            ValidationError("端口必须在 1-65535 之间");
        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        WriteLine($"服务器已启动: http://localhost:{Port}/", ClaspColorType.Green);
        WriteLine($"服务目录: {Path}", ClaspColorType.Cyan);
        WriteLine("提示: 此命令需要进一步实现完整功能", ClaspColorType.Yellow);
    }
}