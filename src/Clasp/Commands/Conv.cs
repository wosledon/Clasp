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

    public override async Task ExecuteAsync(ClaspCommandArgs args, CancellationToken cancellationToken = default)
    {
        var values = args.Values;
        if (values.Count < 3)
        {
            ShowHelp();
            return;
        }

        if (!double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            WriteLine($"无效数值: {values[0]}", ClaspColorType.BrightRed);
            return;
        }

        var from = values[1];
        var to = values[2];

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
