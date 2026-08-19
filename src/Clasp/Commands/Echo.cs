using Clasp.Plugin;
using Clasp.Plugin.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clasp.Commands;

[ClaspCommand("echo", Description = "输出内容")]
internal class Echo : ClaspCommand
{
    public enum LevelTypeEnum
    {
        Info,
        Warning,
        Error
    }

    [ClaspOption("--level", "-l", Description = "输出级别")]
    public LevelTypeEnum Level { get; set; }

    [ClaspOption("--msg", "-m", Description = "输出内容")]
    public string Message { get; set; } = string.Empty;

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            ShowHelp();
            return;
        }

        WriteLine(Level switch
        {
            LevelTypeEnum.Warning => $"[Warning] {Message}",
            LevelTypeEnum.Error => $"[Error] {Message}",
            _ => $"[Info] {Message}"
        }, Level switch
        {
            LevelTypeEnum.Warning => ClaspColorType.Yellow,
            LevelTypeEnum.Error => ClaspColorType.BrightRed,
            _ => ClaspColorType.Cyan
        });
    }
}
