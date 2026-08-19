using System.Diagnostics;
using System.Linq;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("kill", Description = "干掉占用端口的程序")]
internal class Kill : ClaspCommand
{
    [ClaspOption("--port", "-p", Description = "端口号")]
    public int PortNumber { get; set; }

    [ClaspOption("--force", "-f", Description = "强制结束")]
    public bool Force { get; set; }

    [ClaspOption("--list", "-l", Description = "列出占用端口的进程")]
    public bool List { get; set; }

    [ClaspOption("--dry-run", "-d", Description = "只查看，不结束进程")]
    public bool DryRun { get; set; }

    [ClaspOption("--name", "-n", Description = "按进程名模糊匹配")]
    public string Name { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (PortNumber is < 1 or > 65535 && string.IsNullOrWhiteSpace(Name))
        {
            ValidationError("请提供有效的端口号，或使用 --name 按进程名匹配");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        List<int> pids;
        if (!string.IsNullOrWhiteSpace(Name) && PortNumber is < 1 or > 65535)
        {
            pids = FindPidsByName(Name);
        }
        else if (!string.IsNullOrWhiteSpace(Name))
        {
            pids = FindProcessesByPort(PortNumber)
                .Where(pid => NameMatches(pid, Name))
                .ToList();
        }
        else
        {
            pids = FindProcessesByPort(PortNumber);
        }

        if (pids.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(Name))
                WriteLine($"未找到匹配进程名 \"{Name}\" 的进程", ClaspColorType.Yellow);
            else
                WriteLine($"未找到占用端口 {PortNumber} 的进程", ClaspColorType.Yellow);

            return;
        }

        if (List)
        {
            foreach (var pid in pids)
            {
                var name = GetProcessName(pid);
                WriteLine($"PID: {pid,8}  {name}");
            }
            return;
        }

        foreach (var pid in pids)
        {
            var name = GetProcessName(pid);
            WriteLine($"发现进程: {name} (PID: {pid})");

            if (DryRun)
                continue;

            try
            {
                using var process = Process.GetProcessById(pid);
                if (Force)
                {
                    process.Kill(true);
                    process.WaitForExit(5000);
                    WriteLine($"已强制结束: {name} (PID: {pid})", ClaspColorType.BrightGreen);
                }
                else
                {
                    process.CloseMainWindow();
                    if (process.WaitForExit(5000))
                    {
                        WriteLine($"已关闭: {name} (PID: {pid})", ClaspColorType.BrightGreen);
                    }
                    else
                    {
                        WriteLine($"进程 {name} (PID: {pid}) 未响应，请使用 --force 强制结束", ClaspColorType.Yellow);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"结束进程失败: {ex.Message}", ClaspColorType.BrightRed);
            }
        }

        await Task.CompletedTask;
    }

    private List<int> FindPidsByName(string name)
    {
        var pids = new List<int>();
        try
        {
            var allProcesses = Process.GetProcesses();
            foreach (var process in allProcesses)
            {
                try
                {
                    var processName = process.ProcessName;
                    if (processName.Contains(name, StringComparison.OrdinalIgnoreCase) && !pids.Contains(process.Id))
                    {
                        pids.Add(process.Id);
                    }
                }
                catch
                {
                    // ignore processes that can't be accessed
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch
        {
            // ignore
        }

        return pids;
    }

    private bool NameMatches(int pid, string name)
    {
        var processName = GetProcessName(pid);
        return processName.Contains(name, StringComparison.OrdinalIgnoreCase);
    }

    private List<int> FindProcessesByPort(int port)
    {
        var pids = new List<int>();
        if (IsWindows())
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    if (!line.Contains($":{port} ", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var trimmed = line.TrimEnd();
                    var lastSpace = trimmed.LastIndexOf(' ');
                    if (lastSpace < 0)
                        continue;

                    var pidText = trimmed[(lastSpace + 1)..];
                    if (int.TryParse(pidText, out var pid) && !pids.Contains(pid))
                        pids.Add(pid);
                }
            }
            catch
            {
                // ignore
            }

            return pids;
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-iTCP:{port} -sTCP:LISTEN -t",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line, out var pid) && !pids.Contains(pid))
                    pids.Add(pid);
            }
        }
        catch
        {
            // ignore
        }

        return pids;
    }

    private string GetProcessName(int pid)
    {
        if (IsWindows())
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return process.ProcessName;
            }
            catch
            {
                return $"PID:{pid}";
            }
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ps",
                    Arguments = $"-p {pid} -o comm=",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var name = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return string.IsNullOrEmpty(name) ? $"PID:{pid}" : name;
        }
        catch
        {
            return $"PID:{pid}";
        }
    }
}
