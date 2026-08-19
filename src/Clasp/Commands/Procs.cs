using System.Diagnostics;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("procs", Description = "列出进程信息")]
internal class Procs : ClaspCommand
{
    [ClaspOption("--name", Description = "按名称模糊过滤")]
    public string Name { get; set; } = string.Empty;

    [ClaspOption("--top", "-n", Description = "按内存显示前 N 个 (默认 20)")]
    public int Top { get; set; } = 20;

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var procs = Process.GetProcesses();
        if (!string.IsNullOrWhiteSpace(Name))
            procs = procs.Where(p => p.ProcessName.Contains(Name, StringComparison.OrdinalIgnoreCase)).ToArray();

        var top = Math.Clamp(Top, 1, 500);
        var list = procs
            .Select(p => (Process: p, Mem: SafeWorkingSet(p)))
            .OrderByDescending(item => item.Mem)
            .Take(top)
            .ToList();

        WriteLine($"{"PID",-8}{"内存(MB)",-10}名称");
        foreach (var item in list)
        {
            WriteLine($"{item.Process.Id,-8}{item.Mem / 1048576.0,8:N1}  {item.Process.ProcessName}");
            item.Process.Dispose();
        }

        await Task.CompletedTask;
    }

    private static long SafeWorkingSet(Process p)
    {
        try
        {
            return p.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }
}
