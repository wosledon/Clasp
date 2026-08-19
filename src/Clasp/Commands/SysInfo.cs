using System.IO;
using System.Runtime.InteropServices;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("sysinfo", Description = "显示系统信息")]
internal class SysInfo : ClaspCommand
{
    [ClaspOption("--cpu", Description = "仅显示 CPU/系统信息")]
    public bool CpuOnly { get; set; }

    [ClaspOption("--mem", Description = "仅显示内存信息")]
    public bool MemOnly { get; set; }

    [ClaspOption("--disk", Description = "仅显示磁盘信息")]
    public bool DiskOnly { get; set; }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var showAll = !CpuOnly && !MemOnly && !DiskOnly;

        if (showAll || CpuOnly)
        {
            WriteLine($"系统: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            WriteLine($"CPU: {Environment.ProcessorCount} 核");
        }

        if (showAll || MemOnly)
        {
            var total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            WriteLine($"内存总量: {total / 1073741824.0:N2} GB");
        }

        if (showAll || DiskOnly)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                    continue;

                WriteLine($"磁盘 {drive.Name} [{drive.DriveFormat}] 总量 {drive.TotalSize / 1073741824.0:N2} GB 可用 {drive.AvailableFreeSpace / 1073741824.0:N2} GB");
            }
        }

        await Task.CompletedTask;
    }
}
