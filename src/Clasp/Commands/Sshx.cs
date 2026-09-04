using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("sshx", Description = "调用 ssh/scp 进行远程命令执行或文件传输")]
internal class Sshx : ClaspCommand
{
    [ClaspOption("--mode", "-m", Description = "运行模式：ssh（默认）或 scp")]
    public string Mode { get; set; } = "ssh";

    [ClaspOption("--host", "-H", Description = "目标主机")]
    public string Host { get; set; } = string.Empty;

    [ClaspOption("--port", "-p", Description = "SSH 端口（默认 22）")]
    public int Port { get; set; } = 22;

    [ClaspOption("--user", "-u", Description = "用户名")]
    public string User { get; set; } = string.Empty;

    [ClaspOption("--command", "-c", Description = "SSH 模式下要执行的远程命令")]
    public string Command { get; set; } = string.Empty;

    [ClaspOption("--source", "-s", Description = "SCP 源路径")]
    public string Source { get; set; } = string.Empty;

    [ClaspOption("--target", "-t", Description = "SCP 目标路径")]
    public string Target { get; set; } = string.Empty;

    [ClaspOption("--recursive", "-r", Description = "SCP 递归复制目录")]
    public bool Recursive { get; set; }

    [ClaspOption("--profile", "-P", Description = "使用 SSH config 中的配置名")]
    public string Profile { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var mode = Mode.Trim().ToLowerInvariant();
        if (mode != "ssh" && mode != "scp")
            ValidationError("--mode 仅支持 ssh 或 scp");

        if (!string.IsNullOrWhiteSpace(Profile))
        {
            var config = ResolveSshConfig();
            if (config is null)
                ValidationError($"未找到 SSH config: {GetSshConfigPath()}");

            if (!TryParseHost(config!, Profile.Trim(), out var hostName, out var user, out var port))
                ValidationError($"未在 SSH config 中找到配置: {Profile}");
        }

        if (string.IsNullOrWhiteSpace(Host) && string.IsNullOrWhiteSpace(Profile))
            ValidationError("请提供目标主机 (--host)，或使用 SSH config 配置名 (--profile)");

        if (Port is < 1 or > 65535)
            ValidationError("端口必须在 1-65535 之间");

        if (mode == "ssh" && string.IsNullOrWhiteSpace(Command))
            ValidationError("SSH 模式下请提供 --command");

        if (mode == "scp")
        {
            if (string.IsNullOrWhiteSpace(Source))
                ValidationError("SCP 模式下请提供 --source");

            if (string.IsNullOrWhiteSpace(Target))
                ValidationError("SCP 模式下请提供 --target");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var mode = Mode.Trim().ToLowerInvariant();
        ApplySshConfig();

        var sshOptions = BuildSshOptions();
        string fileName;
        string arguments;

        if (mode == "scp")
        {
            fileName = IsWindows() ? "scp.exe" : "scp";
            arguments = BuildScpArguments(sshOptions);
        }
        else
        {
            fileName = IsWindows() ? "ssh.exe" : "ssh";
            arguments = $"{sshOptions} {Host} {Command}";
        }

        WriteLine($"执行: {fileName} {arguments}", ClaspColorType.Cyan);
        WriteLine(string.Empty);

        try
        {
            var result = await CmdAsync(fileName, arguments, cancellationToken: cancellationToken);
            if (!string.IsNullOrEmpty(result.StandardOutput))
                WriteLine(result.StandardOutput);

            if (!string.IsNullOrEmpty(result.StandardError))
                WriteLine(result.StandardError, ClaspColorType.Yellow);

            WriteLine(string.Empty);
            WriteLine($"退出码: {result.ExitCode}", result.ExitCode == 0 ? ClaspColorType.Green : ClaspColorType.BrightRed);
        }
        catch (Exception ex)
        {
            WriteLine($"执行失败: {ex.Message}", ClaspColorType.BrightRed);
        }
    }

    private string BuildSshOptions()
    {
        var options = new List<string>();
        if (Port != 22)
            options.Add($"-p {Port}");

        return string.Join(" ", options);
    }

    private string BuildScpArguments(string sshOptions)
    {
        var options = new List<string>();
        if (Recursive)
            options.Add("-r");

        if (!string.IsNullOrEmpty(sshOptions))
            options.Add(sshOptions);

        options.Add($"{ResolvePath(Source)} {ResolvePath(Target)}");
        return string.Join(" ", options);
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (path.Contains("@", StringComparison.OrdinalIgnoreCase) || path.Contains(":", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!string.IsNullOrWhiteSpace(User))
            return $"{User}@{Host}:{path}";

        return $"{Host}:{path}";
    }

    private void ApplySshConfig()
    {
        if (string.IsNullOrWhiteSpace(Profile))
            return;

        var config = ResolveSshConfig();
        if (config is null)
            return;

        if (!TryParseHost(config, Profile.Trim(), out var hostName, out var user, out var port))
            return;

        if (string.IsNullOrWhiteSpace(Host))
            Host = hostName;

        if (string.IsNullOrWhiteSpace(User))
            User = user;

        if (Port == 22 && port is > 0 and <= 65535)
            Port = port;
    }

    private static string? ResolveSshConfig()
    {
        var path = GetSshConfigPath();
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static string GetSshConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".ssh", "config");
    }

    private static bool TryParseHost(string config, string profile, out string hostName, out string user, out int port)
    {
        hostName = string.Empty;
        user = string.Empty;
        port = 22;

        var profilePattern = $@"(?<=\bHost\s+{Regex.Escape(profile)}\s*(?:\r?\n|$))";
        var startMatch = Regex.Match(config, profilePattern, RegexOptions.IgnoreCase);
        if (!startMatch.Success)
            return false;

        var startIndex = startMatch.Index;
        var nextHost = Regex.Match(config.Substring(startIndex), @"\r?\nHost\s+\S+", RegexOptions.IgnoreCase);
        var block = nextHost.Success ? config.Substring(startIndex, nextHost.Index) : config.Substring(startIndex);

        foreach (var line in block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                continue;

            var eq = trimmed.IndexOf(' ');
            if (eq < 0)
                continue;

            var key = trimmed.Substring(0, eq).Trim().ToLowerInvariant();
            var value = trimmed.Substring(eq + 1).Trim();

            switch (key)
            {
                case "hostname":
                    hostName = value;
                    break;
                case "user":
                    user = value;
                    break;
                case "port":
                    if (int.TryParse(value, out var parsed) && parsed is > 0 and <= 65535)
                        port = parsed;
                    break;
            }
        }

        return !string.IsNullOrWhiteSpace(hostName);
    }
}
