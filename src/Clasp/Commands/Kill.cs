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
            if (pid == Environment.ProcessId)
            {
                WriteLine($"跳过 Clasp 自身进程 (PID: {pid})", ClaspColorType.Yellow);
                continue;
            }

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
                    if (Matches(processName, name) && !pids.Contains(process.Id))
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
        return Matches(processName, name);
    }

    /// <summary>进程名模糊匹配（不区分大小写）</summary>
    private static bool Matches(string processName, string keyword)
        => processName.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private List<int> FindProcessesByPort(int port)
    {
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

                return ParseNetstatListenPids(output, port);
            }
            catch
            {
                // ignore
            }

            return [];
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

            return ParseLsofPids(output);
        }
        catch
        {
            // ignore
        }

        return [];
    }

    /// <summary>
    /// 解析 netstat -ano：只取 LISTENING 的 TCP 行。
    /// 不过滤状态会把连接到该端口的客户端进程（ESTABLISHED / TIME_WAIT）一起杀掉。
    /// </summary>
    internal static List<int> ParseNetstatListenPids(string output, int port)
    {
        var pids = new List<int>();
        foreach (var rawLine in output.Split('\n'))
        {
            // 列：协议 本地地址 外部地址 状态 PID
            var columns = rawLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 5) continue;
            if (!columns[0].Equals("TCP", StringComparison.OrdinalIgnoreCase)) continue;
            if (!columns[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase)) continue;
            if (!EndsWithPort(columns[1], port)) continue;

            if (int.TryParse(columns[4], out var pid) && pid > 0 && !pids.Contains(pid))
            {
                pids.Add(pid);
            }
        }

        return pids;
    }

    /// <summary>解析 lsof -t 的一列 PID 输出</summary>
    internal static List<int> ParseLsofPids(string output)
    {
        var pids = new List<int>();
        foreach (var rawLine in output.Split('\n'))
        {
            if (int.TryParse(rawLine.Trim(), out var pid) && pid > 0 && !pids.Contains(pid))
            {
                pids.Add(pid);
            }
        }

        return pids;
    }

    /// <summary>本地地址（如 0.0.0.0:8080 / [::]:8080）的端口是否等于目标端口</summary>
    private static bool EndsWithPort(string address, int port)
    {
        var separator = address.LastIndexOf(':');
        return separator >= 0
            && int.TryParse(address[(separator + 1)..], out var parsed)
            && parsed == port;
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
