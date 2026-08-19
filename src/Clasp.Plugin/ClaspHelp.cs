using System.Reflection;

using Clasp.Plugin.Attributes;

namespace Clasp.Plugin;

public static class ClaspHelp
{
    public static string[] RenderCommandHelp(Type commandType)
    {
        var lines = new List<string>();
        var attr = commandType.GetCustomAttribute<ClaspCommandAttribute>();

        lines.Add($"命令: {string.Join(", ", attr?.Names ?? Array.Empty<string>())}");
        lines.Add($"描述: {attr?.Description ?? "无描述"}");
        lines.Add(string.Empty);
        lines.Add("选项:");

        var options = new List<(string Names, string? Description)>();
        foreach (var property in commandType.GetProperties())
        {
            var optionAttr = property.GetCustomAttribute<ClaspOptionAttribute>();
            if (optionAttr is null)
                continue;

            options.Add((string.Join(", ", optionAttr.Names), optionAttr.Description));
        }

        if (options.Count == 0)
        {
            lines.Add("  无选项");
        }
        else
        {
            var width = options.Max(o => o.Names.Length) + 2;
            foreach (var (names, description) in options)
                lines.Add($"  {names.PadRight(width)}{description ?? ""}");
        }

        return lines.ToArray();
    }

    public static string[] RenderCommandList(IEnumerable<(string Names, string? Description)> commands)
    {
        var lines = new List<string> { "可用命令:" };
        var list = commands.ToList();

        if (list.Count == 0)
        {
            lines.Add("  无命令");
            return lines.ToArray();
        }

        var width = list.Max(c => c.Names.Length) + 2;
        foreach (var (names, description) in list)
            lines.Add($"  {names.PadRight(width)}{description ?? ""}");

        return lines.ToArray();
    }
}
