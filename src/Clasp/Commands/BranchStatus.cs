using System.Text;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("branch", "br", Description = "显示当前分支状态汇总：分支、工作区、提交、ahead/behind")]
internal class BranchStatus : ClaspCommand
{
    [ClaspOption("--all", "-a", Description = "同时显示所有本地分支摘要")]
    public bool All { get; set; }

    [ClaspOption("--porcelain", "-p", Description = "以更紧凑的格式输出")]
    public bool Porcelain { get; set; }

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var check = await CmdAsync("git", "rev-parse --is-inside-work-tree");
        if (check.ExitCode != 0)
        {
            ValidationError("当前目录不是 Git 仓库，无法执行 branch 命令");
        }

        if (Porcelain && All)
        {
            ValidationError("--porcelain 与 --all 暂不支持同时使用");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (Porcelain)
        {
            await ExecutePorcelainAsync(cancellationToken);
            return;
        }

        var currentBranch = (await CmdResultTrimmedAsync("git", "rev-parse --abbrev-ref HEAD")) ?? string.Empty;
        var upstream = await CmdResultTrimmedAsync("git", "rev-parse --abbrev-ref @{upstream}");
        var aheadBehind = await CmdResultTrimmedAsync("git", "rev-list --left-right --count HEAD...@{upstream}");
        var statusResult = await CmdResultAsync("git", "status --short --branch");
        var recentCommitsResult = await CmdResultAsync("git", "log --oneline --decorate -n 5");

        var branchLabel = string.IsNullOrWhiteSpace(upstream) ? currentBranch : $"{currentBranch} -> {upstream}";
        WriteLine(branchLabel, ClaspColorType.Cyan);

        if (!string.IsNullOrWhiteSpace(aheadBehind) && aheadBehind.Contains('\t'))
        {
            var parts = aheadBehind.Split('\t');
            if (parts.Length == 2 && (parts[0] != "0" || parts[1] != "0"))
            {
                WriteLine($"ahead {parts[0]}, behind {parts[1]}", ClaspColorType.Yellow);
            }
        }

        if (!string.IsNullOrWhiteSpace(statusResult.StandardOutput))
        {
            WriteLine(string.Empty);
            WriteLine("工作区状态:", ClaspColorType.Green);
            WriteLine(statusResult.StandardOutput);
        }

        WriteLine(string.Empty);
        WriteLine("最近提交:", ClaspColorType.Green);
        if (string.IsNullOrWhiteSpace(recentCommitsResult.StandardOutput))
        {
            WriteLine("暂无提交记录", ClaspColorType.Yellow);
        }
        else
        {
            WriteLine(recentCommitsResult.StandardOutput);
        }

        if (All)
        {
            WriteLine(string.Empty);
            WriteLine("本地分支:", ClaspColorType.Green);
            var branchesResult = await CmdResultAsync("git", "branch --format='%(refname:short) %(upstream:short) %(ahead:1) %(behind:1)' --no-abbrev");
            WriteLine(string.IsNullOrWhiteSpace(branchesResult.StandardOutput) ? "暂无本地分支" : branchesResult.StandardOutput);
        }
    }

    private async Task ExecutePorcelainAsync(CancellationToken cancellationToken)
    {
        var currentBranch = (await CmdResultTrimmedAsync("git", "rev-parse --abbrev-ref HEAD")) ?? string.Empty;
        var upstream = await CmdResultTrimmedAsync("git", "rev-parse --abbrev-ref @{upstream}");
        var aheadBehind = await CmdResultTrimmedAsync("git", "rev-list --left-right --count HEAD...@{upstream}");
        var statusResult = await CmdResultAsync("git", "status --short --branch");
        var recentCommitsResult = await CmdResultAsync("git", "log --oneline --decorate -n 5");

        WriteLine($"branch {currentBranch}");
        WriteLine($"upstream {upstream}");

        if (!string.IsNullOrWhiteSpace(aheadBehind) && aheadBehind.Contains('\t'))
        {
            var parts = aheadBehind.Split('\t');
            if (parts.Length == 2)
            {
                WriteLine($"ahead {parts[0]}");
                WriteLine($"behind {parts[1]}");
            }
        }

        if (!string.IsNullOrWhiteSpace(statusResult.StandardOutput))
        {
            foreach (var line in statusResult.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                WriteLine($"status {line}");
            }
        }

        if (!string.IsNullOrWhiteSpace(recentCommitsResult.StandardOutput))
        {
            var index = 0;
            foreach (var line in recentCommitsResult.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                WriteLine($"commit.{++index} {line}");
            }
        }
    }

    private async Task<CommandResult> CmdResultAsync(string fileName, string? arguments = null, CancellationToken cancellationToken = default)
    {
        var result = await CmdAsync(fileName, arguments, cancellationToken: cancellationToken);
        return result.ExitCode == 0 ? result : new CommandResult(result.ExitCode, string.Empty, result.StandardError);
    }

    private async Task<string?> CmdResultTrimmedAsync(string fileName, string? arguments = null, CancellationToken cancellationToken = default)
    {
        var result = await CmdResultAsync(fileName, arguments, cancellationToken);
        return result.StandardOutput?.Trim();
    }
}
