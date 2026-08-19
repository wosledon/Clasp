using System.Globalization;

using Clasp.Plugin;
using Clasp.Plugin.Attributes;

namespace Clasp.Commands;

[ClaspCommand("conv", Description = "单位换算 (字节/温度)")]
internal class Conv : ClaspCommand
{
    private static readonly Dictionary<string, double> ByteUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["b"] = 1,
        ["kb"] = 1024,
        ["mb"] = 1024 * 1024,
        ["gb"] = 1024 * 1024 * 1024,
        ["tb"] = 1024 * 1024 * 1024 * 1024.0
    };

    [ClaspOption("--number", "-n", Description = "数值")]
    public double Number { get; set; }

    [ClaspOption("--from", Description = "源单位，如 b/kb/mb/gb/tb 或 c/f/k")]
    public string From { get; set; } = string.Empty;

    [ClaspOption("--to", Description = "目标单位，如 b/kb/mb/gb/tb 或 c/f/k")]
    public string To { get; set; } = string.Empty;

    public override async Task ValidateAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var from = From;
        var to = To;

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            ValidationError("请提供 --from 和 --to");
        }

        if (!ByteUnits.ContainsKey(from.ToLowerInvariant()) && !IsTempUnit(from))
        {
            ValidationError($"不支持的单位: {from}");
        }

        if (!ByteUnits.ContainsKey(to.ToLowerInvariant()) && !IsTempUnit(to))
        {
            ValidationError($"不支持的单位: {to}");
        }

        await Task.CompletedTask;
    }

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var number = Number;
        var from = From;
        var to = To;

        if (ByteUnits.TryGetValue(from, out var fromFactor) && ByteUnits.TryGetValue(to, out var toFactor))
        {
            WriteLine($"{Format(number * fromFactor / toFactor)} {to.ToUpperInvariant()}");
            return;
        }

        if (IsTempUnit(from) && IsTempUnit(to))
        {
            var celsius = from.ToLowerInvariant() switch
            {
                "f" => (number - 32) * 5 / 9,
                "k" => number - 273.15,
                _ => number
            };
            var result = to.ToLowerInvariant() switch
            {
                "f" => celsius * 9 / 5 + 32,
                "k" => celsius + 273.15,
                _ => celsius
            };
            WriteLine($"{Format(result)} {to.ToUpperInvariant()}");
            return;
        }

        WriteLine($"不支持的单位: {from} -> {to}", ClaspColorType.BrightRed);
    }

    private static bool IsTempUnit(string unit) =>
        unit is "c" or "C" or "f" or "F" or "k" or "K";

    private static string Format(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);
}
