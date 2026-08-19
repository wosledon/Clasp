using System.Diagnostics;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("gittag", "gt", Description = "Git 标签管理：列出 / 查看 / 创建 / 删除标签")]
internal class GitTag : ClaspCommand
{
    [ClaspOption("--create", "-c", Description = "创建标签")]
    public bool Create { get; set; }

    [ClaspOption("--delete", "-d", Description = "删除标签")]
    public bool Delete { get; set; }

    [ClaspOption("--message", "-m", Description = "附注标签说明，需配合 --create 使用")]
    public string Message { get; set; } = string.Empty;

    [ClaspOption("--push", "-p", Description = "创建后推送到远程，或单独推送指定标签")]
    public bool Push { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var mode = (Create ? 1 : 0) + (Delete ? 1 : 0);
        if (mode > 1)
        {
            ValidationError("--create / --delete 不能同时使用");
        }

        var tagName = args.Values.FirstOrDefault();
        if (Delete && string.IsNullOrWhiteSpace(tagName))
        {
            ValidationError("删除标签时需要指定标签名，例如: gittag --delete <tag>");
        }

        if (Create && string.IsNullOrWhiteSpace(tagName))
        {
            ValidationError("创建标签时需要指定标签名，例如: gittag --create <tag>");
        }

        if (!string.IsNullOrWhiteSpace(Message) && !Create)
        {
            ValidationError("--message 需配合 --create 使用");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var tagName = args.Values.FirstOrDefault();

        if (Delete)
        {
            await RunGitAsync($"tag -d {EscapeArg(tagName)}", cancellationToken);
            WriteLine($"已删除标签: {tagName}", ClaspColorType.Green);
            return;
        }

        if (Push && !Create)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                await RunGitAsync("push --tags", cancellationToken);
                WriteLine("已推送全部标签", ClaspColorType.Green);
            }
            else
            {
                await RunGitAsync($"push origin {EscapeArg(tagName)}", cancellationToken);
                WriteLine($"已推送标签: {tagName}", ClaspColorType.Green);
            }

            return;
        }

        if (Create)
        {
            var gitArgs = string.IsNullOrWhiteSpace(Message)
                ? $"tag {EscapeArg(tagName)}"
                : $"tag -a {EscapeArg(tagName)} -m {EscapeArg(Message)}";

            await RunGitAsync(gitArgs, cancellationToken);
            WriteLine($"已创建标签: {tagName}", ClaspColorType.Green);

            if (Push)
            {
                await RunGitAsync($"push origin {EscapeArg(tagName)}", cancellationToken);
                WriteLine($"已推送标签: {tagName}", ClaspColorType.Green);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(tagName))
        {
            var result = await RunGitResultAsync($"show --no-pager --no-notes {EscapeArg(tagName)}", cancellationToken);
            if (string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                WriteLine($"未找到标签: {tagName}", ClaspColorType.Yellow);
                return;
            }

            WriteLine(result.StandardOutput);
            return;
        }

        var listResult = await RunGitResultAsync("tag --list", cancellationToken);
        var output = listResult.StandardOutput;
        if (string.IsNullOrWhiteSpace(output))
        {
            WriteLine("当前仓库没有标签", ClaspColorType.Yellow);
            return;
        }

        WriteLine(output);
    }

    private async Task RunGitAsync(string arguments, CancellationToken cancellationToken)
    {
        var result = await RunGitResultAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
            WriteLine(message.Trim(), ClaspColorType.Red);
        }
    }

    private async Task<CommandResult> RunGitResultAsync(string arguments, CancellationToken cancellationToken)
    {
        var check = await CmdAsync("git", "rev-parse --is-inside-work-tree");
        if (check.ExitCode != 0)
        {
            ValidationError("当前目录不是 Git 仓库，无法执行 gittag 命令");
        }

        return await CmdAsync("git", arguments, cancellationToken: cancellationToken);
    }

    private static string EscapeArg(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (!value.Contains(' ') && !value.Contains('"') && !value.Contains('\t'))
            return value;

        return $"\"{value.Replace("\"", "\\\"")}\"";
    }
}
