using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

sealed class CommandRegistry
{
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    private CommandRegistry() { }

    public static CommandRegistry Scan(Assembly assembly)
    {
        var registry = new CommandRegistry();

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(Clasp.Plugin.ClaspCommand).IsAssignableFrom(type) || type.IsAbstract)
                continue;

            var attr = type.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspCommandAttribute>();
            if (attr is null)
                continue;

            foreach (var name in attr.Names)
                registry._commands[name] = type;
        }

        return registry;
    }

    public async Task<int> DispatchAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var commandName = args[0];
        if (!_commands.TryGetValue(commandName, out var commandType))
        {
            Console.WriteLine($"未知命令: {commandName}");
            PrintUsage();
            return 1;
        }

        var parsed = ParseOptions(args.Skip(1).ToArray(), commandType);
        if (parsed.Options.ContainsKey("--help") || parsed.Options.ContainsKey("-h"))
        {
            PrintHelp(commandType);
            return 0;
        }

        var commandArgs = new Clasp.Plugin.ClaspCommandArgs { Command = commandName };

        var optionsField = typeof(Clasp.Plugin.ClaspCommandArgs).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
        var optionsDict = (Dictionary<string, string>)optionsField!.GetValue(commandArgs)!;
        foreach (var (key, value) in parsed.Options)
            optionsDict[key] = value;

        foreach (var positional in parsed.Positionals)
            commandArgs.AddValue(positional);

        if (parsed.Positionals.Count > 0)
            commandArgs.Value = parsed.Positionals[0];

        var command = (Clasp.Plugin.ClaspCommand)Activator.CreateInstance(commandType)!;
        InjectOptions(command, parsed.Options);

        try
        {
            await command.ExecuteAsync(commandArgs, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }

    private static (Dictionary<string, string> Options, List<string> Positionals) ParseOptions(string[] args, Type commandType)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        var boolOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var valueOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in commandType.GetProperties())
        {
            var optionAttr = property.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspOptionAttribute>();
            if (optionAttr is null)
                continue;

            foreach (var name in optionAttr.Names)
            {
                if (property.PropertyType == typeof(bool))
                    boolOptions.Add(name);
                else
                    valueOptions.Add(name);
            }
        }

        var optionsEnded = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (optionsEnded || arg == "-" || !arg.StartsWith("-"))
            {
                positionals.Add(arg);
                continue;
            }

            if (arg == "--")
            {
                optionsEnded = true;
                continue;
            }

            if (arg.StartsWith("--"))
            {
                if (boolOptions.Contains(arg))
                {
                    options[arg] = "true";
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[arg] = args[i + 1];
                    i++;
                }
                continue;
            }

            var shortOptions = arg.Substring(1);
            if (shortOptions.Length == 1)
            {
                var key = $"-{shortOptions}";
                if (boolOptions.Contains(key))
                {
                    options[key] = "true";
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[key] = args[i + 1];
                    i++;
                }
                else
                {
                    options[key] = "true";
                }
            }
            else
            {
                var valueIndex = -1;
                for (var j = shortOptions.Length - 1; j >= 0; j--)
                {
                    var key = $"-{shortOptions[j]}";
                    if (valueOptions.Contains(key))
                    {
                        valueIndex = j;
                        break;
                    }
                }

                if (valueIndex >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    options[$"-{shortOptions[valueIndex]}"] = args[i + 1];
                    i++;

                    for (var j = 0; j < valueIndex; j++)
                        options[$"-{shortOptions[j]}"] = "true";

                    for (var j = valueIndex + 1; j < shortOptions.Length; j++)
                        options[$"-{shortOptions[j]}"] = "true";
                }
                else
                {
                    foreach (var ch in shortOptions)
                        options[$"-{ch}"] = "true";
                }
            }
        }

        return (options, positionals);
    }

    private static void InjectOptions(Clasp.Plugin.ClaspCommand command, Dictionary<string, string> parsedOptions)
    {
        var commandType = command.GetType();
        foreach (var property in commandType.GetProperties())
        {
            var optionAttr = property.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspOptionAttribute>();
            if (optionAttr is null)
                continue;

            foreach (var name in optionAttr.Names)
            {
                if (parsedOptions.TryGetValue(name, out var rawValue))
                {
                    var converted = ConvertValue(rawValue, property.PropertyType);
                    property.SetValue(command, converted);
                    break;
                }
            }
        }
    }

    private static object ConvertValue(string raw, Type targetType)
    {
        if (targetType.IsEnum)
            return Enum.TryParse(targetType, raw, ignoreCase: true, out var result)
                ? result
                : Activator.CreateInstance(targetType)!;

        if (targetType == typeof(bool))
            return bool.Parse(raw);

        return Convert.ChangeType(raw, targetType);
    }

    public IEnumerable<(string Names, string? Description)> GetCommands()
    {
        return _commands
            .GroupBy(pair => pair.Value)
            .Select(group =>
            {
                var names = string.Join(", ", group.Select(pair => pair.Key).Distinct());
                var desc = group.Key.GetCustomAttribute<Clasp.Plugin.Attributes.ClaspCommandAttribute>()?.Description;
                return (Names: names, Description: desc);
            })
            .OrderBy(item => item.Names);
    }

    private void PrintUsage()
    {
        foreach (var line in Clasp.Plugin.ClaspHelp.RenderCommandList(GetCommands()))
            Console.WriteLine(line);
    }

    private void PrintHelp(Type commandType)
    {
        foreach (var line in Clasp.Plugin.ClaspHelp.RenderCommandHelp(commandType))
            Console.WriteLine(line);
    }
}